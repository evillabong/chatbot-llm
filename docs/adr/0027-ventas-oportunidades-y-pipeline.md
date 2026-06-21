# 0027. Ventas: oportunidades y pipeline (corte 1)

- Estado: Aceptado
- Fecha: 2026-06-21
- Decisores: evill

## Contexto

La propuesta comercial (§4.10) describe **Ventas** como una **capacidad opcional** (no core): captura
de oportunidades desde la conversación, **pipeline** por etapas, tareas de seguimiento y sincronización
opcional con CRM externo. Es la pendiente #26. El enfoque de producto define la atención al cliente
como núcleo y las ventas como una capacidad **opt-in por plan**.

La sincronización con CRM externo depende de credenciales/proveedores (no disponible aquí); este corte
entrega la **base verificable**: la entidad oportunidad y su pipeline.

## Decisión

- **Entidad `Opportunity`** en el **esquema del tenant** (`opportunities`): título, contacto
  (nombre/correo/teléfono), **etapa** (`OpportunityStage`: New/Qualified/Proposal/Won/Lost), **monto**
  estimado, conversación de origen opcional, responsable opcional y notas. Aislada por organización
  (ADR 0009).
- **Reglas de pipeline puras** (`OpportunityStageRules`): `IsClosed` (Won/Lost) y `ResolveClosedAt`
  (fija `ClosedAt` al cerrar, lo conserva si ya estaba, lo limpia al reabrir). Unit-testeable.
- **Monto no-nullable** (`decimal`, 0 = sin monto) para que el SDK lo tipe como número y no como
  `UntypedNode` (limitación OpenAPI 3.0 con value types nullables, #18).
- **Endpoints `/opportunities`** (política **Agent**: operación de funcionarios): listar (filtro por
  etapa/responsable), crear, actualizar (incluye cambio de etapa → aplica el cierre) y eliminar.
- **UI `/ventas`** (`Mimo.App`): bandeja con filtro por etapa, alta/edición (contacto, monto, etapa,
  responsable, notas) y borrado.

## Consecuencias

### Positivas

- Entrega el núcleo del módulo de ventas reutilizando los patrones del repo, sin depender de LLM ni de
  CRMs externos. **Verificado E2E:** crear (monto decimal); pasar a `Won` fija `ClosedAt`; reabrir lo
  limpia; filtros por etapa; authz (sin token → 401); borrado 204 → 404. Unit tests de las reglas de
  etapa.

### Negativas / Costos

- **Sin gating por plan todavía:** el módulo se expone a cualquier funcionario; falta condicionar su
  disponibilidad a la `AiPlanPolicy`/entitlement del plan (coherente con "ventas opt-in por plan").
- **Pipeline de etapas fijo** (enum) en este corte; etapas configurables por tenant quedan para después.
- Faltan: **vincular tareas de seguimiento** a la oportunidad (hoy `WorkTask` enlaza conversación/ticket),
  captura asistida **desde la conversación** y la **sincronización con CRM externo** (corte posterior,
  requiere proveedor).

## Corte 2 — Tareas de seguimiento vinculadas

- `WorkTask` (#24) gana `OpportunityId` (opcional), de modo que una tarea puede ser **seguimiento** de
  una oportunidad. El endpoint `/tasks` acepta el filtro `opportunityId` y `CreateWorkTaskRequest` lo
  admite al crear.
- UI `/ventas`: cada oportunidad tiene un panel **«Tareas»** que lista su seguimiento y permite **agregar**
  una tarea (título + vencimiento) y **completarla** al vuelo, reutilizando el módulo de tareas.
- **Verificado E2E:** crear oportunidad → crear tarea con `opportunityId` (se persiste el vínculo) →
  `/tasks?opportunityId=` devuelve solo las suyas (la tarea suelta queda excluida). Migración aditiva
  `TaskOpportunityLink`.

## Alternativas consideradas

- **Reusar `Ticket` o `WorkTask` como oportunidad:** descartado; una oportunidad tiene contacto
  comercial, monto y un pipeline de ventas con semántica propia, distinta de la atención (ticket) o de
  un pendiente operativo (tarea).
- **Etapas configurables desde el inicio:** se pospone; un enum cubre el caso base y se evita el peso de
  un editor de pipeline antes de validar el módulo.
