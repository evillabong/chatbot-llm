# 0016. Webhooks salientes con firma HMAC y reintentos

- Estado: Aceptado
- Fecha: 2026-06-18
- Decisores: evill

## Contexto

El módulo de Interoperabilidad ya permite a un sistema externo **consultar** la plataforma (API de
integración por API key, [ADR 0015](0015-autenticacion-por-api-key-y-api-de-integracion.md)). Falta
el sentido inverso: que la plataforma **notifique** a los sistemas del cliente cuando algo ocurre
(se creó una conversación, se asignó/resolvió un ticket, se registró una encuesta), sin que el cliente
tenga que sondear. Esto es el corte 3 de [pendings #29](../pendings/README.md).

Requisitos: suscripción por organización a eventos concretos, entrega **firmada** (para que el
receptor verifique autenticidad e integridad), **reintentos** ante fallos transitorios y una
**bitácora** de entregas. Debe ser aislado por tenant y no debe poder tumbar la operación que originó
el evento.

## Decisión

- **Modelo en el esquema del tenant** (a diferencia de las API keys, que son globales): cuando un
  evento ocurre el tenant ya está resuelto, así que el aislamiento por esquema basta.
  `webhook_subscriptions` (URL, eventos como `text[]`, secreto **cifrado** en reposo) y
  `webhook_deliveries` (cola + bitácora: payload exacto, estado, intentos, próximo reintento, error).
- **Secreto cifrado, no hasheado.** La plataforma necesita el secreto en claro para firmar, así que se
  cifra con `ISecretProtector` (Data Protection, [ADR 0010](0010-cifrado-de-api-keys-de-conectores-de-ia.md))
  y se muestra al administrador **una sola vez** al crear la suscripción.
- **Publicación desacostumbrada del envío.** `IWebhookPublisher` (best-effort, nunca lanza) crea una
  entrega encolada por cada suscripción activa al evento, dentro de la misma petición que lo originó.
  El **envío HTTP** lo hace `WebhookDeliveryWorker` (background), de modo que la latencia y los fallos
  de red no afectan la operación de negocio.
- **Firma HMAC-SHA256** sobre el cuerpo exacto, en la cabecera `X-Mimo-Signature: sha256=<hex>`, más
  `X-Mimo-Event`, `X-Mimo-Delivery` y `X-Mimo-Timestamp`. El cuerpo es un envelope estable
  `{ event, occurredAt, data }`.
- **Reintentos con backoff** (1m → 5m → 15m → 1h), hasta 5 intentos; agotados, la entrega queda en
  estado `Failed` (terminal). Respuesta 2xx → `Delivered`.
- **Gestión bajo JWT/TenantAdmin** en `/integration/webhooks` (crear/listar/revocar + bitácora de
  entregas), igual criterio que las API keys. Distinto de los webhooks **entrantes** de canales
  (`/webhooks/incoming`), donde la plataforma es el receptor.
- **Migración de esquemas de tenants existentes al arrancar** (`TenantSchemaMigrator`). El
  aprovisionamiento migra cada tenant al crearlo, pero una migración nueva de `TenantDbContext`
  (como la de webhooks) no llegaba sola a los tenants previos. Ahora, tras migrar el esquema global,
  la API migra los esquemas de todos los tenants activos (idempotente).

## Consecuencias

### Positivas

- Entrega fiable y auditable: **verificado E2E** — `conversation.created` se entrega con firma HMAC
  válida (recalculada por el receptor) y queda `Delivered` (HTTP 200); un endpoint inaccesible deja la
  entrega `Pending` con `attempt_count`, error y próximo reintento programado; revocar desactiva.
- El secreto nunca se expone tras la creación; el listado no lo incluye.
- `TenantSchemaMigrator` cierra una brecha real de despliegue multi-tenant que aplica a **toda**
  migración futura de tenant, no solo a webhooks.

### Negativas / Costos

- El worker recorre los tenants activos por tick; con muchos tenants conviene paralelizar o priorizar
  por actividad (hoy es secuencial, lote de 50 entregas por tenant).
- El backoff vive en el worker (no configurable por suscripción todavía).
- Migrar todos los esquemas al arrancar añade tiempo de arranque proporcional al número de tenants
  (mitigable moviéndolo a un paso de despliegue dedicado si crece).

## Nota de implementación (bug de multi-tenancy en workers)

Al integrar el worker se halló que los workers que usan `IDbContextFactory<TenantDbContext>` (sin el
`SearchPathConnectionInterceptor`, que solo lleva el contexto scoped de DI) fijaban el `search_path`
con `ExecuteSqlAsync` **parametrizado** (`SET search_path TO @p0` → error 42601) y, además, sobre una
conexión distinta a la de la consulta por el pooling (→ 42P01). Se corrigió en `WebhookDeliveryWorker`,
`QueueNotificationWorker` e `InactivityTimeoutWorker`: SQL crudo con el identificador saneado y una
**conexión abierta explícitamente** durante cada bloque de BD.
