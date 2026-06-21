# Anexo técnico — Stack, integraciones y canales

- Audiencia: **interna / decisor técnico** (no forma parte del documento comercial).
- Complementa a [propuesta-comercial.md](propuesta-comercial.md). El detalle de arquitectura vive
  en los [ADR](adr/README.md); aquí se resume lo necesario para evaluar **integraciones y encaje**.
- Convención de estado: **✅ operativo · 🟡 andamiado/parcial · ▹ roadmap/diseño**.

## 1. Stack (resumen)

| Capa | Tecnología |
|---|---|
| APIs | .NET 10 (Minimal APIs) — API de tenant + API de administración |
| Frontend | Blazor WebAssembly (apps estáticas) + biblioteca de UI propia |
| Base de datos | PostgreSQL con `pgvector` (búsqueda semántica) |
| Tiempo real | SignalR (mensajería en vivo de la consola y el chat) |
| IA / LLM | Gateway in-process que media el acceso a modelos por organización y plan |
| Multi-tenant | Aislamiento por organización (esquema por tenant) |

> Detalle: gateway de IA ([ADR 0005](adr/0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md)),
> binding de tenant ([ADR 0008](adr/0008-binding-de-tenant-al-principal-autenticado.md)),
> aislamiento por `search_path` ([ADR 0009](adr/0009-search-path-por-interceptor-de-conexion.md)),
> cifrado de secretos ([ADR 0010](adr/0010-cifrado-de-api-keys-de-conectores-de-ia.md)),
> frontend ([ADR 0011](adr/0011-arquitectura-frontend-blazor-wasm-aislado.md)).

## 2. Chat web embebible (WebChat) — ✅

- Se integra en cualquier sitio con un **widget embebible** (un *snippet*/script que monta el chat).
- La conversación usa la **API REST** para iniciar/historial y **SignalR** para el tiempo real
  (mensajes del ciudadano ↔ bot/agente).
- **Personalizable por organización**: color, logo y mensaje de bienvenida.
- No requiere autenticación del ciudadano (anónimo); el tenant se resuelve por configuración del
  *embed*.

## 3. Canales de mensajería

**Modelo común:** cada canal es un **conector** (`IChannelConnector`) y los mensajes entrantes
llegan por **webhook** (`POST /webhooks/incoming?channel=...`) con **verificación de firma** del
proveedor; un orquestador único procesa el mensaje sin importar el canal de origen.

| Canal | API del proveedor | Entrante | Estado |
|---|---|---|---|
| WebChat | Propio (REST + SignalR) | en vivo | ✅ operativo |
| WhatsApp | WhatsApp Business Platform (Cloud API) | webhook + firma `X-Hub-Signature-256` | 🟡 andamiado |
| Telegram | Telegram Bot API | webhook + *secret token* | 🟡 andamiado |
| Facebook Messenger | Meta Messenger Platform (Graph API) | webhook + verificación Meta | 🟡 andamiado |
| Instagram DM | Meta Graph API (Instagram Messaging) | webhook + verificación Meta | 🟡 andamiado |

- La verificación de webhook (GET con *challenge* de Meta / *secret token* de Telegram) ya está
  contemplada en el endpoint.
- "Andamiado" = la estructura (conector, ruteo, verificación de firma) existe; falta **completar y
  certificar** cada integración contra el proveedor (números/cuentas, plantillas, aprobaciones).
- **Credenciales por canal y por organización**, cifradas en reposo (mismo mecanismo que las API
  keys de IA, [ADR 0010](adr/0010-cifrado-de-api-keys-de-conectores-de-ia.md)).

## 4. Acciones del asistente / consumo de APIs (vía MCP) — ▹

Capacidad para que el asistente **consulte sistemas externos y ejecute acciones** durante la
conversación (estado de pedido, saldo, agenda, registrar una solicitud), respondiendo con **datos
reales**.

**Diseño propuesto (roadmap):**
- Se modela como **herramientas (tools / function calling)** que el modelo puede invocar a través
  del **gateway de IA**.
- Las herramientas se publican mediante un **servidor MCP (Model Context Protocol) por
  organización**: cada tenant declara sus herramientas con **esquema de entrada/salida**,
  *endpoint*, **autenticación** y límites.
- El gateway ofrece al modelo solo las herramientas **autorizadas** del tenant y **media la
  ejecución** (no es el modelo quien tiene las credenciales).

**Controles:**
- **Allowlist** de herramientas por organización; nada se invoca sin declararse.
- **Credenciales por tenant**, cifradas; aislamiento total entre organizaciones.
- **Límites y auditoría** de invocaciones (cuotas, *logging*), integrables con el conteo de uso.
- Herramientas de **solo lectura** vs **con efectos**: estas últimas requieren confirmación/permiso
  explícito.

