# 0001. Eliminar Redis: usar EF Core y IMemoryCache

- Estado: Aceptado
- Fecha: 2026-06-09
- Decisores: evill

## Contexto

El diseño inicial contemplaba Redis para la cola de tickets (sorted set por prioridad)
y para caché de lookups frecuentes (tenant por slug). Redis añade una pieza de
infraestructura adicional que operar, monitorear y escalar. Para el volumen previsto
de esta plataforma de atención ciudadana, esa complejidad no se justifica, y ya
dependemos de PostgreSQL como almacén principal.

## Decisión

Eliminar Redis por completo. Usar **EF Core sobre PostgreSQL** para todo lo persistente
(incluida la cola de tickets) y **`IMemoryCache`** para el estado efímero sin necesidad
de persistencia.

- La cola de tickets se implementa con una transacción `Serializable` + `CreateExecutionStrategy`
  para un *dequeue* atómico, en lugar de un sorted set de Redis.
- La resolución de tenant por slug se cachea en `IMemoryCache` (TTL 5 min), invalidando
  ante tenant inexistente o inactivo.

## Consecuencias

### Positivas

- Una pieza menos de infraestructura (sin servicio Redis en `docker-compose`).
- Un único almacén transaccional; consistencia más simple de razonar.

### Negativas / Costos

- `IMemoryCache` es por instancia: con varias réplicas de la API el caché no se comparte
  (aceptable por el TTL corto; revisar si se escala horizontalmente de forma agresiva).
- El *dequeue* por transacción serializable puede reintentar bajo contención alta.

## Alternativas consideradas

- **Mantener Redis**: máximo rendimiento de cola/caché distribuido, descartado por
  complejidad operativa innecesaria para el volumen objetivo.
- **Caché distribuido en tabla PostgreSQL**: redundante frente a `IMemoryCache` para
  datos efímeros de bajo costo de recálculo.
