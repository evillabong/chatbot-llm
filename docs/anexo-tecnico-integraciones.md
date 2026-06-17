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
> del cliente del núcleo de MIMO y permite que cada organización conecte sus sistemas sin tocar el
> producto.

## 5. Automatización: flujos, campañas y tareas — ▹

- **Flujos de trabajo:** motor de reglas "evento → condición → acción" (enrutar, etiquetar,
  responder, crear tarea, escalar, disparar encuesta). Se apoya en los eventos que ya emite el
  sistema (mensaje entrante, inactividad, cambio de estado de ticket).
- **Campañas:** envíos **salientes** por canal, segmentados y programados; respetan las ventanas y
  reglas de cada proveedor (p. ej. plantillas/ventana de 24 h en WhatsApp). Las respuestas reingresan
  a la bandeja.
- **Tareas:** entidad propia con responsable, vencimiento y estado, vinculable a conversación,
  ticket u oportunidad; alimentada por flujos y por la consola.

## 6. Ventas / integración con CRM — ▹

- Captura de oportunidades y **pipeline básico** dentro de MIMO (ver propuesta §4.10).
- **Sincronización opcional con CRM externo** vía su API (salida) cuando la organización ya opera
  un CRM; evita duplicar la gestión.

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
| Flujos de trabajo / Campañas / Tareas | ▹ roadmap |
| Ventas (leads + pipeline) e integración con CRM | ▹ roadmap |
| Mejora continua (aprende con el uso) | ▹ ver [vision-mejora-continua.md](vision-mejora-continua.md) |

> Este panorama debe mantenerse al día conforme avanza el producto; es la referencia para
> conversaciones técnicas y para acotar el alcance de cada propuesta.
