using Microsoft.AspNetCore.DataProtection;
using Mimo.Infrastructure;
using Mimo.Worker;

// Host de procesos en segundo plano de MIMO (ADR 0017). Separado de Mimo.Api para poder escalar y
// desplegar los workers de forma independiente del tráfico HTTP. Solo aloja los workers que NO
// dependen del proceso web (BD/HTTP): la entrega de webhooks y el cierre por inactividad. El worker
// de notificación de cola se queda en Mimo.Api porque empuja por SignalR (IHubContext).
var builder = Host.CreateApplicationBuilder(args);

// Permite instalarse como Servicio de Windows en producción; en consola corre igual (dev).
builder.Services.AddWindowsService(options => options.ServiceName = "MIMO Worker");

// Infraestructura mínima para los workers: DbContexts (global + tenant) y cifrado de secretos.
builder.Services.AddWorkerInfrastructure(builder.Configuration);

// Data Protection con el MISMO anillo de llaves que las APIs (mismo ApplicationName y ubicación):
// el worker debe descifrar el secreto HMAC que cifró Mimo.Api al crear la suscripción (ADR 0010).
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MIMO", "dp-keys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("MIMO")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

// HttpClient para la entrega de webhooks salientes (timeout acotado por intento).
builder.Services.AddHttpClient(WebhookDeliveryWorker.HttpClientName,
    c => c.Timeout = TimeSpan.FromSeconds(10));

// Workers alojados.
builder.Services.AddHostedService<WebhookDeliveryWorker>();
builder.Services.AddHostedService<InactivityTimeoutWorker>();

var host = builder.Build();
host.Run();
