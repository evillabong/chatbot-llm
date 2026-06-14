using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mimo.Core.Constants;
using Mimo.Core.Interfaces;
using Mimo.Core.Models.Configuration;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.AI;

/// <summary>
/// Resuelve el cliente LLM correspondiente al conector de IA activo en la base de datos.
///
/// Para añadir un nuevo proveedor de IA basta con:
///   1. Implementar ILlmClient para ese proveedor.
///   2. Registrar su clave en Mimo.Core.Constants.AiProviders.
///   3. Añadir el caso correspondiente en el switch de Build.
/// El cambio de proveedor en producción es solo un cambio de datos (IsActive en ai_connectors).
/// </summary>
public class LlmClientFactory(
    GlobalDbContext db,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) : ILlmClientFactory
{
    /// <summary>Nombre del HttpClient con nombre usado por todos los conectores de IA.</summary>
    public const string HttpClientName = "llm";

    public async Task<ILlmClient> GetActiveClientAsync(CancellationToken ct = default)
    {
        var connector = await db.AiConnectors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsActive, ct)
            ?? throw new InvalidOperationException(
                "No hay un conector de IA activo configurado en la base de datos.");

        return Build(connector.Provider, connector.Settings);
    }

    private ILlmClient Build(string provider, LlmConnectorSettings settings)
    {
        var http = httpClientFactory.CreateClient(HttpClientName);

        return provider switch
        {
            AiProviders.DeepSeek => new DeepSeekClient(http, settings, loggerFactory.CreateLogger<DeepSeekClient>()),
            _ => throw new NotSupportedException($"Proveedor de IA no soportado: '{provider}'.")
        };
    }
}
