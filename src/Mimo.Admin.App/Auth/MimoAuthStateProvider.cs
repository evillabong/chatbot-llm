using Microsoft.AspNetCore.Components.Authorization;

namespace Mimo.Admin.App.Auth;

/// <summary>Expone el estado de autenticación del SuperAdmin a Blazor. Hidrata desde sessionStorage.</summary>
public sealed class MimoAuthStateProvider(SessionState session, ITokenStore store) : AuthenticationStateProvider
{
    private bool _hydrated;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_hydrated)
        {
            _hydrated = true;
            var token = await store.LoadAsync();
            if (!string.IsNullOrEmpty(token))
            {
                session.Set(token);
                if (session.IsExpired())
                {
                    session.Clear();
                    await store.ClearAsync();
                }
            }
        }

        return new AuthenticationState(session.Principal);
    }

    public void NotifyChanged() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(session.Principal)));
}
