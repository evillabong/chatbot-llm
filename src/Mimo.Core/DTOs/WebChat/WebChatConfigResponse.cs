namespace Mimo.Core.DTOs.WebChat;

/// <summary>
/// Configuración pública que el widget embebible del WebChat necesita para presentarse: nombre a
/// mostrar, mensaje de bienvenida y branding. NO expone secretos ni configuración interna del tenant.
/// </summary>
public record WebChatConfigResponse(
    string Name,
    string WelcomeMessage,
    string? PrimaryColor,
    string? LogoUrl
);
