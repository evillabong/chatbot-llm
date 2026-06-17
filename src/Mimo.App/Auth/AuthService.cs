using Microsoft.Kiota.Abstractions;
using Mimo.ApiClient.MimoApi;
using Mimo.ApiClient.MimoApi.Models;

namespace Mimo.App.Auth;

/// <summary>
/// Orquesta el inicio y cierre de sesión: fija el tenant, llama al endpoint de login,
/// persiste el token y actualiza el estado de autenticación de Blazor.
/// </summary>
public sealed class AuthService(
    MimoApiClient api,
    SessionState session,
    ITokenStore store,
    MimoAuthStateProvider authState)
{
    public sealed record LoginResult(bool Success, string? Error);

    public async Task<LoginResult> LoginAsync(string tenantSlug, string email, string password, CancellationToken ct = default)
    {
        // El backend resuelve el tenant por el header X-Tenant-Slug; debe viajar en el login.
        session.SetPendingTenant(tenantSlug);

        try
        {
            var response = await api.Auth.Login.PostAsync(
                new LoginRequest { Email = email, Password = password }, cancellationToken: ct);

            if (response?.AccessToken is null)
            {
                session.Clear();
                return new LoginResult(false, "Respuesta de autenticación inválida.");
            }

            session.Set(response.AccessToken, tenantSlug);
            await store.SaveAsync(response.AccessToken, tenantSlug);
            authState.NotifyChanged();
            return new LoginResult(true, null);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 401)
        {
            session.Clear();
            return new LoginResult(false, "Credenciales o tenant incorrectos.");
        }
        catch (Exception ex)
        {
            session.Clear();
            return new LoginResult(false, $"No se pudo conectar con el servidor: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        session.Clear();
        await store.ClearAsync();
        authState.NotifyChanged();
    }
}
