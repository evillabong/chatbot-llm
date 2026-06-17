using Microsoft.AspNetCore.Components.Authorization;

namespace Mimo.App.Auth;

/// <summary>
/// Expone el estado de autenticación a Blazor (AuthorizeView, AuthorizeRouteView).
/// La primera consulta hidrata el <see cref="SessionState"/> desde sessionStorage.
/// </summary>
public sealed class MimoAuthStateProvider(SessionState session, ITokenStore store) : AuthenticationStateProvider
{
    private bool _hydrated;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_hydrated)
        {
            _hydrated = true;
            var (token, tenant) = await store.LoadAsync();
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tenant))
            {
                session.Set(token, tenant);
                if (session.IsExpired())
                {
                    session.Clear();
                    await store.ClearAsync();
                }
            }
        }

        return new AuthenticationState(session.Principal);
    }

    /// <summary>Notifica a Blazor que la sesión cambió (tras login/logout).</summary>
    public void NotifyChanged() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(session.Principal)));
}
