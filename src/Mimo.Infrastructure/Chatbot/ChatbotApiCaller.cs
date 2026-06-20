using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mimo.Core.DTOs.Chatbot;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Chatbot;

/// <summary>
/// Ejecuta los nodos ApiCall del chatbot por opciones (#27 corte 3) con controles anti-SSRF:
/// allow-list de hosts (deny por defecto), bloqueo de IPs internas/metadata, timeout, tamaño máximo
/// y sin redirecciones. La URL/cuerpo admiten <c>{variable}</c>.
/// </summary>
public sealed class ChatbotApiCaller(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatbotApiCaller> logger) : IChatbotApiCaller
{
    public const string HttpClientName = "ChatbotApiCall";

    public async Task<ApiCallResult> CallAsync(
        FlowNode apiNode, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default)
    {
        var allowedHosts = configuration.GetSection("Chatbot:ApiCall:AllowedHosts").Get<string[]>() ?? [];
        var allowLocalhost = configuration.GetValue("Chatbot:ApiCall:AllowLocalhost", false);
        var maxBytes = configuration.GetValue("Chatbot:ApiCall:MaxResponseBytes", 65536);

        var url = FlowText.Substitute(apiNode.Url, variables);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("ApiCall con URL inválida en nodo {Node}", apiNode.Id);
            return new ApiCallResult(false, null);
        }

        if (!IsHostAllowed(uri.Host, allowedHosts))
        {
            logger.LogWarning("ApiCall a host no permitido '{Host}' (nodo {Node})", uri.Host, apiNode.Id);
            return new ApiCallResult(false, null);
        }

        if (await ResolvesToBlockedAddressAsync(uri.Host, allowLocalhost, ct))
        {
            logger.LogWarning("ApiCall a host '{Host}' que resuelve a una IP bloqueada (nodo {Node})", uri.Host, apiNode.Id);
            return new ApiCallResult(false, null);
        }

        try
        {
            var method = string.Equals(apiNode.Method, "POST", StringComparison.OrdinalIgnoreCase)
                ? HttpMethod.Post : HttpMethod.Get;
            using var request = new HttpRequestMessage(method, uri);
            if (method == HttpMethod.Post)
            {
                var body = FlowText.Substitute(apiNode.Body, variables);
                request.Content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json");
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            string? captured = null;
            if (!string.IsNullOrEmpty(apiNode.CaptureVariable))
                captured = await ReadCappedAsync(response, maxBytes, ct);

            return new ApiCallResult(response.IsSuccessStatusCode, captured);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ApiCall falló (nodo {Node})", apiNode.Id);
            return new ApiCallResult(false, null);
        }
    }

    /// <summary>El host debe coincidir (exacto o sufijo de dominio) con la allow-list. Vacía = deny all.</summary>
    public static bool IsHostAllowed(string host, IReadOnlyList<string> allowedHosts)
    {
        foreach (var allowed in allowedHosts)
        {
            if (string.IsNullOrWhiteSpace(allowed)) continue;
            var a = allowed.Trim().TrimStart('.');
            if (host.Equals(a, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + a, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task<bool> ResolvesToBlockedAddressAsync(string host, bool allowLocalhost, CancellationToken ct)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var literal)) addresses = [literal];
        else
        {
            try { addresses = await Dns.GetHostAddressesAsync(host, ct); }
            catch { return true; } // no resuelve → tratar como bloqueado
        }
        // Bloquear si CUALQUIER IP resuelta es interna (defensa contra registros mixtos).
        return addresses.Any(ip => IsBlockedAddress(ip, allowLocalhost));
    }

    /// <summary>
    /// IPs a bloquear: link-local/metadata (169.254/fe80, incl. 169.254.169.254) SIEMPRE; loopback,
    /// privadas y ULA salvo que <paramref name="allowLocalhost"/> esté habilitado (dev/integración interna).
    /// </summary>
    public static bool IsBlockedAddress(IPAddress ip, bool allowLocalhost)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

        if (IsLinkLocal(ip)) return true; // metadata cloud / link-local: siempre bloqueado

        var internal_ = IPAddress.IsLoopback(ip) || IsPrivate(ip);
        return internal_ && !allowLocalhost;
    }

    private static bool IsLinkLocal(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 169 && b[1] == 254;            // 169.254.0.0/16
        }
        return ip.IsIPv6LinkLocal;                         // fe80::/10
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 10                                     // 10.0.0.0/8
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)      // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168);                  // 192.168.0.0/16
        }
        // IPv6 ULA fc00::/7
        var bytes = ip.GetAddressBytes();
        return (bytes[0] & 0xFE) == 0xFC;
    }

    private static async Task<string> ReadCappedAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[Math.Clamp(maxBytes, 1, 1024 * 1024)];
        var total = 0;
        int read;
        while (total < buffer.Length &&
               (read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct)) > 0)
            total += read;
        return Encoding.UTF8.GetString(buffer, 0, total);
    }
}
