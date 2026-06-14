namespace Mimo.Core.Enums;

/// <summary>
/// Tipo de operación de IA, usado para medición de uso y reportes.
/// </summary>
public enum AiOperation
{
    /// <summary>Completion de chat (respuesta del bot).</summary>
    Chat,

    /// <summary>Generación de embedding (indexación o búsqueda semántica).</summary>
    Embedding
}
