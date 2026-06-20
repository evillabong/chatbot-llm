namespace Mimo.Core.Interfaces;

/// <summary>Refresh token recién emitido (valor en claro, solo se entrega una vez).</summary>
public record IssuedRefreshToken(string Token, DateTime ExpiresAt);

/// <summary>Resultado de rotar un refresh token válido: el funcionario y el nuevo token emitido.</summary>
public record RefreshRotation(Guid AgentId, string Token, DateTime ExpiresAt);

/// <summary>
/// Emisión, rotación y revocación de refresh tokens de funcionarios (#9). Opera sobre el esquema del
/// tenant ya resuelto. Solo persiste el hash del token.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Emite y persiste un refresh token nuevo para el funcionario.</summary>
    Task<IssuedRefreshToken> IssueAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Valida un refresh token (existe, no expirado, no revocado) y lo ROTA: revoca el actual y emite
    /// uno nuevo. Devuelve null si el token no es válido.
    /// </summary>
    Task<RefreshRotation?> ValidateAndRotateAsync(string plainToken, CancellationToken ct = default);

    /// <summary>Revoca un refresh token (logout). No falla si no existe.</summary>
    Task RevokeAsync(string plainToken, CancellationToken ct = default);
}
