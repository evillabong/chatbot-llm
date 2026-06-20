using System.ComponentModel.DataAnnotations;

namespace Mimo.Core.DTOs.Chatbot;

/// <summary>Solicitud para crear un flujo guiado del chatbot por opciones (#27).</summary>
public record CreateChatbotFlowRequest(
    [Required, MaxLength(120)] string Name,
    [Required] FlowDefinition Definition,
    bool Activate = true
);

/// <summary>Metadatos de un flujo guiado (sin la definición completa).</summary>
public record ChatbotFlowResponse(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt
);
