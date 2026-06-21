using System.ComponentModel.DataAnnotations;
using Mimo.Core.Enums;

namespace Mimo.Core.DTOs.Sales;

/// <summary>Oportunidad de venta (#26).</summary>
public record OpportunityResponse(
    Guid Id,
    string Title,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    OpportunityStage Stage,
    decimal Amount,
    Guid? ConversationId,
    Guid? AssignedAgentId,
    string? Notes,
    string? ExternalCrmId,
    DateTime? LastSyncedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ClosedAt
);

/// <summary>Resultado de sincronizar una oportunidad con el CRM externo (#26).</summary>
public record CrmSyncResultResponse(bool Success, string? ExternalId, string? Message);

/// <summary>Entrada de la bitácora de sincronización con CRM externo (#26).</summary>
public record CrmSyncLogResponse(
    Guid Id,
    string Provider,
    bool Success,
    string? ExternalId,
    string? Message,
    DateTime CreatedAt
);

/// <summary>Crea una oportunidad de venta.</summary>
public record CreateOpportunityRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(160)] string? ContactName = null,
    [MaxLength(160)] string? ContactEmail = null,
    [MaxLength(40)] string? ContactPhone = null,
    decimal Amount = 0,
    Guid? ConversationId = null,
    Guid? AssignedAgentId = null,
    string? Notes = null
);

/// <summary>Actualiza una oportunidad de venta, incluida su etapa.</summary>
public record UpdateOpportunityRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(160)] string? ContactName,
    [MaxLength(160)] string? ContactEmail,
    [MaxLength(40)] string? ContactPhone,
    OpportunityStage Stage,
    decimal Amount,
    Guid? AssignedAgentId,
    string? Notes
);
