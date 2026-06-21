using Mimo.Core.Enums;

namespace Mimo.Core.Models;

/// <summary>
/// Regla de automatización (#24, motor "evento → condición → acción"). Vive en el esquema del tenant.
/// Ante un evento de dominio (p. ej. <c>conversation.created</c>), si se cumplen las condiciones
/// (AND sobre campos del evento), ejecuta una acción. Por ahora la acción es "crear tarea". Aislada
/// por organización.
/// </summary>
public class AutomationRule
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Evento disparador (uno de <see cref="Webhooks.WebhookEventTypes"/>).</summary>
    public string TriggerEvent { get; set; } = string.Empty;

    /// <summary>Condiciones serializadas (JSON: lista de {field, value}). "[]" = sin condiciones.</summary>
    public string ConditionsJson { get; set; } = "[]";

    public AutomationActionType ActionType { get; set; } = AutomationActionType.CreateTask;

    /// <summary>Título de la tarea a crear (admite marcadores {campo} del evento).</summary>
    public string ActionTaskTitle { get; set; } = string.Empty;

    /// <summary>Responsable opcional de la tarea creada.</summary>
    public Guid? ActionAssignedAgentId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
