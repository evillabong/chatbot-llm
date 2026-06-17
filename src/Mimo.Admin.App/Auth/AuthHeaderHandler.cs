namespace Mimo.Admin.App.Auth;

/// <summary>
/// Inyecta el token Bearer en cada request a la admin API. No envía X-Tenant-Slug:
/// la administración opera sobre el catálogo global, no sobre un tenant.
/// </summary>
public sealed class AuthHeaderHandler(SessionState session) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(session.Token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
