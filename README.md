# MIMO - CRM Omnicanal Multi-Tenant

CRM omnicanal multi-tenant para atención ciudadana con bot de IA, escalación a funcionarios, transferencia entre departamentos, chat interno, documentación semántica y conectores para canales externos.

El proyecto usa documentación y comentarios en español, pero mantiene nombres de clases, métodos, propiedades, variables, tablas y columnas en inglés.

## Índice

- [Visión general](#visión-general)
- [Stack tecnológico](#stack-tecnológico)
- [Actores](#actores)
- [Arquitectura objetivo](#arquitectura-objetivo)
- [Estructura planificada](#estructura-planificada)
- [Flujo de sesión](#flujo-de-sesión)
- [Modelo multi-tenant](#modelo-multi-tenant)
- [Inicio de desarrollo](#inicio-de-desarrollo)
- [Documentación](#documentación)
- [Versionado](#versionado)

## Visión general

El sistema está diseñado como una plataforma SaaS multi-tenant donde cada entidad administra sus propios canales, funcionarios, roles, documentos, parámetros y conversaciones. El ciudadano puede iniciar atención por WebChat o canales externos; el bot responde usando conocimiento filtrado por tenant, visibilidad y autenticación; cuando se requiere atención humana, la sesión entra a una cola y puede ser tomada, transferida, resuelta, cerrada o reabierta según reglas configurables.

Premisas principales:

- Aislamiento de datos por tenant.
- Visibilidad documental pública o privada.
- Filtrado de contexto antes de enviarlo al LLM.
- Atención humana sincrónica con cola en vivo.
- Transferencia entre roles y funcionarios.
- Configuración parametrizable por entidad.
- Búsqueda semántica con PostgreSQL + pgvector.

## Stack tecnológico

| Capa | Tecnología objetivo |
|---|---|
| Backend API | .NET 10 Minimal APIs |
| Frontend administrativo | Blazor WebAssembly |
| Tiempo real | SignalR |
| Base principal | PostgreSQL + pgvector |
| Caché efímera | `IMemoryCache` en proceso (sin Redis, ver [ADR 0001](docs/adr/0001-eliminar-redis-usar-ef-y-memorycache.md)) |
| LLM | Conectores configurables en BD vía gateway de plataforma (DeepSeek inicial, ver [ADR 0004](docs/adr/0004-configuracion-de-conectores-de-ia-en-base-de-datos.md) y [ADR 0005](docs/adr/0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md)) |
| Herramientas LLM | MCP Server integrado en .NET |
| Canales externos | Facebook Messenger, WhatsApp, Telegram, Instagram DM |
| WebChat | Blazor WASM standalone embebible |
| Contenedores | Docker Compose |

## Actores

| Actor | Responsabilidad |
|---|---|
| `SuperAdmin` | Administra tenants, planes, límites y monitoreo global. |
| `TenantAdmin` | Configura una entidad, funcionarios, roles, documentos, canales y reportes. |
| `Agent` | Atiende sesiones, transfiere, conversa internamente y gestiona su cola. |
| `Customer` | Inicia conversaciones, se autentica cuando aplica, espera atención y califica. |

## Arquitectura objetivo

```text
Canales externos / WebChat
  |
  v
Mimo.Api (.NET 10 Minimal APIs) [tenant-facing]
  |-- WebhookEndpoints
  |-- AuthEndpoints
  |-- DocumentEndpoints
  |-- TicketEndpoints
  |-- ConfigurationEndpoints
  |-- ReportEndpoints
  |
  |-- ChatHub
  |-- TicketHub
  |-- InternalChatHub
  |
  +--> PostgreSQL + pgvector
  +--> IMemoryCache (estado efímero)
  +--> Gateway de IA (entitlements + cuotas por plan) --> conector activo (DeepSeek)
  +--> MCP tools

Mimo.Admin.Api (.NET 10 Minimal APIs) [host separado]
  |-- TenantEndpoints
  |-- PlanEndpoints
  |-- BillingEndpoints
  |-- MonitoringEndpoints

Frontends Blazor WASM
  |-- Mimo.Admin.Web   (consume Mimo.Admin.Api)
  |-- Mimo.Tenant.Web  (consume Mimo.Api)
  |-- Mimo.Agent.Web   (consume Mimo.Api)
  |-- Mimo.WebChat     (consume Mimo.Api)
```

## Estructura planificada

```text
/
├── docs/
│   ├── PLAN.md
│   └── adr/                # Architecture Decision Records
├── src/
│   ├── Mimo.Core/
│   ├── Mimo.Infrastructure/
│   ├── Mimo.Api/
│   ├── Mimo.Admin.Api/
│   ├── Mimo.Admin.Web/
│   ├── Mimo.Tenant.Web/
│   ├── Mimo.Agent.Web/
│   └── Mimo.WebChat/
├── tests/
│   ├── Mimo.UnitTests/
│   └── Mimo.IntegrationTests/
├── docker-compose.yml
├── README.md
├── CONTRIBUTING.md
└── CHANGELOG.md
```

La estructura anterior está descrita en detalle en [docs/PLAN.md](docs/PLAN.md). El backend (`Mimo.Core`, `Mimo.Infrastructure`, `Mimo.Api`, `Mimo.Admin.Api`) ya está implementado; los frontends Blazor siguen pendientes.

## Flujo de sesión

```text
Customer inicia conversación
  |
  v
BotActive
  |
  |-- Respuesta automática con conocimiento filtrado
  |-- Solicitud de atención humana
          |
          v
        InQueue
          |
          |-- Agent toma sesión
          |-- Auto-asignación balanceada
          |-- Timeout configurable
          |
          v
        Assigned / InProgress
          |
          |-- Resolver
          |-- Transferir a rol
          |-- Transferir a funcionario
          |-- Devolver a cola
          |
          v
        Resolved
          |
          |-- Survey
          |-- Closed
          |-- Reopened si baja calificación
```

## Modelo multi-tenant

Estrategia objetivo:

- Una base PostgreSQL.
- Esquema `public` para catálogo global de tenants.
- Un esquema separado por tenant: `tenant_{slug}`.
- Resolución del tenant por JWT, subdominio, header, WebChat embed o configuración de webhook.
- `search_path` establecido por middleware al procesar la petición.

## Inicio de desarrollo

Infraestructura local (PostgreSQL + pgvector):

```bash
docker compose up postgres -d
```

Build y pruebas:

```bash
dotnet build MIMO.slnx
dotnet test MIMO.slnx
```

Las migraciones de EF Core (esquema global y por tenant) se aplican automáticamente
al arrancar `Mimo.Api` / `Mimo.Admin.Api`. La configuración de IA inicial se siembra
desde la sección `DeepSeek` de `appsettings` a la tabla `ai_connectors`.

## Documentación

| Documento | Descripción |
|---|---|
| [memory.md](memory.md) | Contexto rápido del proyecto, decisiones y próximos pasos. |
| [docs/PLAN.md](docs/PLAN.md) | Arquitectura canónica, actores, flujo, modelo de datos y fases. |
| [docs/adr/](docs/adr/README.md) | Architecture Decision Records: decisiones de arquitectura y su justificación. |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Reglas de colaboración, commits, branches y documentación. |
| [CHANGELOG.md](CHANGELOG.md) | Historial de cambios del proyecto. |

## Versionado

El proyecto usará Semantic Versioning:

```text
vMAJOR.MINOR.PATCH
```

Formato de commit:

```text
<tipo>: <autor> <descripcion-en-kebab-case>
```

Ejemplo:

```text
docs: jvillarreal agregar-plan-arquitectura-crm
feat: jvillarreal crear-api-base-multitenant
fix: jvillarreal corregir-resolucion-tenant
```

## Estado actual

Backend en desarrollo activo. Implementado:

- Solución .NET 10 (`Mimo.Core`, `Mimo.Infrastructure`, `Mimo.Api`, `Mimo.Admin.Api`) + tests base.
- Multi-tenant con esquema por tenant y resolución por middleware.
- Autenticación JWT (agentes y SuperAdmin) y autorización por roles.
- Cola de tickets, asignación, transferencias, chat interno y hubs SignalR.
- Conectores de canales y pipeline de webhooks; encuestas de satisfacción.
- Búsqueda semántica con pgvector y orquestación RAG con escalada.
- Conectores de IA configurables en BD y gateway de plataforma con entitlements/cuotas por plan.

Próximos pasos: endpoints de administración de IA (conectores, políticas de plan, estadísticas
de uso) en `Mimo.Admin.Api`, frontends Blazor y suite de pruebas.
