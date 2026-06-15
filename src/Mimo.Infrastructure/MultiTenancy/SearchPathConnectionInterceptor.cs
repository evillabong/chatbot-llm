using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.MultiTenancy;

/// <summary>
/// Fija el search_path de PostgreSQL en cada apertura de conexión según el esquema del
/// tenant resuelto para la petición (<see cref="ITenantSchemaProvider"/>).
///
/// Resuelve el problema del pooling: aplicar el search_path una sola vez no basta porque
/// cada operación de EF puede tomar una conexión distinta del pool. Aplicándolo en cada
/// apertura, todas las consultas de la petición operan sobre el esquema del tenant.
///
/// El nombre de esquema lo construye el middleware con un patrón seguro ([a-z0-9_]),
/// por lo que es seguro interpolarlo (los identificadores no pueden parametrizarse).
/// </summary>
public class SearchPathConnectionInterceptor(ITenantSchemaProvider schemaProvider) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await ApplyAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void Apply(DbConnection connection)
    {
        var schema = schemaProvider.Schema;
        if (string.IsNullOrEmpty(schema)) return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET search_path TO \"{schema}\", public";
        cmd.ExecuteNonQuery();
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken ct)
    {
        var schema = schemaProvider.Schema;
        if (string.IsNullOrEmpty(schema)) return;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET search_path TO \"{schema}\", public";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
