# Changelog

Todos los cambios importantes del CRM omnicanal multi-tenant se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y el proyecto usará [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

## [0.3.0] - 2026-06-19

### Added

- **Frontend (Blazor WebAssembly aislado + biblioteca de UI `Mimo.Ui`)**: apps `Mimo.App` (tenant) y `Mimo.Admin.App` (SuperAdmin), con Tailwind + Flowbite centralizados en `Mimo.Ui` (cero hardcode en las apps) y clientes tipados generados con Kiota desde OpenAPI (`Mimo.Api.Sdk`, `Mimo.Admin.Api.Sdk`). ADR 0011 y 0012.
- **Mimo.App — Fases A/B/C**: login JWT; CRUD de funcionarios y roles; gestión de conocimiento (documentos) y configuración self-service del tenant (ADR 0013); consola de agente: bandeja, triage, mensajería en vivo (SignalR), transferencias, cola en vivo, chat interno entre funcionarios y visualización de la encuesta de satisfacción.
- **Mimo.Admin.App — Fase D**: login de SuperAdmin y gestión de tenants, planes y uso de IA, conectores y políticas de IA.
- **Resolución de tenant en hubs SignalR** por invocación con `TenantHubFilter` (ADR 0014).
- **Módulo de Interoperabilidad**:
  - Gestión de **API keys** del tenant (hash SHA-256 + prefijo visible, clave en claro una sola vez, revocación) bajo `TenantAdmin`.
  - **Autenticación por API key** (esquema `X-Api-Key`) y **API pública de integración** `/integration/v1` (`me`, conversaciones); el tenant se resuelve a partir de la clave (ADR 0015).
  - **Webhooks salientes**: suscripciones por evento (`conversation.created`, `ticket.assigned/resolved`, `survey.recorded`), entrega firmada (HMAC-SHA256), reintentos con backoff y bitácora (ADR 0016).
- **`Mimo.Worker`**: host de procesos de background separado (entrega de webhooks y cierre por inactividad), ejecutable como Servicio de Windows; migración de los esquemas de tenants existentes al arrancar la API (ADR 0017). Script `deploy-worker.ps1`.
- **WebChat embebible**: widget en JavaScript vanilla servido por `Mimo.Api` (`/webchat/widget.js`) + endpoint público `/webchat/config` y política CORS abierta acotada a la superficie del widget (ADR 0018).
- **Seeders de desarrollo** en ambas APIs y negocio de demostración **AndinaShop** (funcionarios, clientes, conversaciones, tickets, encuestas y conocimiento).
- **Documentación de presentación**: propuesta comercial, anexo técnico de integraciones y visión de mejora continua (con nombre clave del producto).

### Changed

- Respuestas tipadas (`.Produces<T>()`) en `Mimo.Api` y regeneración del cliente Kiota.
- Pipeline de Tailwind movido a `Mimo.Ui`: las apps no contienen JS de build.
- Cliente Kiota renombrado a `Mimo.Api.Sdk` (nombre acorde a su capa en la arquitectura).
- Scripts de despliegue separados por capa: `deploy-tenant.ps1`, `deploy-admin.ps1` y `deploy-worker.ps1` (reemplazan `deploy-iis.ps1`).
- Workers de background movidos de `Mimo.Api` a `Mimo.Worker`, excepto el de cola (acoplado a SignalR) (ADR 0017).
- CORS permisivo en Development para publicaciones locales.

### Fixed

- Bug multi-tenant en los workers: `SET search_path` se parametrizaba (error 42601) y, con pooling, la consulta tomaba otra conexión apuntando a `public` (42P01). Corregido en `WebhookDeliveryWorker`, `QueueNotificationWorker` e `InactivityTimeoutWorker` con SQL crudo saneado y una conexión abierta explícita durante cada bloque de BD.
- `GET /roles` y `GET /agents` pasaron a la política `Agent` (las escrituras siguen en `TenantAdmin`) para alimentar los selectores de transferencia y chat interno de la consola.

### Security

- Las **API keys de interoperabilidad** se almacenan solo como hash SHA-256 (+ prefijo); nunca en claro.
- El **secreto de firma de webhooks** se cifra en reposo (Data Protection) y se muestra una sola vez al crear la suscripción.
- La superficie pública del **WebChat** es anónima y sin cookies; el CORS abierto a cualquier origen se limita a esa superficie (config + conversaciones + encuesta).

## [0.2.0] - 2026-06-15

### Added

- Administración del SuperAdmin en `Mimo.Admin.Api`: catálogo de planes (`/plans`),
  conectores de IA (`/ai/connectors`, con activación exclusiva y sin exponer la API key),
  políticas de IA por plan (`/ai/plan-policies`, upsert de modelos permitidos y cuotas) y
  estadísticas de uso por tenant (`/ai/usage/summary`).
- Repositorios `IAiConnectorRepository`, `IAiPlanPolicyRepository`, `IAiUsageRepository` y
  extensión de `IPlanRepository` para gestión del catálogo.
- Tests de agregación de uso de IA (resumen por tenant y periodo).
- `search_path` multi-tenant vía `SearchPathConnectionInterceptor` + `ITenantSchemaProvider` (ADR 0009).
- `scripts/deploy-iis.ps1`: publica y despliega ambas APIs a IIS local (auto-elevación, App Pool sin código administrado, preserva `appsettings.Production.json`) + plantilla y README.
- Cifrado en reposo de las API keys de conectores de IA con Data Protection (`ISecretProtector`); anillo de llaves compartido entre ambas APIs (ADR 0010).

### Security

- Las API keys de conectores de IA ya no se almacenan en texto plano en la base de datos; se cifran al escribir y se descifran solo al construir el cliente del proveedor.

### Fixed

- La autorización por roles no funcionaba en runtime: `JwtBearer` remapeaba el claim `role`; se desactiva con `MapInboundClaims = false` en ambas APIs.
- Aprovisionamiento de tenant roto: `SET search_path`/`CREATE SCHEMA` se parametrizaban (error 42601) y el `search_path` no persistía con pooling; ahora se usa SQL crudo con esquema saneado y una conexión abierta durante la migración.
- La migración del esquema de tenant fallaba por `CREATE INDEX CONCURRENTLY` dentro de transacción (25001); el índice HNSW se crea sin `CONCURRENTLY` (esquema nuevo, tabla vacía).
- Creación de tenant no atómica: si el aprovisionamiento fallaba quedaba un registro huérfano. Ahora se aprovisiona primero y el registro global se persiste solo si tuvo éxito; ante fallo se elimina el esquema parcial. Se corrige también `DeprovisionAsync` (mismo bug de parametrización del identificador).

## [0.1.0] - 2026-06-14

### Added

- Documentación de fundación: `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md` y `docs/PLAN.md`.
- Solución .NET 10 multi-tenant: `Mimo.Core`, `Mimo.Infrastructure`, `Mimo.Api`, `Mimo.Admin.Api` y proyectos de pruebas.
- Resolución de tenant por middleware (header/subdominio/JWT) con `search_path` por petición y caché en `IMemoryCache`.
- Modelo de datos con EF Core (esquema global y por tenant) y migraciones para ambos contextos.
- Cola de tickets, asignación, transferencias entre roles/funcionarios, chat interno y hubs SignalR (`ChatHub`, `TicketHub`).
- Conectores de canales (Facebook, WhatsApp, Telegram, Instagram, WebChat), pipeline de webhooks y encuestas de satisfacción.
- Búsqueda semántica con pgvector y orquestación RAG con detección de escalada.
- Autenticación JWT para agentes y super administradores; creación del administrador inicial al aprovisionar el tenant.
- Autorización basada en roles por políticas (`TenantAdmin`, `Agent`, `SuperAdmin`).
- Paginación reutilizable (`PagedResult<T>` + extensión `ToPagedResultAsync`).
- Conectores de IA configurables en base de datos (JSONB) con abstracción `ILlmClient` y fábrica por proveedor.
- Gateway de IA in-process con entitlements y cuotas por plan y medición de uso por tenant.
- Catálogo de planes (`plans`) con integridad referencial: FK desde `tenants` y `ai_plan_policies` por código; validación al crear tenant y siembra por defecto (`free`, `pro`).
- Primera red de pruebas (xUnit): hashing de contraseñas, emisión de JWT, paginación y gateway de IA (entitlements/cuotas) sobre EF InMemory.
- Architecture Decision Records en `docs/adr/` (0001–0008).

### Changed

- Eliminado Redis: cola de tickets con transacción serializable en PostgreSQL y caché efímera con `IMemoryCache` (ADR 0001).
- La configuración del LLM se trasladó de `appsettings` a la base de datos; el acceso al modelo pasa por el gateway de plataforma (ADR 0004, 0005).
- Convención de endpoints sin parámetros en la ruta: simples por query, objetos por body, seguridad por header (ADR 0006). Afecta a todas las rutas con `{id}`/`{roleId}`/`{agentId}`/`{channel}`.
- Documentación (`README.md`, `docs/PLAN.md`, `CONTRIBUTING.md`) sincronizada con el estado real del backend.

### Fixed

- `GET /tickets/my` devolvía siempre vacío (consultaba un rol inexistente y filtraba en memoria); ahora filtra por funcionario en la consulta.
- El control de IA por plan caía en silencio a modo permisivo cuando el código de plan no coincidía por mayúsculas; ahora el catálogo de planes con FK y el match insensible a mayúsculas lo evitan (ADR 0007).

### Security

- Cerrado un acceso cross-tenant: en peticiones autenticadas el tenant lo dicta el token (claim `tenant_slug`); un `X-Tenant-Slug`/subdominio en conflicto responde 403 (ADR 0008). El acceso al tenant se centraliza en accesores tipados (`HttpContext.GetTenantId()`/`GetTenantSlug()`).

[Unreleased]: https://github.com/evillabong/chatbot-llm/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/evillabong/chatbot-llm/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/evillabong/chatbot-llm/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/evillabong/chatbot-llm/releases/tag/v0.1.0