> Por qué MCP: estandariza cómo se exponen las herramientas al modelo, desacopla las integraciones
> del cliente del núcleo de CHATBOT/AI CRM y permite que cada organización conecte sus sistemas sin tocar el
> producto.

**Servidores MCP externos (◐ catálogo hecho; invocación ▹):** además de las herramientas declaradas
por el tenant, se podrán **registrar servidores MCP de terceros** (catálogo por organización) para
sumar capacidades sin desarrollarlas en CHATBOT/AI CRM. Cada servidor se habilita con su *endpoint*,
autenticación, allowlist de herramientas y límites; el gateway sigue mediando y auditando. Aislado por
organización. ✅ **Corte 1 ([ADR 0024](adr/0024-catalogo-de-servidores-mcp-externos.md)):** catálogo
`McpServer` + endpoints `/mcp-servers` (TenantAdmin) con token cifrado write-only, allowlist
deny-by-default y validación de endpoint; UI `/mcp-servers`. **Pendiente:** la invocación mediada por el
gateway (con anti-SSRF), scopes por herramienta y auditoría/cuotas.

## 4.bis. Modo de respuesta del bot y agentes de IA — ▹/✅

**El modo de respuesta es configurable por organización** (propuesta §4.2/§4.16):

- **Modo catálogo de opciones (▹):** motor de **flujos guiados** (árbol de opciones/menús),
  **determinista, sin IA**. Pasos de menú, captura/validación de datos y **pasos de llamada a API
  externa** (consultar/registrar y bifurcar según la respuesta). Puede **derivar a la IA** o a un
  funcionario. Constructor visual de flujos.
- **Modo IA generativa (✅ base; extensiones ▹):** el `ConversationOrchestrator` resuelve con LLM +
  **búsqueda semántica** (`pgvector`) sobre el conocimiento; extensible con herramientas/MCP.

**Agentes de IA por rol / tipo de atención (▹):** en lugar de un único asistente, se podrán definir
**perfiles de agente** por rol o por tipo de atención, cada uno con su *system prompt*/instrucciones,
**subconjunto de conocimiento**, **herramientas/MCP autorizados** y tono. El gateway selecciona el
agente según el contexto (rol destino, canal, categoría) y aplica los límites del plan. Todo por
organización y aislado.

## 5. Automatización: flujos, campañas y tareas — ▹

- **Flujos de trabajo (◐ motor base hecho):** motor de reglas "evento → condición → acción" (enrutar,
  etiquetar, responder, crear tarea, escalar, disparar encuesta). Se apoya en los eventos que ya emite
  el sistema. ✅ **Corte 1 ([ADR 0026](adr/0026-motor-de-reglas-de-automatizacion.md)):** `AutomationRule`
  + `IDomainEventPublisher` (fan-out webhooks + automatización) + `AutomationDispatcher`; condiciones AND
  sobre campos del evento, acciones **crear tarea** (título con marcadores `{campo}`) y **escalar a
  funcionario** (crea ticket y lo encola); endpoints `/automation-rules` (TenantAdmin) y UI
  `/automatizaciones`. **Pendiente:** más acciones (etiquetar/enrutar/disparar encuesta) y operadores de
  condición más ricos.
- **Campañas:** envíos **salientes** por canal, segmentados y programados; respetan las ventanas y
  reglas de cada proveedor (p. ej. plantillas/ventana de 24 h en WhatsApp). Las respuestas reingresan
  a la bandeja.
- **Tareas (◐ entidad hecha):** entidad propia con responsable, vencimiento y estado, vinculable a
  conversación, ticket u oportunidad; alimentada por flujos y por la consola. ✅ **Corte 1
  ([ADR 0025](adr/0025-tareas-operativas.md)):** entidad `WorkTask` + endpoints `/tasks` (Agent) con
  ciclo de vida (cierre automático al completar/cancelar) y UI `/tareas`. **Pendiente:** que el motor de
  reglas las cree como acción y avisos de vencimiento.

## 6. Ventas / integración con CRM — ▹

- Captura de oportunidades y **pipeline básico** dentro de CHATBOT/AI CRM (ver propuesta §4.10).
- **Sincronización opcional con CRM externo** vía su API (salida) cuando la organización ya opera
  un CRM; evita duplicar la gestión.

## 6.bis. Interoperabilidad (API de integración, API keys, webhooks salientes) — 🟡

Superficie de integración **por organización** para que sus sistemas (ERP, CRM, e-commerce,
portal) se conecten. Gestionada por el `TenantAdmin` desde `Mimo.App`; aislada por tenant.

