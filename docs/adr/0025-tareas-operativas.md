# 0025. Tareas operativas como base de la automatización

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

El [anexo técnico](../anexo-tecnico-integraciones.md) §5 describe el módulo de **automatización**:
(a) motor de reglas "evento → condición → acción", (b) campañas salientes y (c) **tareas** como
entidad propia con responsable, vencimiento y estado, vinculable a conversación/ticket/oportunidad y
alimentada por flujos y por la consola. Es la pendiente #24.

Las **campañas** dependen de proveedores de canal reales (pendiente #25) y el **motor de reglas**
tiene como una de sus acciones "crear tarea". Por eso la **entidad Tarea** es la base verificable y la
dependencia natural del resto: este corte la entrega.

## Decisión

- **Entidad `WorkTask`** en el **esquema del tenant** (tabla `tasks`; el nombre de clase evita el
  choque con `System.Threading.Tasks.Task`): título, descripción, estado
  (`Pending`/`InProgress`/`Done`/`Cancelled`), responsable (`AssignedAgentId`), vencimiento (`DueAt`),
  vínculos opcionales (`ConversationId`, `TicketId`), y fechas de creación/actualización/cierre.
  Aislada por organización (ADR 0009).
- **Reglas de ciclo de vida puras** (`WorkTaskStatusRules`): `IsTerminal` (Done/Cancelled) y
  `ResolveCompletedAt` (fija `CompletedAt` al entrar a estado terminal, lo conserva si ya estaba, lo
  limpia al reabrir). Sin dependencias → unit-testeable y reutilizable por el futuro motor de reglas.
- **Endpoints `/tasks`** (política **Agent**: cualquier funcionario gestiona tareas): listar (filtro por
  estado y responsable), crear, actualizar (incluye el cambio de estado, que aplica las reglas de
  cierre) y eliminar. Convención del repo: query/headers/body.
- **UI `/tareas`** (`Mimo.App`): bandeja con filtro por estado, alta/edición (responsable desde la lista
  de funcionarios, vencimiento, estado) y borrado.

## Consecuencias

### Positivas

- Entrega la pieza concreta del módulo de automatización y deja el terreno listo para el motor de
  reglas (acción "crear tarea") sin depender de LLM ni de proveedores. **Verificado E2E:** crear;
  cambio a `Done` fija `CompletedAt`; reabrir lo limpia; filtros por estado; authz (sin token → 401);
  borrado 204 → 404. Unit tests de las reglas de estado.

### Negativas / Costos

- **No** incluye el **motor de reglas** "evento → condición → acción" (corte siguiente: suscribirse a
  los eventos ya emitidos —mensaje entrante, inactividad, cambio de estado de ticket— y ejecutar
  acciones, entre ellas crear tarea) ni las **campañas** salientes (dependen de #25).
- Sin notificaciones de vencimiento todavía (un worker podría avisar tareas vencidas; se evaluará con
  el motor de reglas).

## Alternativas consideradas

- **Reusar `Ticket` como tarea:** descartado; un ticket modela una atención al ciudadano (cola, SLA,
  asignación por rol); una tarea es un pendiente interno con semántica y ciclo de vida propios.
- **Empezar por el motor de reglas:** descartado; su acción principal ("crear tarea") necesita esta
  entidad; construir la base primero reduce el riesgo del corte del motor.
