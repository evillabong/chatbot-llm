# 0021. Chatbot por opciones: flujos guiados deterministas (corte 1)

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

La propuesta (§4.16) plantea un modo de atención **alternativo y configurable** al modo IA: un
chatbot determinista por **catálogo de opciones** (menús/árbol), útil cuando el negocio prefiere
respuestas controladas y predecibles, o no quiere/no puede usar LLM para ciertos flujos. El bot
actual responde siempre vía el orquestador con RAG+LLM ([ADR 0005](0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md)).

## Decisión (corte 1)

- **Modelo `ChatbotFlow` por tenant** (esquema del tenant): `Definition` es un JSON con el nodo de
  entrada y los nodos. Índice **único parcial** sobre `is_active` → a lo sumo un flujo activo por
  tenant. Gestión bajo JWT/TenantAdmin en `/chatbot/flows` (crear/activar/listar).
- **Tipos de nodo (corte 1):** `Message` (muestra texto y continúa solo al `Next`), `Menu` (texto +
  opciones; espera la elección del usuario) y `Escalate` (deriva a funcionario). Captura/validación de
  datos, llamadas a APIs externas y constructor visual quedan para cortes siguientes.
- **Motor determinista puro** (`IChatbotFlowEngine`): sin BD ni estado; dado flujo + nodo actual +
  entrada, calcula el siguiente paso (encadena nodos de mensaje hasta detenerse en menú/escalado;
  re-muestra el menú ante opción inválida; corta ciclos). Al ser puro, es fácil de probar.
- **Estado por conversación:** `Conversation.FlowNodeId` guarda el nodo actual. Null = flujo no
  iniciado o terminado.
- **Selección de modo = presencia de flujo activo:** si el tenant tiene un flujo activo, el
  orquestador conduce las conversaciones del bot por el flujo (sin IA); si no, sigue el camino
  RAG+LLM. Así "activar un flujo" ES la configuración del modo opciones (sin un flag aparte).
- **Integración en el orquestador:** en `HandleIncomingMessageAsync`, tras descartar conversaciones ya
  atendidas por un funcionario y antes de la búsqueda semántica/LLM, si hay flujo activo se delega al
  motor; el escalado del flujo reutiliza la misma derivación a humano que el modo IA (MCP/ticket).

## Consecuencias

### Positivas

- Modo de atención **sin IA**, determinista y barato (no consume LLM), activable por tenant.
  **Verificado E2E:** menú → opción (encadena a mensaje y vuelve al menú) → opción inválida (re-prompt)
  → escalar (crea ticket, `status=InQueue`, `FlowNodeId` se limpia). + pruebas unitarias del motor.
- Reutiliza el orquestador y la derivación a humano existentes; el motor puro es testeable aislado.

### Negativas / Costos

- Corte 1 limitado a menú/mensaje/escalar: faltan nodos de **captura/validación de datos** y de
  **llamada a API externa** con bifurcación, y un **constructor visual** (cortes siguientes).
- El modo se infiere de "hay flujo activo"; si en el futuro se quiere convivencia fina IA+flujo por
  intención, hará falta una configuración explícita.
- La definición se valida superficialmente al crear (existe el nodo de entrada); no se valida aún que
  todas las transiciones referencien nodos existentes.

## Actualización — corte 2 (captura de datos)

- Nuevo tipo de nodo **`Input`**: pide un dato, lo **valida** (regex opcional, con timeout anti-ReDoS)
  y lo guarda en una **variable**; ante dato inválido re-muestra el prompt con el mensaje de error.
- **Variables de flujo** persistidas por conversación en `Conversation.FlowState` (JSON). Los textos
  de cualquier nodo admiten **sustitución `{variable}`** con lo capturado.
- El motor sigue siendo puro (sin I/O): `Process` ahora recibe y devuelve el estado de variables.
- **Verificado E2E:** captura de nombre/correo en turnos sucesivos, re-prompt ante correo inválido y
  sustitución `{nombre}`/`{email}` en mensajes posteriores; `flow_state` persiste en BD.
- Pendiente (corte 3): nodos de **llamada a API externa** con bifurcación (introducen I/O y riesgo de
  SSRF; requieren control de seguridad propio) y el constructor visual.

## Actualización — corte 3 (llamada a API externa)

- Nuevo tipo de nodo **`ApiCall`**: hace una petición HTTP (GET/POST, URL y cuerpo con `{variable}`),
  opcionalmente **captura** el cuerpo de la respuesta en una variable y **bifurca** a `SuccessNext`
  (2xx) o `FailureNext`.
- **El motor sigue siendo puro:** al llegar a un `ApiCall` se **detiene** y lo reporta
  (`PendingApiCall`); el **orquestador** ejecuta la llamada (I/O) y reanuda con `ResolveFrom` según el
  resultado. Bucle acotado (máx. 5 llamadas por turno) para evitar cadenas infinitas.
- **Controles anti-SSRF** (`ChatbotApiCaller`, `Chatbot:ApiCall:*`):
  - **Allow-list de hosts obligatoria** (deny por defecto; vacía = ninguna llamada permitida).
  - **Bloqueo de IPs internas**: link-local/metadata (169.254/fe80, incl. 169.254.169.254) **siempre**;
    loopback/privadas/ULA salvo `AllowLocalhost=true` (dev/integración interna).
  - **Sin redirecciones automáticas**, **timeout** corto y **tamaño de respuesta** acotado.
- **Verificado E2E:** opción que llama a un host permitido → captura la respuesta y bifurca a éxito;
  opción a un host fuera de la allow-list → **bloqueada** y bifurca a fallo. + pruebas unitarias de la
  allow-list y del bloqueo de IPs.
- Residual: **DNS rebinding** (la validación resuelve el host y bloquea IPs internas, pero no fija la
  conexión a la IP validada) y el **constructor visual** quedan para más adelante.
