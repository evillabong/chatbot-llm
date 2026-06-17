using System.Security.Claims;

namespace Mimo.Admin.App.Auth;

/// <summary>
/// Estado de sesión del SuperAdmin en memoria. Fuente sincrónica del token para el
/// AuthHeaderHandler y del principal para el AuthStateProvider. Sin tenant (la admin API
/// opera sobre el catálogo global).
/// </summary>
public sealed class SessionState
{
    public string? Token { get; private set; }
    public ClaimsPrincipal Principal { get; private set; } = Anonymous;

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    private static ClaimsPrincipal Anonymous => new(new ClaimsIdentity());

    public void Set(string token)
    {
        Token = token;
        var claims = JwtParser.ParseClaims(token).ToList();
        Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", nameType: "email", roleType: "role"));
    }

    public void Clear()
    {
        Token = null;
        Principal = Anonymous;
    }

    public bool IsExpired(TimeSpan margin = default)
    {
        if (Token is null) return true;
        var exp = JwtParser.GetExpiration(Principal.Claims);
        return exp is null || exp.Value <= DateTimeOffset.UtcNow.Add(margin);
    }
}
