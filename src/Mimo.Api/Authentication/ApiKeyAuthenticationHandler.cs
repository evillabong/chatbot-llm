using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mimo.Core.Interfaces;
using Mimo.Infrastructure.Data;

namespace Mimo.Api.Authentication;

/// <summary>
/// Esquema de autenticación por API key para la superficie de interoperabilidad (ADR 0015).
///
/// El llamante presenta su clave en la cabecera <c>X-Api-Key</c>. La clave se hashea (SHA-256) y se
/// busca en <c>public.api_keys</c>; si está activa y su tenant también, se emite un principal que
/// lleva el claim <c>tenant_slug</c>. A partir de ahí <see cref="Middleware.TenantResolutionMiddleware"/>
/// fija el esquema del tenant igual que con el JWT: la API key NO viaja por <c>X-Tenant-Slug</c>, el
/// tenant se deriva exclusivamente de la propia clave.
///
/// Importante: ningún endpoint de gestión (JWT/TenantAdmin) acepta este esquema; solo los endpoints
/// que declaran la política <see cref="Mimo.Core.Authorization.MimoAuthorization.Policies.Integration"/>.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    GlobalDbContext db,
    IApiKeyService apiKeys)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Nombre del esquema de autenticación.</summary>
    public const string SchemeName = "ApiKey";

    /// <summary>Cabecera por la que se presenta la clave.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>Claims emitidos por el esquema (consumidos por el pipeline y los endpoints).</summary>
    public static class Claims
    {
        public const string TenantSlug = "tenant_slug";
        public const string TenantId   = "tenant_id";
        public const string ApiKeyId   = "api_key_id";
        public const string ApiKeyName = "api_key_name";
        public const string AuthMethod = "auth_method";
        public const string ApiKeyValue = "api_key";
    }

    /// <summary>El "último uso" solo se reescribe si pasó este intervalo, para no escribir por petición.</summary>
    private static readonly TimeSpan LastUsedThrottle = TimeSpan.FromMinutes(1);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Sin cabecera: este esquema no aplica (permite que otros esquemas/anónimo decidan).
        if (!Request.Headers.TryGetValue(HeaderName, out var values))
            return AuthenticateResult.NoResult();

        var presented = values.ToString().Trim();
        if (string.IsNullOrEmpty(presented))
            return AuthenticateResult.NoResult();

        var hash = apiKeys.Hash(presented);

        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == hash, Context.RequestAborted);

        if (key is null || !key.IsActive)
            return AuthenticateResult.Fail("API key inválida o revocada.");

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == key.TenantId, Context.RequestAborted);

        if (tenant is null || !tenant.IsActive)
            return AuthenticateResult.Fail("La organización de la API key no está activa.");

        // Registrar último uso de forma best-effort y throttled (evita un UPDATE por petición).
        var now = DateTime.UtcNow;
        if (key.LastUsedAt is null || now - key.LastUsedAt.Value > LastUsedThrottle)
        {
            key.LastUsedAt = now;
            try { await db.SaveChangesAsync(Context.RequestAborted); }
            catch (Exception ex) { Logger.LogWarning(ex, "No se pudo actualizar LastUsedAt de la API key {KeyId}.", key.Id); }
        }

        var claims = new[]
        {
            new Claim(Claims.TenantSlug, tenant.Slug),
            new Claim(Claims.TenantId,   tenant.Id.ToString()),
            new Claim(Claims.ApiKeyId,   key.Id.ToString()),
            new Claim(Claims.ApiKeyName, key.Name),
            new Claim(Claims.AuthMethod, Claims.ApiKeyValue),
            new Claim(ClaimTypes.Name,   key.Name)
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
