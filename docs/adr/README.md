# Architecture Decision Records (ADR)

Este directorio registra las decisiones de arquitectura relevantes del proyecto MIMO.

Un ADR documenta **una** decisión: su contexto, la opción elegida, las alternativas
consideradas y sus consecuencias. Los ADR son inmutables una vez aceptados; si una
decisión se revierte o cambia, se crea un ADR nuevo que marca al anterior como
*Reemplazado por*.

## Convención

- Formato: [MADR](https://adr.github.io/madr/) simplificado (ver [plantilla](0000-plantilla.md)).
- Nombre de archivo: `NNNN-titulo-en-kebab-case.md` con numeración incremental de 4 dígitos.
- Idioma: español (igual que el resto de la documentación).
- Estados posibles: `Propuesto`, `Aceptado`, `Reemplazado por NNNN`, `Obsoleto`.
- Al tomar una decisión arquitectónica, crear el ADR **en el mismo lote** del código
  (regla de `CONTRIBUTING.md` sobre documentación obligatoria).

## Índice

| ADR | Título | Estado |
|---|---|---|
| [0001](0001-eliminar-redis-usar-ef-y-memorycache.md) | Eliminar Redis: usar EF Core y IMemoryCache | Aceptado |
| [0002](0002-autenticacion-jwt-agentes-y-superadmin.md) | Autenticación JWT para agentes y super administradores | Aceptado |
| [0003](0003-autorizacion-basada-en-roles.md) | Autorización basada en roles por políticas | Aceptado |
| [0004](0004-configuracion-de-conectores-de-ia-en-base-de-datos.md) | Configuración de conectores de IA en base de datos | Aceptado |
| [0005](0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md) | Gateway de IA in-process con entitlements y cuotas por plan | Aceptado |
| [0006](0006-convencion-de-endpoints-sin-parametros-en-la-ruta.md) | Convención de endpoints sin parámetros en la ruta | Aceptado |
| [0007](0007-catalogo-de-planes-con-integridad-referencial.md) | Catálogo de planes con integridad referencial | Aceptado |
| [0008](0008-binding-de-tenant-al-principal-autenticado.md) | Binding del tenant al principal autenticado | Aceptado |
| [0009](0009-search-path-por-interceptor-de-conexion.md) | search_path por interceptor de conexión | Aceptado |
| [0010](0010-cifrado-de-api-keys-de-conectores-de-ia.md) | Cifrado de las API keys de conectores de IA en reposo | Aceptado |
| [0011](0011-arquitectura-frontend-blazor-wasm-aislado.md) | Arquitectura de frontend: Blazor WASM aislado con biblioteca de UI y cliente generado | Aceptado |
| [0012](0012-autenticacion-en-frontend-wasm-con-jwt-en-sessionstorage.md) | Autenticación en el frontend WASM: JWT en sessionStorage e inyección por handler | Aceptado |
| [0013](0013-configuracion-self-service-del-tenant-en-mimo-api.md) | Configuración self-service del tenant en Mimo.Api | Aceptado |
| [0014](0014-resolucion-de-tenant-en-hubs-signalr.md) | Resolución de tenant en hubs SignalR | Aceptado |
| [0015](0015-autenticacion-por-api-key-y-api-de-integracion.md) | Autenticación por API key y superficie de integración | Aceptado |
| [0016](0016-webhooks-salientes-con-firma-hmac-y-reintentos.md) | Webhooks salientes con firma HMAC y reintentos | Aceptado |
| [0017](0017-host-de-workers-separado-mimo-worker.md) | Host de workers separado (Mimo.Worker) | Aceptado |
| [0018](0018-webchat-embebible-widget-vanilla-js.md) | WebChat embebible: widget vanilla JS servido por la API | Aceptado |
| [0019](0019-acceso-cross-origin-anonimo-al-chathub.md) | Acceso cross-origin anónimo al ChatHub (WebChat en vivo) | Aceptado |
| [0020](0020-claim-atomico-de-entregas-de-webhook.md) | Claim atómico de entregas de webhook (worker escalable) | Aceptado |
| [0021](0021-chatbot-por-opciones-flujos-guiados.md) | Chatbot por opciones: flujos guiados deterministas (corte 1) | Aceptado |
| [0022](0022-refresh-tokens-de-funcionarios.md) | Refresh tokens y revocación de sesión (funcionarios) | Aceptado |
| [0023](0023-instrumentacion-de-vacios-de-conocimiento.md) | Instrumentación de señales de recuperación y vacíos de conocimiento (#22, Fase 1) | Aceptado |
