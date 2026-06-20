namespace Mimo.Core.Models;

/// <summary>
/// Refresh token de un funcionario (#9). Vive en el esquema del tenant. Se guarda solo el hash
/// (SHA-256) del token, nunca el valor en claro. Permite renovar el access token sin re-login y
/// revocar sesiones (logout / rotación).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>Funcionario dueño del token.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Hash (SHA-256, hex) del token; el valor en claro solo lo tiene el cliente.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de revocación (logout o rotación). Null = activo.</summary>
    public DateTime? RevokedAt { get; set; }
}
