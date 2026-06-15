namespace Mimo.Core.Interfaces;

/// <summary>
/// Portador (scoped, por petición) del esquema PostgreSQL del tenant resuelto.
/// El middleware de resolución de tenant lo establece y un interceptor de conexión
/// lo usa para fijar el search_path en cada apertura de conexión.
///
/// Este enfoque es fiable con pooling de conexiones: a diferencia de un único
/// `SET search_path`, garantiza que TODA conexión abierta durante la petición opere
/// sobre el esquema correcto.
/// </summary>
public interface ITenantSchemaProvider
{
    /// <summary>Nombre del esquema del tenant (ej: "tenant_acme"). Null si aún no se resolvió.</summary>
    string? Schema { get; set; }
}
