namespace Mimo.Core.Models;

/// <summary>
/// Registro de auditoría de cada transferencia de ticket entre roles o funcionarios.
/// </summary>
public class TransferRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid? FromRoleId { get; set; }
    public Guid? FromAgentId { get; set; }
    public Guid ToRoleId { get; set; }
    public Guid? ToAgentId { get; set; }

    /// <summary>Funcionario que ejecutó la transferencia.</summary>
    public Guid TransferredBy { get; set; }

    /// <summary>Motivo declarado de la transferencia.</summary>
    public string? Reason { get; set; }

    /// <summary>Indica si fue una transferencia parcial del historial.</summary>
    public bool IsPartial { get; set; } = false;

    /// <summary>Nota de contexto enviada al rol/funcionario destino.</summary>
    public string? ContextNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
