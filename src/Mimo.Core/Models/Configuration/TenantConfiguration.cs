using Mimo.Core.Enums;

namespace Mimo.Core.Models.Configuration;

/// <summary>
/// Parámetros de comportamiento configurables por el TenantAdmin.
/// Se persiste como JSON en la columna Configuration de la tabla Tenants.
/// </summary>
public class TenantConfiguration
{
    public CustomerAttentionConfig CustomerAttention { get; set; } = new();
    public SessionAssignmentConfig SessionAssignment { get; set; } = new();
    public TransfersConfig Transfers { get; set; } = new();
    public AgentInterventionConfig AgentIntervention { get; set; } = new();
    public SatisfactionSurveyConfig SatisfactionSurvey { get; set; } = new();
    public UnattendedSessionsConfig UnattendedSessions { get; set; } = new();
    public BusinessHoursConfig BusinessHours { get; set; } = new();
    public ChannelCustomizationConfig ChannelCustomization { get; set; } = new();
}

/// <summary>Parámetros de atención al ciudadano.</summary>
public class CustomerAttentionConfig
{
    /// <summary>Tiempo máximo en cola en minutos. Null = sin límite.</summary>
    public int? MaxQueueWaitTimeMinutes { get; set; }

    public string QueueWaitMessage { get; set; } = "Estamos buscando un funcionario disponible para atenderte.";
    public string TransferMessage { get; set; } = "Tu consulta está siendo transferida a otro departamento.";

    /// <summary>Minutos sin mensajes antes de cerrar la sesión. Null = nunca.</summary>
    public int? InactivityTimeoutMinutes { get; set; } = 30;
}

/// <summary>Parámetros de asignación de sesiones a funcionarios.</summary>
public class SessionAssignmentConfig
{
    public AssignmentMode AssignmentMode { get; set; } = AssignmentMode.Manual;
    public bool NotifyPendingByEmail { get; set; } = false;
    public int NotificationFrequencyMinutes { get; set; } = 5;
    public int MaxConcurrentSessionsPerAgent { get; set; } = 5;
}

/// <summary>Parámetros de transferencia de sesiones.</summary>
public class TransfersConfig
{
    public bool AllowPartialTransfer { get; set; } = false;
    public bool RequireCustomerConfirmation { get; set; } = false;
    public bool EnableInternalChat { get; set; } = true;

    /// <summary>Límite de transferencias por sesión. Null = sin límite.</summary>
    public int? MaxTransfersPerSession { get; set; }
}

/// <summary>Parámetros de intervención de funcionarios en sesiones del bot.</summary>
public class AgentInterventionConfig
{
    public bool AllowIntervention { get; set; } = true;
    public bool CanViewBotConversations { get; set; } = true;
    public bool CanViewOtherRolesSessions { get; set; } = false;

    /// <summary>Número de mensajes sin resolución antes de sugerir intervención humana.</summary>
    public int InterventionSuggestionThreshold { get; set; } = 5;
}

/// <summary>Parámetros de encuesta de satisfacción.</summary>
public class SatisfactionSurveyConfig
{
    public bool SurveyEnabled { get; set; } = true;
    public bool SurveyRequired { get; set; } = false;
    public bool AllowObservations { get; set; } = true;
    public bool ReopenOnLowRating { get; set; } = false;

    /// <summary>Calificación mínima para no reabrir el ticket (1-5).</summary>
    public int ReopenRatingThreshold { get; set; } = 3;
}

/// <summary>Parámetros de sesiones no atendidas.</summary>
public class UnattendedSessionsConfig
{
    /// <summary>Minutos sin que nadie tome la sesión antes de aplicar la acción. Null = sin límite.</summary>
    public int? UnattendedTimeoutMinutes { get; set; } = 15;

    public TimeoutAction TimeoutAction { get; set; } = TimeoutAction.MarkAsUnattended;
    public string TimeoutCustomerMessage { get; set; } = "En este momento no hay funcionarios disponibles. Por favor intenta más tarde.";
    public bool AutoRetry { get; set; } = false;
}

/// <summary>Parámetros de horario de atención.</summary>
public class BusinessHoursConfig
{
    public bool BusinessHoursEnabled { get; set; } = false;
    public TimeOnly StartTime { get; set; } = new TimeOnly(8, 0);
    public TimeOnly EndTime { get; set; } = new TimeOnly(18, 0);

    /// <summary>Días de atención: 1=Lunes, 7=Domingo.</summary>
    public int[] WorkingDays { get; set; } = [1, 2, 3, 4, 5];

    public string OutOfHoursMessage { get; set; } = "Nuestro horario de atención es de lunes a viernes de 8:00 a 18:00.";
}

/// <summary>Personalización visual y credenciales por canal.</summary>
public class ChannelCustomizationConfig
{
    // WebChat
    public string? PrimaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public string WelcomeMessage { get; set; } = "Hola, ¿en qué te podemos ayudar?";

    // Facebook
    public string? FacebookPageId { get; set; }

    // WhatsApp
    public string? WhatsAppPhoneNumber { get; set; }

    // Telegram
    public string? TelegramBotToken { get; set; }

    // Instagram
    public string? InstagramAccountId { get; set; }
}
