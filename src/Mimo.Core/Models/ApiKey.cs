namespace Mimo.Core.Models;

/// <summary>
/// Clave de API para la superficie de interoperabilidad de un tenant.
/// Vive en el esquema global (public) con TenantId porque la autenticación por API key
/// debe resolver el tenant A PARTIR de la clave (antes de conocer el esquema del tenant).
/// Nunca se almacena la clave en claro: solo su hash (SHA-256) y un prefijo para mostrar.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }

    /// <summary>Tenant dueño de la clave.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Nombre descriptivo dado por el administrador (p. ej. "Integración ERP").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Prefijo visible de la clave (p. ej. "mk_Abc123…") para identificarla en la UI.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Hash (SHA-256, hex) de la clave completa. La clave en claro solo se muestra al crearla.</summary>
    public string KeyHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Último uso registrado (se actualiza al autenticar con la clave).</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Fecha de revocación, si aplica.</summary>
    public DateTime? RevokedAt { get; set; }
}
