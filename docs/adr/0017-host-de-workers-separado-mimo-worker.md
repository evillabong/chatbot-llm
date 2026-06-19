# 0017. Host de workers separado (Mimo.Worker)

- Estado: Aceptado
- Fecha: 2026-06-18
- Decisores: evill

## Contexto

Los procesos de background vivían como `IHostedService` dentro de `Mimo.Api`: `QueueNotificationWorker`,
`InactivityTimeoutWorker` y, tras el corte 3 de Interoperabilidad, `WebhookDeliveryWorker`
([ADR 0016](0016-webhooks-salientes-con-firma-hmac-y-reintentos.md)). Alojar workers en el host web es
idiomático en .NET y suficiente con **una sola instancia**, pero tiene límites:

- **Escalado acoplado:** no se puede escalar el throughput de background sin escalar también el HTTP.
- **Duplicación al escalar horizontalmente:** con N réplicas de la API, cada una corre su worker; el de
  webhooks leería N veces la misma cola por tenant → **entregas duplicadas** y carreras.
- **Aislamiento:** un worker pesado compite por recursos con las peticiones; un reinicio por despliegue
  de la API interrumpe el trabajo de fondo.

## Decisión

- **Nuevo proyecto `Mimo.Worker`** (Generic Host, `Microsoft.NET.Sdk.Worker`) que se despliega y escala
  de forma independiente. Puede correr como **Servicio de Windows** (`AddWindowsService`).
- **Se mueven solo los workers de BD/HTTP**, que no dependen del proceso web:
  `WebhookDeliveryWorker` e `InactivityTimeoutWorker`.
- **`QueueNotificationWorker` se queda en `Mimo.Api`** porque empuja por SignalR
  (`IHubContext<TicketHub>`), atado a las conexiones de ese proceso. Sacarlo exigiría un backplane de
  SignalR (Redis); no se justifica hoy.
- **Registro de infraestructura mínimo** (`AddWorkerInfrastructure`): solo DbContexts (global + tenant
  por factory) y `ISecretProtector`. No registra orquestador, conectores de canal ni notificaciones
  (dependen del proceso web; intentar `AddInfrastructure` completo falla la validación del contenedor
  por dependencias ausentes como `INotificationService`).
- **Mismo anillo de Data Protection que las APIs** (`SetApplicationName("MIMO")` + misma ruta de
  llaves): el worker debe **descifrar** el secreto HMAC que `Mimo.Api` cifró al crear la suscripción
  ([ADR 0010](0010-cifrado-de-api-keys-de-conectores-de-ia.md)).

## Consecuencias

### Positivas

- **Verificado E2E (multiproceso):** con `Mimo.Api` y `Mimo.Worker` como procesos separados, una
  suscripción creada en la API se entrega desde el worker con **firma HMAC válida** (el worker descifró
  el secreto del anillo compartido) y queda `Delivered` (200). La API ya solo aloja el worker de cola.
- Background escalable y desplegable aparte del tráfico HTTP; un reinicio de la API no interrumpe la
  entrega de webhooks.

### Negativas / Costos

- **Un despliegue más** (un Servicio de Windows además de los sitios IIS): nuevo `deploy-worker.ps1`.
- **Aún una sola instancia del worker.** Correr varias réplicas del propio worker reintroduciría la
  duplicación; antes de escalarlo hará falta **lock/leader-election** sobre la cola de entregas
  (pendiente, registrado en `docs/pendings`).
- El worker depende del anillo de llaves compartido: si la ruta/`ApplicationName` no coincide, no podrá
  descifrar los secretos (mismo riesgo operativo ya documentado entre las dos APIs).
