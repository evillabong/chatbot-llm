# 0026. Motor de reglas de automatización (evento → condición → acción)

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

El [anexo técnico](../anexo-tecnico-integraciones.md) §5 define el módulo de **automatización**: un
motor de reglas "evento → condición → acción" que se apoya en los eventos que el sistema **ya emite**
(conversación creada, ticket asignado/resuelto, encuesta registrada). La [entidad Tarea](0025-tareas-operativas.md)
(corte 1 del #24) dejó lista la acción principal: "crear tarea". Este corte añade el motor.

Hoy esos eventos se publican llamando directamente a `IWebhookPublisher.PublishAsync(...)` en cada
endpoint. Hacía falta un punto donde, además de los webhooks, reaccione la automatización, sin duplicar
la llamada en cada sitio ni acoplar webhooks con reglas.

## Decisión

- **Punto único de publicación `IDomainEventPublisher`** (fan-out): reparte el evento a (a) los webhooks
  salientes (#29) y (b) el motor de automatización (#24). Los 5 sitios que publicaban eventos pasan a
  depender de esta interfaz (mismo método `PublishAsync(eventType, payload, ct)`); cada consumidor sigue
  siendo **best-effort** por su cuenta. Así, agregar consumidores futuros no toca los endpoints.
- **Entidad `AutomationRule`** (esquema del tenant): `TriggerEvent` (uno de `WebhookEventTypes`),
  condiciones (JSON, lista de `{field, value}`), acción (`CreateTask`) con `ActionTaskTitle` (admite
  marcadores `{campo}`) y responsable opcional, `IsEnabled`.
- **Lógica pura y testeable:** `RuleEvaluator.Matches` (AND sobre campos del evento; sin condiciones →
  siempre coincide) y `TemplateRenderer.Render` (sustituye `{campo}` por el dato del evento; deja los
  desconocidos). Sin dependencias.
- **`AutomationDispatcher`** (best-effort): ante un evento, aplana el payload a un diccionario, carga las
  reglas **habilitadas** para ese disparador, evalúa condiciones y ejecuta la acción (crea `WorkTask`
  con título renderizado, enlazada a la conversación/ticket si el payload los trae). Un fallo se registra
  y **no** propaga (no rompe la operación que originó el evento).
- **Endpoints `/automation-rules`** (TenantAdmin): listar, crear, actualizar, eliminar; valida que el
  `TriggerEvent` sea soportado (→ 400). **UI `/automatizaciones`** en `Mimo.App`.

## Consecuencias

### Positivas

- El asistente automatiza trabajo sin LLM ni proveedores externos, reutilizando los eventos existentes.
  **Verificado E2E:** crear regla; disparar `conversation.created` (POST anónimo) → crea la tarea con el
  título renderizado (`Atender a {externalUserId}` → `Atender a cliente-777`) y la conversación enlazada;
  una condición que no coincide (`channel=Telegram` sobre un chat WebChat) **no** dispara; al ajustarla a
  `channel=WebChat` sí; trigger inválido → 400; authz (sin token → 401); borrar la regla detiene el
  disparo. Unit tests del evaluador y del renderer.
- El fan-out deja un único lugar para enchufar futuros consumidores de eventos.

### Negativas / Costos

- **Acciones limitadas a "crear tarea"** en este corte; faltan etiquetar/enrutar/escalar/disparar
  encuesta y condiciones más ricas (operadores ≠, contiene, rangos) — se sumarán de forma aditiva.
- Las condiciones operan sobre el **payload plano** del evento (propiedades de primer nivel como string);
  estructuras anidadas no se evalúan todavía.
- Las **campañas** salientes del §5 siguen pendientes (dependen de proveedores de canal reales, #25).

## Corte 3 — Acción «escalar a funcionario»

Amplía el motor con una segunda acción, sin tocar el diseño del fan-out:

- **`AutomationActionType.Escalate`**: la regla, ante su evento (típicamente `conversation.created`),
  escala la conversación llamando a `IMcpToolProvider.RequestHumanAgentAsync(conversationId, reason)`
  —que crea el ticket, lo encola y, si aplica, lo asigna— con un **motivo** que admite marcadores
  `{campo}`. Si el evento no trae conversación, se omite (log).
- El `AutomationRule` gana `ActionEscalateReason` (y `ActionTaskTitle` pasa a ser opcional: aplica solo
  a CreateTask). El endpoint valida la coherencia acción/parámetros (CreateTask exige título → 400).
- `RequestHumanAgentAsync` **no** republica eventos de dominio, así que escalar no reentra en el motor.
- **Verificado E2E:** regla Escalate + `conversation.created` → se crea el ticket, la conversación pasa
  a `InQueue` y el motivo queda renderizado (`Escalada automatica por {channel}` → `…por WebChat`);
  crear una regla CreateTask sin título → 400. UI `/automatizaciones` con selector de acción.

## Alternativas consideradas

- **Llamar al dispatcher en cada endpoint junto al webhook:** descartado; duplica la llamada en 5 sitios
  y es fácil omitirla en eventos nuevos. El fan-out centraliza.
- **Un bus de eventos / colas:** excesivo para el alcance actual; el fan-out in-process en el ámbito de
  la petición basta y mantiene la simplicidad (los consumidores ya son best-effort).
- **Motor de reglas genérico con DSL:** sobredimensionado; un modelo simple (igualdad AND + una acción)
  cubre los casos iniciales y se amplía sin romper el contrato.
