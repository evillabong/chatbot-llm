# Changelog

Todos los cambios importantes del CRM omnicanal multi-tenant se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y el proyecto usará [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

## [0.18.1] - 2026-06-21

### Fixed

- **Despliegue de las WASM a IIS no incluía el `web.config` (front no cargaba).** `Publish-WasmApp` copiaba solo el contenido de `wwwroot`, pero el SDK de Blazor WebAssembly genera el `web.config` en la **raíz del publish** (con la regla "Serve subdir" que reescribe hacia `wwwroot\{R:0}` + fallback SPA y los MIME de `.wasm`/`.webcil`). Al quedar fuera, IIS servía los archivos con MIME incorrecto y sin enrutado SPA, así que el front no cargaba. Ahora el deploy copia **toda la raíz del publish** (web.config + `wwwroot/`) al sitio. La plantilla `appsettings.Production.template.json` también incorpora `Cors:AllowedOrigins`.

## [0.18.0] - 2026-06-21

### Added

- **Ventas — corte 3: sincronización con CRM externo (proveedor simulado) (#26)** ([ADR 0028](docs/adr/0028-sincronizacion-crm-con-proveedor-simulado.md)): la sincronización de oportunidades con un CRM externo se construye contra una **abstracción `ICrmSyncProvider`** con un **proveedor simulado** por defecto (asigna un id externo determinista `SIM-…`), de modo que la capacidad funciona y se verifica sin depender de un CRM real. `ICrmSyncService` actualiza el estado de sync de la oportunidad (`ExternalCrmId`, `LastSyncedAt`) y registra una **bitácora** (`CrmSyncLog`). Disparo **manual** (`POST /opportunities/sync`, `GET /opportunities/sync-log`, botón «Sincronizar» y columna de estado en `/ventas`) y **automático**: la creación y el cambio de etapa emiten los eventos de dominio `opportunity.created` / `opportunity.stage_changed`, y una nueva acción del motor de automatización **`SyncCrm`** sincroniza la oportunidad del evento. El proveedor de CRM real (con credenciales/mapeo/anti-SSRF) se enchufará luego implementando la misma interfaz. **Verificado E2E** (sync manual fija estado + bitácora; regla `opportunity.created → SyncCrm` sincroniza automáticamente al crear) + unit tests del proveedor simulado.

## [0.17.0] - 2026-06-21

### Added

- **Ventas — corte 2: tareas de seguimiento vinculadas (#26)** ([ADR 0027](docs/adr/0027-ventas-oportunidades-y-pipeline.md)): `WorkTask` (#24) gana `OpportunityId` opcional, de modo que una tarea puede ser seguimiento de una oportunidad; `GET /tasks` acepta el filtro `opportunityId` y la creación lo admite. La página `/ventas` incorpora un panel **«Tareas»** por oportunidad para listar el seguimiento, agregar tareas (título + vencimiento) y completarlas al vuelo. **Verificado E2E** (tarea creada con `opportunityId` → el filtro devuelve solo las de esa oportunidad, excluyendo las sueltas). Migración aditiva `TaskOpportunityLink`.

## [0.16.0] - 2026-06-21

### Added

- **Ventas — corte 1: oportunidades y pipeline (#26)** ([ADR 0027](docs/adr/0027-ventas-oportunidades-y-pipeline.md)): nueva entidad `Opportunity` (esquema del tenant) y endpoints `/opportunities` (política Agent) para **listar** (filtro por etapa y responsable), **crear**, **actualizar** (incluida la etapa) y **eliminar** oportunidades de venta, con contacto (nombre/correo/teléfono), **etapa del pipeline** (`New`/`Qualified`/`Proposal`/`Won`/`Lost`), monto estimado, conversación de origen opcional, responsable y notas. Reglas puras `OpportunityStageRules`: pasar a `Won`/`Lost` fija `ClosedAt` y reabrir lo limpia. El monto es no-nullable (0 = sin monto) para tiparse en el SDK como número (evita `UntypedNode`, #18). Nueva página `/ventas` en `Mimo.App` (bandeja con filtro por etapa, alta/edición y borrado). Capacidad opcional de ventas/CRM; el gating por plan y la sincronización con CRM externo quedan para cortes posteriores. **Verificado E2E** (crear; `Won` fija closedAt; reabrir lo limpia; filtros por etapa; authz 401; 204→404) + unit tests de las reglas de etapa.

## [0.15.0] - 2026-06-20

### Added

- **Automatización — corte 4: operadores de condición (#24)** ([ADR 0026](docs/adr/0026-motor-de-reglas-de-automatizacion.md)): las condiciones de las reglas ahora soportan operadores **igual / distinto / contiene / no contiene** (`RuleCondition.Operator`), además de la igualdad. El operador por defecto es «igual», por lo que las reglas previas siguen funcionando sin cambios. Para campos ausentes, «distinto»/«no contiene» se cumplen y «igual»/«contiene» no. La UI `/automatizaciones` incorpora un selector de operador. **Verificado E2E** (condición `externalUserId contiene "vip"`: solo la conversación VIP dispara la tarea) + unit tests de los cuatro operadores.

## [0.14.0] - 2026-06-20

### Added

- **Automatización — corte 3: acción «escalar a funcionario» (#24)** ([ADR 0026](docs/adr/0026-motor-de-reglas-de-automatizacion.md)): las reglas de automatización ahora soportan, además de «crear tarea», la acción **`Escalate`** que ante su evento (típicamente `conversation.created`) escala la conversación —crea el ticket, lo encola y lo asigna si aplica— con un motivo que admite marcadores `{campo}`. El `AutomationRule` gana `ActionEscalateReason` y `ActionTaskTitle` pasa a ser opcional (aplica solo a CreateTask); el endpoint valida la coherencia acción/parámetros (CreateTask exige título → 400). La UI `/automatizaciones` incorpora un selector de acción con los campos correspondientes. **Verificado E2E** (regla Escalate + evento → ticket creado, conversación a `InQueue`, motivo renderizado `…por WebChat`; CreateTask sin título → 400).

## [0.13.0] - 2026-06-20

### Added

- **Automatización — corte 2: motor de reglas "evento → condición → acción" (#24)** ([ADR 0026](docs/adr/0026-motor-de-reglas-de-automatizacion.md)): nueva entidad `AutomationRule` (esquema del tenant) y endpoints `/automation-rules` (TenantAdmin) para declarar reglas que, ante un evento de dominio (`conversation.created`, `ticket.assigned/resolved`, `survey.recorded`), evalúan condiciones (AND sobre campos del evento) y ejecutan una acción (hoy: **crear tarea**, con título que admite marcadores `{campo}` y responsable opcional). Se introduce `IDomainEventPublisher` como **punto único de publicación** que reparte cada evento a los webhooks salientes y al nuevo `AutomationDispatcher` (best-effort, no rompe la operación origen); lógica de evaluación y plantillas pura y testeable (`RuleEvaluator`, `TemplateRenderer`). Nueva página `/automatizaciones` en `Mimo.App`. **Verificado E2E** (evento real crea la tarea con título renderizado y conversación enlazada; condición que no coincide no dispara; trigger inválido 400; authz 401; borrar regla detiene el disparo) + unit tests.

### Changed

- Los 5 puntos que publicaban eventos de dominio (`/conversations`, `/integration/v1/conversations`, encuesta, asignación y resolución de ticket) pasan a usar `IDomainEventPublisher` en lugar de llamar directamente a `IWebhookPublisher`; el comportamiento de webhooks no cambia (el fan-out los sigue invocando).

## [0.12.0] - 2026-06-20

### Added

- **Automatización — corte 1: tareas operativas (#24)** ([ADR 0025](docs/adr/0025-tareas-operativas.md)): nueva entidad `WorkTask` (esquema del tenant, tabla `tasks`) y endpoints `/tasks` (política Agent) para **listar** (filtro por estado y responsable), **crear**, **actualizar** (incluido el estado) y **eliminar** tareas con responsable, vencimiento, vínculos opcionales a conversación/ticket y ciclo de vida (`Pending`/`InProgress`/`Done`/`Cancelled`): pasar a estado terminal fija `CompletedAt` y reabrir lo limpia (reglas puras `WorkTaskStatusRules`). Nueva página `/tareas` en `Mimo.App` (bandeja con filtro por estado, alta/edición con responsable y vencimiento, borrado). Es la base sobre la que el motor de reglas creará tareas como acción. **Verificado E2E** (crear; Done fija completedAt; reabrir lo limpia; filtros por estado; authz 401; 204→404) + unit tests de las reglas de estado.

## [0.11.0] - 2026-06-20

### Added

- **IA extensible — corte 1: catálogo de servidores MCP externos (#23)** ([ADR 0024](docs/adr/0024-catalogo-de-servidores-mcp-externos.md)): nueva entidad `McpServer` (esquema del tenant) y endpoints `/mcp-servers` (política TenantAdmin) para **registrar/listar/actualizar/eliminar** servidores MCP de terceros por organización — endpoint, allowlist de herramientas (deny-by-default), token de autenticación **cifrado en reposo y write-only** (`ISecretProtector`, ADR 0010; no se devuelve, un guardado en blanco lo conserva) y habilitación. Validación de endpoint (URL absoluta http/https → 400), nombre único (409), allowlist normalizada y timeout acotado (1–120 s). Nueva página `/mcp-servers` en `Mimo.App`. Es el catálogo: la invocación mediada por el gateway (con anti-SSRF), scopes y auditoría quedan para un corte posterior. **Verificado E2E** (CRUD; token cifrado en reposo y nunca expuesto; blanco conserva / rotación reemplaza; allowlist deduplicada; authz 401; 204→404).

## [0.10.0] - 2026-06-20

### Added

- **Mejora continua — corte 2: bandeja de curación de conocimiento (#22, Fase 1 — HITL)** ([ADR 0023](docs/adr/0023-instrumentacion-de-vacios-de-conocimiento.md)): nueva entidad `KnowledgeSuggestion` (esquema del tenant) y endpoints `/knowledge/suggestions` (política TenantAdmin) para **listar** (filtro por estado), **crear** (manual o desde un vacío detectado), **aprobar** y **descartar** borradores de conocimiento. **Aprobar publica un `Document`** (visibilidad pública, reutilizando el camino de creación con embedding que degrada si el LLM no está disponible); aprobar/descartar exigen estado pendiente (→ 409 si ya se revisó). Nueva página `/mejora-continua` en `Mimo.App` que muestra los vacíos detectados (con acción "Crear borrador" prellenada) y la bandeja de sugerencias con aprobar/descartar y filtro por estado. El `TopSimilarity` de los vacíos pasa a no-nullable (0 = sin coincidencias) para que el SDK lo tipe como `double` (evita `UntypedNode`, #18). **Verificado E2E** (ciclo crear→aprobar→documento publicado→descartar→409 al re-revisar; filtros por estado; authz 401).

## [0.9.0] - 2026-06-20

### Added

- **Mejora continua — corte 1: instrumentación de vacíos de conocimiento (#22, Fase 1)** ([ADR 0023](docs/adr/0023-instrumentacion-de-vacios-de-conocimiento.md)): el orquestador ahora captura, por cada consulta atendida por IA, la **mejor similitud de recuperación** y un flag `knowledge_gap` (umbral por defecto 0.75) en una nueva tabla `knowledge_query_signals` del esquema del tenant. `IVectorSearchService.SearchScoredAsync` expone la similitud (`1 - distancia coseno`) sin cambiar el contrato del chat; el registro es **best-effort** (nunca interrumpe la atención) y **no** genera señales falsas cuando el LLM/embeddings no están disponibles (la búsqueda se degrada). Nuevo endpoint `GET /knowledge/gaps` (política TenantAdmin) que lista los vacíos recientes — base de la futura bandeja de curación de conocimiento. Aislado por organización (nunca cross-tenant). **Verificado E2E** (migración, filtro gap-only, authz, `limit`; sin falso positivo en degradación) + unit tests del evaluador de umbral.

## [0.8.4] - 2026-06-20

### Added

- **CRUD y UI de categorías de documentos (#17)**: nuevo endpoint `/document-categories` (`DocumentCategoryEndpoints`, sobre el esquema del tenant) — lectura para cualquier funcionario (poblar el desplegable) y alta/borrado para TenantAdmin. El alta valida nombre duplicado y categoría padre inexistente (409); el borrado se bloquea (409) si hay documentos o subcategorías que la referencian. En el front, `DocumentCategoriesService` (cliente Kiota) y la página `/conocimiento` incorporan el desplegable de categoría en el formulario de documento y un modal «Nueva categoría» que la crea y la selecciona al vuelo.

## [0.8.3] - 2026-06-20

### Added

- **Paginación server-side de funcionarios (#16)**: endpoint `GET /agents/paged` (`PagedResult<AgentResponse>`, `page`/`pageSize`/`isActive`) reusando `ToPagedResultAsync`, componente reutilizable `MimoPager` y paginación (20/pág) en la página `/funcionarios`. `GET /agents` plano se conserva para los selectores (chat interno).

## [0.8.2] - 2026-06-20

### Changed

- La política de IA por plan (`PUT /ai/plan-policies`) **valida `AllowedProviders`** contra los conectores de IA configurados: rechaza con 400 (listando los desconocidos) los proveedores sin conector, evitando habilitar proveedores que el gateway no podría resolver (#11).

## [0.8.1] - 2026-06-20

### Added

- **Gestión autenticada de super administradores (#10)** en `Mimo.Admin.Api`: endpoints `/superadmins` (política SuperAdmin) para **listar**, **crear** (correo único, contraseña hasheada) y **desactivar** otros super administradores, con guardas contra auto-desactivación y contra dejar la plataforma sin ningún SuperAdmin activo. El bootstrap del primero sigue en `/auth/setup`.

## [0.8.0] - 2026-06-20

### Added

- **Refresh tokens y revocación de sesión para funcionarios (#9)** ([ADR 0022](docs/adr/0022-refresh-tokens-de-funcionarios.md)): el login emite un refresh token (se guarda solo su hash en el esquema del tenant); `POST /auth/refresh` renueva el access token **rotando** el refresh (el anterior queda inválido) y `POST /auth/logout` lo revoca. Vida configurable (`Jwt:RefreshTokenExpiryDays`, 7 días por defecto). Alcance: `Mimo.Api` (funcionarios); SuperAdmin y el cableado en el front quedan como seguimiento.

## [0.7.2] - 2026-06-20

### Added

- **Chatbot por opciones — llamada a API externa (corte 3, #27)**: nuevo nodo **`ApiCall`** que hace una petición HTTP (GET/POST, URL y cuerpo con `{variable}`), opcionalmente captura la respuesta en una variable y **bifurca** según éxito/fallo. El motor sigue siendo puro: se detiene en el nodo y el orquestador ejecuta la llamada (bucle acotado) y reanuda. Ver [ADR 0021](docs/adr/0021-chatbot-por-opciones-flujos-guiados.md).

### Security

- **Controles anti-SSRF** para los nodos `ApiCall`: **allow-list de hosts deny-by-default** (`Chatbot:ApiCall:AllowedHosts`), **bloqueo de IPs internas/metadata** (link-local/169.254 siempre; loopback/privadas salvo `AllowLocalhost`), **sin redirecciones automáticas**, timeout y tamaño de respuesta acotados.

## [0.7.1] - 2026-06-20

### Added

- **Chatbot por opciones — captura de datos (corte 2, #27)**: nuevo nodo **`Input`** que pide un dato, lo **valida** (regex opcional con timeout anti-ReDoS) y lo guarda en una **variable**; re-prompt ante dato inválido. **Variables de flujo** persistidas por conversación (`FlowState`, JSON) y **sustitución `{variable}`** en los textos de los nodos. El motor sigue siendo puro. Ver [ADR 0021](docs/adr/0021-chatbot-por-opciones-flujos-guiados.md).

## [0.7.0] - 2026-06-20

### Added

- **Chatbot por catálogo de opciones — flujos guiados deterministas (corte 1, #27)** ([ADR 0021](docs/adr/0021-chatbot-por-opciones-flujos-guiados.md)): modo de atención alternativo al IA, sin LLM. Modelo `ChatbotFlow` por tenant (uno activo, definición JSON), motor determinista con nodos **mensaje/menú/escalar**, estado por conversación (`FlowNodeId`) e integración en el orquestador (si hay flujo activo, el bot se conduce por el flujo; si no, sigue con RAG+LLM). Endpoints `/chatbot/flows` (crear/activar/listar, TenantAdmin). El escalado reutiliza la derivación a funcionario existente.

## [0.6.7] - 2026-06-20

### Changed

- **CORS de producción con orígenes explícitos + `AllowCredentials`** en `Mimo.Api`, para habilitar los hubs SignalR autenticados cross-origin (la consola en `mimo.app` conecta a los hubs en `mimo.api`). Se configura con `Cors:AllowedOrigins` (placeholder documentado en `appsettings.json`). Development sigue permisivo (`AllowAnyOrigin`).

## [0.6.6] - 2026-06-20

### Changed

- **Provisión automática del CLI de Tailwind** en `Mimo.Ui` (target MSBuild `EnsureTailwindCli`): si el binario standalone (gitignored, ≈40 MB) no está, se descarga la versión fijada (v3.4.17) seleccionando el artefacto por SO/arquitectura antes de compilar el CSS. Evita que CI y los clones nuevos queden sin `mimo.min.css` (sin estilos) por falta del binario.

## [0.6.5] - 2026-06-20

### Security

- **Token de canal cifrado en reposo** en `TenantConfiguration`: `TelegramBotToken` se cifra con `ISecretProtector` (Data Protection, ADR 0010) al guardar y es **write-only** — `GET /tenant/configuration` lo enmascara y un guardado en blanco conserva el valor existente (no lo borra). Antes viajaba y se almacenaba en claro en el JSON de configuración.

## [0.6.4] - 2026-06-20

### Added

- **Edición del horario de atención en `/configuracion`** (`Mimo.App`): hora de inicio/fin y días de la semana, además del interruptor y el mensaje fuera de horario que ya existían. Round-trip verificado contra el endpoint de configuración. (Los timeouts `int?` siguen sin editarse por la limitación OpenAPI 3.0 → `UntypedNode`, pendiente #18b.)

## [0.6.3] - 2026-06-20

### Security

- **Corregida la vulnerabilidad NU1903** (`System.Security.Cryptography.Xml 9.0.5`, severidad alta, transitiva vía `Microsoft.AspNetCore.DataProtection.Extensions` en `Mimo.Worker` y `Mimo.UnitTests`). Se actualizaron las referencias de Data Protection a **10.0.9** (la GA 10.0.0 tenía además un CVE crítico propio). `dotnet list package --vulnerable --include-transitive` queda sin paquetes vulnerables.

## [0.6.2] - 2026-06-20

### Fixed

- **La transferencia de tickets perdía el historial**: `/tickets/transfer` no persistía el `TransferRecord` y, aunque se persistiera, un FK con cascada lo borraba al reemplazarse el ticket de origen (conversación↔ticket 1:1). Ahora el `TransferRecord` se ancla a la **conversación** (FK con cascada a `conversations`; `TicketId` queda informativo) y persiste aunque el ticket de origen se elimine. Migración `TransferRecordByConversation`.

## [0.6.1] - 2026-06-20

### Changed

- **Respuestas tipadas en los endpoints restantes de `Mimo.Api`** (`.Produces<T>()`): Survey (`POST`), Conversations (`POST /` y `POST /messages`). De paso, `POST /documents/reindex` y `PATCH /tickets/notes` ahora responden **204 No Content** (antes objeto anónimo / sin tipo). El cliente Kiota regenerado queda sin tipos `UntypedNode`/`Stream`, salvo los modelos de configuración con `int?` (limitación de OpenAPI 3.0, pendiente #18) y el webhook entrante de canales (sin cuerpo).

## [0.6.0] - 2026-06-20

### Added

- **`Mimo.Worker` escalable horizontalmente**: claim atómico de entregas de webhook con `FOR UPDATE SKIP LOCKED` y marca de propietario (`claimed_by`/`claimed_at`, estado `InProgress`), de modo que con varias instancias del worker cada entrega se procesa **exactamente una vez**. Recupera claims vencidos (>5 min) de un worker caído. Sin dependencias nuevas (solo PostgreSQL). [ADR 0020](docs/adr/0020-claim-atomico-de-entregas-de-webhook.md).

## [0.5.2] - 2026-06-19

### Added

- **Throttling por conexión en `ChatHub`**: límite de 30 mensajes/min por conexión WebSocket (alineado con el límite REST), con evento `Error` al exceder y limpieza al desconectar. Cubre el hueco del rate limiting HTTP, que no aplica a mensajes enviados sobre un WebSocket ya abierto.

### Changed

- **IP real tras proxy** (`UseForwardedHeaders`): procesa `X-Forwarded-For`/`X-Forwarded-Proto` para que el rate limiting y los logs usen la IP real del cliente. Por defecto solo confía en loopback; los proxies/balanceadores externos se declaran en `ForwardedHeaders:KnownProxies`.

## [0.5.1] - 2026-06-19

### Changed

- `Mimo.Admin.Api` con respuestas **totalmente tipadas** (`.Produces<T>()`): `POST /auth/setup` devuelve `SetupSuperAdminResponse` y `POST /ai/connectors/activate` responde **204 No Content** (antes objeto anónimo). El SDK del Admin regenerado queda sin tipos `UntypedNode`/`Stream`.

## [0.5.0] - 2026-06-19

### Added

- **Rate limiting / anti-abuso en la superficie pública del WebChat**: rate limiter integrado de .NET particionado por IP del cliente. Crear conversación, encuesta y *handshake* del hub a 10/min (`webchat-start`); envío de mensajes (que cuesta LLM) a 30/min (`webchat-chat`). Al exceder responde **429** con `Retry-After`. Las lecturas (`/webchat/config`, detalle) quedan sin límite.

## [0.4.0] - 2026-06-19

### Added

- **WebChat embebible — funcionario en vivo y encuesta (Fase E corte 2)**: el widget conecta `ChatHub` (SignalR) y recibe en tiempo real los mensajes del funcionario/bot (`MessageReceived`), el aviso `AgentJoined` y los cambios de estado (`StatusChanged`); al cerrarse la conversación muestra una **encuesta de satisfacción** embebida (1–5 + comentario). Si SignalR no conecta, usa respaldo REST. [ADR 0019](docs/adr/0019-acceso-cross-origin-anonimo-al-chathub.md).

### Changed

- `ChatHub` (`/hubs/chat`) expone CORS abierto (política `webchat`) para el *negotiate* cross-origin del widget.
- `TenantResolutionMiddleware` acepta el tenant por query (`?tenant_slug=…`) en rutas `/hubs`, porque el navegador no puede fijar cabeceras en el handshake WebSocket (análogo a `?access_token` del JWT en hubs).

## [0.3.1] - 2026-06-19

### Fixed

- El orquestador de conversación devolvía **500** al ciudadano cuando el LLM no estaba disponible (la búsqueda semántica por embeddings no estaba protegida; p. ej. sin credenciales → 401). Ahora la búsqueda degrada a *sin contexto* y, si el chat también falla, responde **200** con un mensaje de respaldo. Mejora la experiencia del WebChat embebible (Fase E).

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

[Unreleased]: https://github.com/evillabong/chatbot-llm/compare/v0.18.1...HEAD
[0.18.1]: https://github.com/evillabong/chatbot-llm/compare/v0.18.0...v0.18.1
[0.18.0]: https://github.com/evillabong/chatbot-llm/compare/v0.17.0...v0.18.0
[0.17.0]: https://github.com/evillabong/chatbot-llm/compare/v0.16.0...v0.17.0
[0.16.0]: https://github.com/evillabong/chatbot-llm/compare/v0.15.0...v0.16.0
[0.15.0]: https://github.com/evillabong/chatbot-llm/compare/v0.14.0...v0.15.0
[0.14.0]: https://github.com/evillabong/chatbot-llm/compare/v0.13.0...v0.14.0
[0.13.0]: https://github.com/evillabong/chatbot-llm/compare/v0.12.0...v0.13.0
[0.12.0]: https://github.com/evillabong/chatbot-llm/compare/v0.11.0...v0.12.0
[0.11.0]: https://github.com/evillabong/chatbot-llm/compare/v0.10.0...v0.11.0
[0.10.0]: https://github.com/evillabong/chatbot-llm/compare/v0.9.0...v0.10.0
[0.9.0]: https://github.com/evillabong/chatbot-llm/compare/v0.8.4...v0.9.0
[0.8.4]: https://github.com/evillabong/chatbot-llm/compare/v0.8.3...v0.8.4
[0.8.3]: https://github.com/evillabong/chatbot-llm/compare/v0.8.2...v0.8.3
[0.8.2]: https://github.com/evillabong/chatbot-llm/compare/v0.8.1...v0.8.2
[0.8.1]: https://github.com/evillabong/chatbot-llm/compare/v0.8.0...v0.8.1
[0.8.0]: https://github.com/evillabong/chatbot-llm/compare/v0.7.2...v0.8.0
[0.7.2]: https://github.com/evillabong/chatbot-llm/compare/v0.7.1...v0.7.2
[0.7.1]: https://github.com/evillabong/chatbot-llm/compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/evillabong/chatbot-llm/compare/v0.6.7...v0.7.0
[0.6.7]: https://github.com/evillabong/chatbot-llm/compare/v0.6.6...v0.6.7
[0.6.6]: https://github.com/evillabong/chatbot-llm/compare/v0.6.5...v0.6.6
[0.6.5]: https://github.com/evillabong/chatbot-llm/compare/v0.6.4...v0.6.5
[0.6.4]: https://github.com/evillabong/chatbot-llm/compare/v0.6.3...v0.6.4
[0.6.3]: https://github.com/evillabong/chatbot-llm/compare/v0.6.2...v0.6.3
[0.6.2]: https://github.com/evillabong/chatbot-llm/compare/v0.6.1...v0.6.2
[0.6.1]: https://github.com/evillabong/chatbot-llm/compare/v0.6.0...v0.6.1
[0.6.0]: https://github.com/evillabong/chatbot-llm/compare/v0.5.2...v0.6.0
[0.5.2]: https://github.com/evillabong/chatbot-llm/compare/v0.5.1...v0.5.2
[0.5.1]: https://github.com/evillabong/chatbot-llm/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/evillabong/chatbot-llm/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/evillabong/chatbot-llm/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/evillabong/chatbot-llm/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/evillabong/chatbot-llm/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/evillabong/chatbot-llm/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/evillabong/chatbot-llm/releases/tag/v0.1.0
