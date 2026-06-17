using Microsoft.JSInterop;

namespace Mimo.App.Auth;

/// <summary>
/// Almacena el token y el tenant en <c>sessionStorage</c> (se borra al cerrar la pestaña).
/// Se prefiere sessionStorage sobre localStorage para acotar la vida del token al navegador abierto.
/// </summary>
public sealed class SessionStorageTokenStore(IJSRuntime js) : ITokenStore
{
    private const string TokenKey  = "mimo.token";
    private const string TenantKey = "mimo.tenant";

    public async Task<(string? Token, string? TenantSlug)> LoadAsync()
    {
        var token  = await js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
        var tenant = await js.InvokeAsync<string?>("sessionStorage.getItem", TenantKey);
        return (token, tenant);
    }

    public async Task SaveAsync(string token, string tenantSlug)
    {
        await js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        await js.InvokeVoidAsync("sessionStorage.setItem", TenantKey, tenantSlug);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", TenantKey);
    }
}
