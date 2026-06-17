using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Flowbite.Services;
using Mimo.Admin.Api.Sdk;
using Mimo.Admin.App;
using Mimo.Admin.App.Auth;
using Mimo.Admin.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL base de la admin API (configurable; en IIS apuntar al sitio mimo.admin.api).
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// ── Estado y autenticación (SuperAdmin, sin tenant) ────────────────────────────
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ITokenStore, SessionStorageTokenStore>();
builder.Services.AddScoped<MimoAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<MimoAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

// ── Cliente de la admin API (Kiota) con inyección de Bearer ────────────────────
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient("MimoAdminApi", c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("MimoAdminApi");
    var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http)
    {
        BaseUrl = apiBaseUrl
    };
    return new MimoAdminApiClient(adapter);
});

// ── Servicios de dominio ───────────────────────────────────────────────────────
builder.Services.AddScoped<TenantsService>();
builder.Services.AddScoped<PlansService>();
builder.Services.AddScoped<UsageService>();
builder.Services.AddScoped<ConnectorsService>();
builder.Services.AddScoped<PoliciesService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddFlowbite();

await builder.Build().RunAsync();
