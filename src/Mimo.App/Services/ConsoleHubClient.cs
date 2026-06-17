using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Mimo.App.Auth;

namespace Mimo.App.Services;

/// <summary>
/// Conexiones SignalR de la consola de agente. Usa DOS hubs (por el diseño del backend):
///   - ChatHub (/hubs/chat): se une a la conversación abierta y RECIBE "MessageReceived"
///     (mensajes del ciudadano y eco de los del agente).
///   - TicketHub (/hubs/tickets): ENVÍA mensajes al ciudadano (SendMessageToCitizen).
/// El token JWT viaja por el parámetro access_token (lo lee el handler de JwtBearer para /hubs).
/// </summary>
public sealed class ConsoleHubClient(SessionState session, IConfiguration config) : IAsyncDisposable
{
    private HubConnection? _tickets;
    private HubConnection? _chat;
    private string? _joinedConversation;

    /// <summary>Mensaje recibido en la conversación abierta.</summary>
    public event Action<IncomingMessage>? MessageReceived;

    /// <summary>Cambió la cola del rol (nuevo ticket, asignación o resolución). Útil para refrescar la bandeja.</summary>
    public event Action? QueueChanged;

    /// <summary>Mensaje de chat interno dirigido a este funcionario.</summary>
    public event Action<IncomingInternalMessage>? InternalMessageReceived;

    public bool IsConnected =>
        _chat?.State == HubConnectionState.Connected && _tickets?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_tickets is not null) return; // ya iniciado

        var baseUrl = config["Api:BaseUrl"] ?? string.Empty;
        var token   = session.Token ?? string.Empty;

        _tickets = Build($"{baseUrl}/hubs/tickets", token);
        _chat    = Build($"{baseUrl}/hubs/chat", token);

        _chat.On<IncomingMessage>("MessageReceived", msg => MessageReceived?.Invoke(msg));

        // Eventos de cola del TicketHub: solo disparan un refresco de la bandeja (payload ignorado).
        _tickets.On<object?>("TicketEnqueued", _ => QueueChanged?.Invoke());
        _tickets.On<object?>("TicketAssigned", _ => QueueChanged?.Invoke());
        _tickets.On<object?>("TicketResolved", _ => QueueChanged?.Invoke());

        // Chat interno dirigido a este funcionario (grupo agent:{id}, auto-unido al conectar).
        _tickets.On<IncomingInternalMessage>("InternalMessage", m => InternalMessageReceived?.Invoke(m));

        await _tickets.StartAsync();
        await _chat.StartAsync();
    }

    /// <summary>Se suscribe a las colas de los roles indicados para recibir notificaciones en vivo.</summary>
    public async Task JoinRoleQueuesAsync(IEnumerable<string> roleIds)
    {
        if (_tickets is null) return;
        foreach (var roleId in roleIds)
            await _tickets.InvokeAsync("JoinRoleQueue", roleId);
    }

    /// <summary>Se une (o cambia) a la conversación cuyos mensajes se quieren recibir en vivo.</summary>
    public async Task OpenConversationAsync(Guid conversationId)
    {
        if (_chat is null) return;
        var id = conversationId.ToString();
        if (_joinedConversation == id) return;

        if (_joinedConversation is not null)
            await _chat.InvokeAsync("LeaveConversation", _joinedConversation);

        _joinedConversation = id;
        await _chat.InvokeAsync("JoinConversation", id);
    }

    /// <summary>Envía un mensaje del agente al ciudadano (se persiste y se emite a la conversación).</summary>
    public Task SendMessageAsync(Guid conversationId, string content) =>
        _tickets is null
            ? Task.CompletedTask
            : _tickets.InvokeAsync("SendMessageToCitizen", conversationId.ToString(), content);

    private static HubConnection Build(string url, string token) =>
        new HubConnectionBuilder()
            .WithUrl(url, options => options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

    public async ValueTask DisposeAsync()
    {
        if (_chat is not null)    await _chat.DisposeAsync();
        if (_tickets is not null) await _tickets.DisposeAsync();
    }

    /// <summary>Mensaje entrante por SignalR (espejo de MessageResponse del backend).</summary>
    public sealed record IncomingMessage(Guid Id, int Role, string Content, DateTime CreatedAt);

    /// <summary>Mensaje de chat interno entrante (espejo de InternalMessageResponse).</summary>
    public sealed record IncomingInternalMessage(
        Guid Id, Guid FromAgentId, Guid ToAgentId, Guid? RelatedTicketId, string Content, bool IsRead, DateTime CreatedAt);
}
