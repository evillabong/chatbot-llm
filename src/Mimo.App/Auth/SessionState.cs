using System.Security.Claims;

namespace Mimo.App.Auth;

/// <summary>
/// Estado de sesión en memoria. Fuente de verdad sincrónica para el token y el tenant
/// que usan el <see cref="AuthHeaderHandler"/> (al construir requests) y el
/// <see cref="MimoAuthStateProvider"/> (al exponer el principal). Se hidrata desde
/// sessionStorage al iniciar y se persiste mediante <see cref="ITokenStore"/>.
/// </summary>
public sealed class SessionState
{
    /// <summary>Token JWT vigente, o null si no hay sesión.</summary>
    public string? Token { get; private set; }

    /// <summary>Slug del tenant al que pertenece la sesión (header X-Tenant-Slug).</summary>
    public string? TenantSlug { get; private set; }

    /// <summary>Principal derivado de los claims del token; anónimo si no hay sesión.</summary>
    public ClaimsPrincipal Principal { get; private set; } = Anonymous;

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    private static ClaimsPrincipal Anonymous => new(new ClaimsIdentity());

    /// <summary>
    /// Fija el tenant antes de autenticar, para que el login (anónimo) viaje con el
    /// header X-Tenant-Slug requerido por la resolución de tenant del backend.
    /// </summary>
    public void SetPendingTenant(string tenantSlug) => TenantSlug = tenantSlug;

    public void Set(string token, string tenantSlug)
    {
        Token      = token;
        TenantSlug = tenantSlug;
        var claims = JwtParser.ParseClaims(token).ToList();
        Principal  = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "email", roleType: "role"));
    }

    public void Clear()
    {
        Token      = null;
        TenantSlug = null;
        Principal  = Anonymous;
    }

    /// <summary>True si el token expiró (o está por expirar en el margen indicado).</summary>
    public bool IsExpired(TimeSpan margin = default)
    {
        if (Token is null) return true;
        var exp = JwtParser.GetExpiration(Principal.Claims);
        return exp is null || exp.Value <= DateTimeOffset.UtcNow.Add(margin);
    }
}
