namespace Mimo.App.Auth;

/// <summary>Persistencia del token y tenant de la sesión (sessionStorage del navegador).</summary>
public interface ITokenStore
{
    Task<(string? Token, string? TenantSlug)> LoadAsync();
    Task SaveAsync(string token, string tenantSlug);
    Task ClearAsync();
}
