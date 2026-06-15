# 0009. search_path por interceptor de conexión

- Estado: Aceptado
- Fecha: 2026-06-15
- Decisores: evill

## Contexto

El aislamiento multi-tenant se basa en el `search_path` de PostgreSQL (esquema por tenant).
El middleware fijaba el esquema con un único `SET search_path` sobre `TenantDbContext` por
petición. Al ejecutarlo contra Postgres real se descubrieron dos fallas:

1. Se usaba `ExecuteSqlAsync($"SET search_path TO {schema}")`, que **parametriza** el valor
   (`SET search_path TO $1`) — inválido, porque un identificador no puede ser parámetro (error 42601).
2. Aun corregido, con **pooling de conexiones** el `SET` no persiste: cada operación de EF puede
   tomar otra conexión del pool, de modo que las consultas del endpoint corrían sobre el esquema
   equivocado (o `public`).

## Decisión

Aplicar el `search_path` mediante un **interceptor de conexión** (`SearchPathConnectionInterceptor`)
que lo fija en **cada apertura de conexión** de `TenantDbContext`, según un portador scoped por
petición (`ITenantSchemaProvider`) que establece el middleware de resolución de tenant.

- El middleware ya no ejecuta SQL; solo asigna `ITenantSchemaProvider.Schema`.
- El interceptor ejecuta `SET search_path TO "<schema>", public` en `ConnectionOpened(Async)`.
- El nombre de esquema se construye con un patrón seguro (`[a-z0-9_]`), por lo que se interpola
  (los identificadores no se pueden parametrizar).
- En el aprovisionamiento de un tenant (servicio aparte) se mantiene **una conexión abierta**
  durante `CREATE SCHEMA` + migraciones + seed, para que el `search_path` persista allí también.

## Consecuencias

### Positivas

- Fiable con pooling: toda conexión de la petición opera sobre el esquema del tenant.
- El middleware queda libre de I/O de base de datos para la resolución del esquema.

### Negativas / Costos

- Un `SET search_path` extra por apertura de conexión (costo despreciable).
- El interceptor depende de un servicio scoped; debe registrarse junto a `TenantDbContext`
  con el proveedor de servicios por petición.

## Notas

Descubierto al ejecutar el flujo completo contra PostgreSQL local (sin Docker). En el mismo
lote se corrigieron: el remapeo del claim `role` en JWT (rompía la autorización por roles),
y un `CREATE INDEX CONCURRENTLY` dentro de transacción en la migración del tenant.
