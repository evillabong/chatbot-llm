namespace Mimo.Core.Enums;

/// <summary>
/// Etapa de una oportunidad de venta en el pipeline (#26). Won/Lost son terminales (cierran la oportunidad).
/// </summary>
public enum OpportunityStage
{
    /// <summary>Nueva, sin calificar.</summary>
    New = 0,

    /// <summary>Calificada (hay interés/encaje).</summary>
    Qualified = 1,

    /// <summary>Propuesta enviada.</summary>
    Proposal = 2,

    /// <summary>Ganada (cerrada con éxito).</summary>
    Won = 3,

    /// <summary>Perdida (cerrada sin éxito).</summary>
    Lost = 4
}
