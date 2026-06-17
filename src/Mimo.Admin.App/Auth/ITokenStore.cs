namespace Mimo.Admin.App.Auth;

/// <summary>Persistencia del token de la sesión del SuperAdmin (sessionStorage).</summary>
public interface ITokenStore
{
    Task<string?> LoadAsync();
    Task SaveAsync(string token);
    Task ClearAsync();
}
