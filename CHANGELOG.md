# Changelog

Todos los cambios importantes del CRM omnicanal multi-tenant se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y el proyecto usará [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

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
- Architecture Decision Records en `docs/adr/` (0001–0007).

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

## [0.1.0] - Próximamente

### Planned

- Crear solución .NET 10 base.
- Crear proyectos `Mimo.Core`, `Mimo.Infrastructure` y `Mimo.Api`.
- Agregar `docker-compose.yml` con PostgreSQL, pgvector y Redis.
- Implementar resolución inicial de tenant.
- Definir modelo base de `Tenant`, `Agent`, `Role`, `Document`, `Conversation`, `Message` y `Ticket`.
- Implementar WebChat mínimo con SignalR.
- Implementar búsqueda documental semántica inicial.

[Unreleased]: https://github.com/evillabong/chatbot-llm/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/evillabong/chatbot-llm/releases/tag/v0.1.0
