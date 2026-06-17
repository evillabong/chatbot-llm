using Microsoft.JSInterop;

namespace Mimo.Admin.App.Auth;

/// <summary>Almacena el token en <c>sessionStorage</c> (se borra al cerrar la pestaña).</summary>
public sealed class SessionStorageTokenStore(IJSRuntime js) : ITokenStore
{
    private const string TokenKey = "mimo.admin.token";

    public async Task<string?> LoadAsync() =>
        await js.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);

    public async Task SaveAsync(string token) =>
        await js.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);

    public async Task ClearAsync() =>
        await js.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
}