- **API de integración — ✅ implementado (corte 2):** superficie pública versionada `/integration/v1/*`,
  autenticada por **API key** (cabecera `X-Api-Key`, esquema propio), **no** por el JWT de usuario. El
  tenant se resuelve a partir de la API key, no por `X-Tenant-Slug` ([ADR 0015](adr/0015-autenticacion-por-api-key-y-api-de-integracion.md)).
  Endpoints del corte: `GET /me` (verificación de credenciales), `POST /conversations` (iniciar/retomar)
  y `GET /conversations/detail`. **Pendiente:** ampliar el subconjunto (tickets, conocimiento…),
  **scopes por clave** y un documento OpenAPI dedicado a esta superficie.
- **Gestión de API keys — ✅ implementado (corte 1):**
  - Se **genera y se muestra una sola vez**; en BD se guarda solo un **hash SHA-256** (la clave es un
    token aleatorio de alta entropía `mk_…`, por lo que no requiere hashing lento tipo PBKDF2) más un
    **prefijo visible** para identificarla; nunca la clave en claro.
  - Atributos: nombre, prefijo, fecha de creación, **último uso**, estado (activa/revocada).
  - **Revocación** inmediata. Aislada por tenant (tabla `public.api_keys` con `TenantId`, ya que el
    tenant se resolverá a partir de la propia key).
  - Endpoints `/integration/api-keys` (política `TenantAdmin`) y página `/api-keys` en `Mimo.App`.
  - **Pendiente del corte:** scopes/permisos y rotación.
- **Webhooks salientes — ✅ implementado (corte 3, [ADR 0016](adr/0016-webhooks-salientes-con-firma-hmac-y-reintentos.md)):**
  - El tenant registra **URLs** y se suscribe a eventos: `conversation.created`, `ticket.assigned`,
    `ticket.resolved`, `survey.recorded`. Gestión en `/integration/webhooks` (TenantAdmin).
  - Entrega **firmada (HMAC-SHA256 con secreto por suscripción)** en `X-Mimo-Signature` (+ cabeceras
    `X-Mimo-Event/Delivery/Timestamp`); el secreto se cifra en reposo y se muestra una sola vez.
  - **Reintentos con backoff** (1m→5m→15m→1h, hasta 5) por un worker de background, y **bitácora** de
    entregas (estado, intentos, código de respuesta, error).
  - **Distinto** de los webhooks *entrantes* de canales (`/webhooks/incoming`, §3): aquí la
    plataforma es el emisor hacia los sistemas del cliente.
  - **Pendiente:** backoff/eventos configurables por suscripción, reintento manual desde la bitácora.
- **Estado:** los tres cortes están **operativos** — gestión de API keys (corte 1), API de integración
  con autenticación por API key (corte 2, [ADR 0015](adr/0015-autenticacion-por-api-key-y-api-de-integracion.md))
  y webhooks salientes (corte 3, [ADR 0016](adr/0016-webhooks-salientes-con-firma-hmac-y-reintentos.md)).

## 7. Seguridad e identidad (resumen)

- Autenticación por **JWT**; autorización por rol; el tenant se **liga al token**
  ([ADR 0008](adr/0008-binding-de-tenant-al-principal-autenticado.md)).
- **Aislamiento por organización** a nivel de datos
  ([ADR 0009](adr/0009-search-path-por-interceptor-de-conexion.md)); las conexiones en tiempo real
  resuelven el tenant por invocación ([ADR 0014](adr/0014-resolucion-de-tenant-en-hubs-signalr.md)).
- **Secretos cifrados en reposo** (API keys de IA, credenciales de canal)
  ([ADR 0010](adr/0010-cifrado-de-api-keys-de-conectores-de-ia.md)).
- Webhooks entrantes **verificados por firma** del proveedor.

## 8. Estado de implementación (panorama honesto)

| Capacidad | Estado |
|---|---|
| WebChat embebible + tiempo real | ✅ |
| Asistente con IA sobre base de conocimiento (RAG) | ✅ |
| Consola de agentes (bandeja, conversación, triage, transferencias, mensajería en vivo) | ✅ |
| Enrutamiento, colas, tickets, equipos/roles, encuestas, configuración | ✅ |
| Conectores WhatsApp / Telegram / Facebook / Instagram | 🟡 andamiado |
| Acciones del asistente vía MCP (tool-use) | ▹ diseño |
| Chatbot por catálogo de opciones (flujos guiados, sin IA) | ▹ roadmap |
| Agentes de IA por rol/atención + servidores MCP externos | ▹ diseño |
| Flujos de trabajo / Campañas / Tareas | ▹ roadmap |
| Ventas (leads + pipeline) e integración con CRM | ▹ roadmap |
| Interoperabilidad: gestión de **API keys** + **API de integración** (auth por API key) | ✅ |
| Interoperabilidad: **webhooks salientes** (firma HMAC, reintentos, bitácora) | ✅ |
| Mejora continua (aprende con el uso) | ▹ ver [vision-mejora-continua.md](vision-mejora-continua.md) |

> Este panorama debe mantenerse al día conforme avanza el producto; es la referencia para
> conversaciones técnicas y para acotar el alcance de cada propuesta.
