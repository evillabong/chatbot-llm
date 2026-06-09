# Memoria del proyecto

Este archivo mantiene el contexto operativo del proyecto para retomar trabajo sin depender de conversaciones previas.

## Identidad

- Nombre actual del repositorio: `chatbot-llm`.
- Nombre de la solución: `MIMO`.
- Producto objetivo: CRM omnicanal multi-tenant para atención ciudadana.
- Estado actual: planificación y documentación inicial.
- Documento canónico de arquitectura: `docs/PLAN.md`.

## Convenciones

- Documentación y comentarios en español.
- Código, clases, métodos, propiedades, variables, tablas y columnas en inglés.
- Commits con formato:

```text
<tipo>: <autor> <descripcion-en-kebab-case>
```

Ejemplos:

```text
docs: jvillarreal agregar-plan-arquitectura-crm
feat: jvillarreal crear-api-base-multitenant
fix: jvillarreal corregir-resolucion-tenant
```

## Stack objetivo

| Área | Tecnología |
|---|---|
| Backend | .NET 10 Minimal APIs |
| Frontend | Blazor WebAssembly |
| Tiempo real | SignalR |
| Base de datos | PostgreSQL + pgvector |
| Caché y sesiones | Redis |
| LLM | DeepSeek API |
| Herramientas LLM | MCP Server integrado en .NET |
| Canales | WebChat, Facebook Messenger, Instagram DM, WhatsApp, Telegram |
| Contenedores | Docker Compose |

## Arquitectura objetivo

```text
src/
├── Mimo.Core/
├── Mimo.Infrastructure/
├── Mimo.Api/             ← API tenant-facing
├── Mimo.Admin.Api/       ← API exclusiva de administración (host separado)
├── Mimo.Admin.Web/       ← Blazor WASM, consume Mimo.Admin.Api
├── Mimo.Tenant.Web/      ← Blazor WASM, consume Mimo.Api
├── Mimo.Agent.Web/       ← Blazor WASM, consume Mimo.Api
└── Mimo.WebChat/         ← Widget embebible, consume Mimo.Api
```

## Actores

| Actor | Descripción |
|---|---|
| `SuperAdmin` | Administra tenants, planes, límites y monitoreo global. |
| `TenantAdmin` | Administra configuración, canales, documentos, roles, funcionarios y reportes de su entidad. |
| `Agent` | Atiende sesiones, transfiere, usa chat interno y gestiona su cola. |
| `Customer` | Inicia conversaciones, se autentica si aplica, espera atención y califica. |

## Premisas de diseño

- Aislamiento multi-tenant obligatorio.
- La IA no decide visibilidad de datos; el sistema filtra antes de llamar al LLM.
- Documentos con visibilidad pública o privada.
- Documentos vinculables a roles/departamentos.
- Atención humana sincrónica con cola.
- Transferencia entre roles y funcionarios.
- Parámetros configurables por tenant.
- Auditoría para operaciones administrativas, transferencias y acciones sensibles.

## Multi-tenancy

Estrategia objetivo:

- Una base PostgreSQL.
- Esquema `public` para catálogo global de tenants.
- Un esquema separado por tenant: `tenant_{slug}`.
- Resolución del tenant por JWT, subdominio, header, WebChat embed o webhook externo.
- Middleware debe establecer `search_path` antes de consultar datos del tenant.

## Flujo de sesión

```text
BotActive
  ├── responde con conocimiento filtrado
  └── solicita atención humana
        └── InQueue
              ├── Assigned
              ├── InProgress
              ├── Resolved
              ├── Survey
              ├── Closed
              └── Reopened si baja calificación
```

## Archivos principales

| Archivo | Uso |
|---|---|
| `README.md` | Entrada principal del proyecto. |
| `CONTRIBUTING.md` | Reglas de contribución, commits, branches y seguridad. |
| `CHANGELOG.md` | Historial de cambios. |
| `docs/PLAN.md` | Arquitectura detallada y plan de implementación. |
| `memory.md` | Contexto rápido para retomar trabajo. |

## Próximos pasos sugeridos

1. Crear solución .NET 10 base (`MIMO.sln`).
2. Crear proyectos `Mimo.Core`, `Mimo.Infrastructure`, `Mimo.Api` y `Mimo.Admin.Api`.
3. Agregar `docker-compose.yml` con PostgreSQL, pgvector y Redis.
4. Implementar modelo base: `Tenant`, `Agent`, `Role`, `Document`, `Conversation`, `Message`, `Ticket`.
5. Implementar `TenantResolutionMiddleware`.
6. Crear pruebas de aislamiento multi-tenant.
7. Implementar WebChat mínimo con SignalR.

## Notas de mantenimiento

- Actualizar este archivo cuando cambien decisiones importantes, estructura del proyecto o próximos pasos.
- No duplicar aquí detalles extensos de arquitectura; ponerlos en `docs/PLAN.md` y resumirlos en esta memoria.
- No guardar secretos, credenciales ni datos personales.
