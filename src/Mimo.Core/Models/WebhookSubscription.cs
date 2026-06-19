namespace Mimo.Core.Models;

/// <summary>
/// Suscripción de webhook saliente de un tenant: una URL del cliente que recibe notificaciones
/// firmadas cuando ocurren ciertos eventos en la plataforma.
///
/// Vive en el esquema del tenant (a diferencia de <see cref="ApiKey"/>, que es global): cuando un
/// evento ocurre el tenant ya está resuelto, así que el aislamiento por esquema basta y no hace falta
/// una columna TenantId.
/// </summary>
public class WebhookSubscription
{
    public Guid Id { get; set; }

    /// <summary>Nombre descriptivo dado por el administrador (p. ej. "ERP producción").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL HTTPS del cliente a la que se entregan los eventos (POST).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secreto de firma HMAC, cifrado en reposo (ISecretProtector, ADR 0010). Se descifra solo al
    /// firmar la entrega; la plataforma lo necesita en claro para firmar, por eso se cifra (no se
    /// hashea). Se muestra al administrador UNA sola vez al crear la suscripción.
    /// </summary>
    public string SecretProtected { get; set; } = string.Empty;

    /// <summary>Eventos suscritos (nombres de <see cref="Webhooks.WebhookEventTypes"/>).</summary>
    public List<string> Events { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }
}
