using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Flowbite.Services;
using Mimo.App;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Servicios de Flowbite (Floating UI, toasts, modal, drawer, foco, portapapeles).
builder.Services.AddFlowbite();

await builder.Build().RunAsync();
