using Mimo.Core.Models;

namespace Mimo.Core.Interfaces;

/// <summary>
/// Resultado de la emisión de un token JWT.
/// </summary>
/// <param name="Token">Token firmado en formato JWT compacto.</param>
/// <param name="ExpiresAtUtc">Fecha y hora (UTC) de expiración del token.</param>
public record AuthToken(string Token, DateTime ExpiresAtUtc);

/// <summary>
/// Emisión de tokens JWT para funcionarios (Mimo.Api) y super administradores (Mimo.Admin.Api).
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Genera un token para un funcionario autenticado, incluyendo los claims
    /// "agent_id", "tenant_slug" y un claim "role" por cada rol asignado.
    /// </summary>
    AuthToken GenerateAgentToken(Agent agent, string tenantSlug, IEnumerable<string> roleNames);

    /// <summary>
    /// Genera un token para un super administrador, incluyendo el claim "role" = "SuperAdmin".
    /// </summary>
    AuthToken GenerateSuperAdminToken(SuperAdmin superAdmin);
}
