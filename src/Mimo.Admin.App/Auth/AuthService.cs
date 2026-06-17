using Microsoft.Kiota.Abstractions;
using Mimo.Admin.Api.Sdk;
using Mimo.Admin.Api.Sdk.Models;

namespace Mimo.Admin.App.Auth;

/// <summary>
/// Inicio y cierre de sesión del SuperAdmin contra Mimo.Admin.Api (sin tenant).
/// </summary>
public sealed class AuthService(
    MimoAdminApiClient api,
    SessionState session,
    ITokenStore store,
    MimoAuthStateProvider authState)
{
    public sealed record LoginResult(bool Success, string? Error);

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var response = await api.Auth.Login.PostAsync(
                new LoginRequest { Email = email, Password = password }, cancellationToken: ct);

            if (response?.AccessToken is null)
            {
                session.Clear();
                return new LoginResult(false, "Respuesta de autenticación inválida.");
            }

            session.Set(response.AccessToken);
            await store.SaveAsync(response.AccessToken);
            authState.NotifyChanged();
            return new LoginResult(true, null);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 401)
        {
            session.Clear();
            return new LoginResult(false, "Credenciales incorrectas.");
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
