namespace Mimo.Core.Models.Configuration;

/// <summary>
/// Opciones de configuración para la emisión de tokens JWT.
/// Se enlaza desde la sección "Jwt" de appsettings.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    /// <summary>Tiempo de vida del token emitido, en minutos.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
