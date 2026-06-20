# 0020. Claim atómico de entregas de webhook (worker escalable)

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

`Mimo.Worker` ([ADR 0017](0017-host-de-workers-separado-mimo-worker.md)) entrega los webhooks
salientes recorriendo los tenants y procesando las entregas `Pending`. El diseño asumía **una sola
instancia**: con dos o más réplicas, ambas hacían el mismo `SELECT` de pendientes y entregaban el
mismo webhook → **entregas duplicadas** y carreras (pendings #30). El proyecto evita Redis
([ADR 0001](0001-eliminar-redis-usar-ef-y-memorycache.md)), así que un lock distribuido externo no
es la vía preferida.

## Decisión

- **Claim atómico en PostgreSQL con `FOR UPDATE SKIP LOCKED`.** Cada ciclo del worker genera un
  *token* de lote y ejecuta un único `UPDATE ... WHERE id IN (SELECT … FOR UPDATE SKIP LOCKED)` que
  marca un conjunto **disjunto** de entregas como `InProgress` con `claimed_by = token` y
  `claimed_at = now()`. Luego recarga por token y las procesa. Dos réplicas nunca reclaman la misma
  fila: `SKIP LOCKED` salta las bloqueadas por la otra y el cambio de estado se confirma, así que tras
  el commit cada entrega pertenece a un solo worker.
- **Estado `InProgress` (3)** + columnas `claimed_by` / `claimed_at` en `webhook_deliveries`.
- **Recuperación de claims vencidos:** si un worker cae tras reclamar pero antes de terminar, las
  filas quedan `InProgress`; el claim las vuelve a tomar cuando `claimed_at` supera los 5 min (muy por
  encima del timeout HTTP de 10 s), evitando entregas perdidas.
- **Liberación del claim:** al resolver (Delivered / Failed / reprogramada a Pending) se limpian
  `claimed_by`/`claimed_at` en el mismo `SaveChanges`.

## Consecuencias

### Positivas

- `Mimo.Worker` se puede **escalar horizontalmente** sin duplicar entregas. **Verificado E2E** con
  **dos instancias** compitiendo por 6 entregas: el receptor recibió 6 (6 ids distintos, **cero
  duplicados**) y las 6 quedaron `Delivered`.
- Sin dependencias nuevas: usa solo PostgreSQL (coherente con ADR 0001). `SKIP LOCKED` evita que las
  réplicas se bloqueen entre sí (avanzan en paralelo sobre filas distintas).

### Negativas / Costos

- El claim se hace con SQL crudo (`ExecuteSqlInterpolated`) porque EF/LINQ no expresa
  `FOR UPDATE SKIP LOCKED`; el `search_path` del tenant se fija en la misma conexión abierta.
- Las filas `InProgress` de un worker caído esperan hasta 5 min para reintentarse (compromiso entre
  no duplicar y no demorar demasiado); ajustable si hiciera falta.
- `InactivityTimeoutWorker` usa `ExecuteUpdate` idempotente (cerrar conversaciones inactivas no
  duplica efecto), por lo que no requiere claim; si en el futuro hace trabajo no idempotente, aplicar
  el mismo patrón.
