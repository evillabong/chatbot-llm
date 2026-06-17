using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Integration;

/// <summary>Metadatos de una API key (nunca incluye la clave en claro).</summary>
public record ApiKeyResponse(
    Guid Id,
    string Name,
    string Prefix,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastUsedAt
);

/// <summary>
/// Respuesta al crear una API key: incluye la clave en claro (<see cref="ApiKey"/>),
/// que se muestra UNA sola vez. No se puede recuperar después.
/// </summary>
public record CreatedApiKeyResponse(
    Guid Id,
    string Name,
    string Prefix,
    string ApiKey,
    DateTime CreatedAt
);

/// <summary>Solicitud para crear una API key.</summary>
public record CreateApiKeyRequest(
    [Required, MaxLength(120)] string Name
);
