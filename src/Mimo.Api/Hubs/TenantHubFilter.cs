using Microsoft.AspNetCore.SignalR;
using Mimo.Api.Middleware;
using Mimo.Core.Interfaces;

namespace Mimo.Api.Hubs;

/// <summary>
/// Fija el esquema del tenant (search_path) en cada invocación de hub y al conectar.
///
/// El <see cref="TenantResolutionMiddleware"/> solo corre en el handshake HTTP, no por cada
/// llamada de método del hub; y <see cref="ITenantSchemaProvider"/> es scoped por invocación.
/// Sin esto, las operaciones de BD dentro de los hubs (persistir mensajes, resolver tickets)
/// no apuntarían al esquema correcto. El filtro reutiliza el esquema que el middleware ya
/// resolvió y guardó en el HttpContext del handshake (accesible vía Context.GetHttpContext()).
/// </summary>
public sealed class TenantHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ApplyTenantSchema(invocationContext.Context, invocationContext.ServiceProvider);
        return await next(invocationContext);
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        ApplyTenantSchema(context.Context, context.ServiceProvider);
        return next(context);
    }

    private static void ApplyTenantSchema(HubCallerContext context, IServiceProvider services)
    {
        var schema = context.GetHttpContext()?.GetTenantSchema();
        if (!string.IsNullOrEmpty(schema))
            services.GetRequiredService<ITenantSchemaProvider>().Schema = schema;
    }
}
