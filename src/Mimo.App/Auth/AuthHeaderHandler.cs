namespace Mimo.App.Auth;

/// <summary>
/// Inyecta en cada request el token Bearer y el header X-Tenant-Slug a partir del
/// <see cref="SessionState"/> en memoria. Mantener este handler como único punto de
/// inserción de credenciales permite que el cliente Kiota use autenticación anónima.
/// </summary>
public sealed class AuthHeaderHandler(SessionState session) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(session.Token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

        if (!string.IsNullOrEmpty(session.TenantSlug))
            request.Headers.TryAddWithoutValidation("X-Tenant-Slug", session.TenantSlug);

        return base.SendAsync(request, cancellationToken);
    }
}
