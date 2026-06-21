namespace Mimo.Core.Sales;

/// <summary>Resultado de sincronizar una oportunidad con un CRM externo (#26).</summary>
public sealed record CrmSyncResult(bool Success, string? ExternalId, string? Message);
