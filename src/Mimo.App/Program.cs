using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Flowbite.Services;
using Mimo.Api.Sdk;
using Mimo.App;
using Mimo.App.Auth;
using Mimo.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── URL base de la API (configurable; en IIS apuntar al sitio mimo.api) ────────
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// ── Estado y autenticación ─────────────────────────────────────────────────────
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ITokenStore, SessionStorageTokenStore>();
builder.Services.AddScoped<MimoAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<MimoAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

// ── Cliente de API (Kiota) con inyección de Bearer + X-Tenant-Slug ─────────────
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient("MimoApi", c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("MimoApi");
    // El Bearer lo añade AuthHeaderHandler; Kiota usa autenticación anónima.
    var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http)
    {
        BaseUrl = apiBaseUrl
    };
    return new MimoApiClient(adapter);
});

// ── Servicios de dominio (sobre el cliente Kiota) ──────────────────────────────
builder.Services.AddScoped<AgentsService>();
builder.Services.AddScoped<RolesService>();
builder.Services.AddScoped<DocumentsService>();
builder.Services.AddScoped<TenantConfigService>();
builder.Services.AddScoped<ConsoleService>();

// HttpClient genérico (recursos estáticos de la propia app).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Servicios de Flowbite (Floating UI, toasts, modal, drawer, foco, portapapeles).
builder.Services.AddFlowbite();

await builder.Build().RunAsync();
