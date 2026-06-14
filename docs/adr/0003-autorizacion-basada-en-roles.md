# 0003. Autorización basada en roles por políticas

- Estado: Aceptado
- Fecha: 2026-06-13
- Decisores: evill

## Contexto

Tras habilitar la emisión de JWT, todos los endpoints tenant-facing usaban
`RequireAuthorization()` sin distinguir capacidades: cualquier funcionario autenticado
podía gestionar funcionarios, roles o la base de conocimiento. Hacía falta separar la
administración del tenant de la operación de atención.

## Decisión

Centralizar nombres de roles y políticas en `MimoAuthorization` (Core) y aplicarlos como
políticas de autorización:

- `TenantAdmin` (claim `role = Administrador`): gestión de funcionarios, roles y
  administración de la base de conocimiento (crear/editar/desactivar/reindexar).
- `Agent` (claim `agent_id` presente): tickets, chat interno, lectura de conocimiento y
  conexión al `TicketHub`.
- `SuperAdmin` (claim `role = SuperAdmin`): única política de `Mimo.Admin.Api`.

Las políticas a nivel de grupo y de endpoint se acumulan; el token de un Administrador
incluye tanto `agent_id` como `role=Administrador`, por lo que satisface ambas en los
endpoints de escritura de documentos.

## Consecuencias

### Positivas

- Capacidades explícitas y verificables; sin literales mágicos dispersos.
- El rol "Administrador" inicial se crea al aprovisionar el tenant (ADR implícito en
  `TenantProvisioningService`), por lo que el tenant puede operar desde el primer login.

### Negativas / Costos

- Modelo de permisos de grano grueso (no por recurso/acción individual). Suficiente hoy;
  ampliable a permisos finos si se requiere.

## Alternativas consideradas

- **Permisos finos (claims por acción)**: mayor flexibilidad a costa de complejidad;
  innecesario para el conjunto actual de operaciones.
- **Autorización ad-hoc en cada handler**: propensa a inconsistencias; las políticas
  centralizan la regla.
