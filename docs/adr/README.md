# Architecture Decision Records (ADR)

Este directorio registra las decisiones de arquitectura relevantes del proyecto MIMO.

Un ADR documenta **una** decisión: su contexto, la opción elegida, las alternativas
consideradas y sus consecuencias. Los ADR son inmutables una vez aceptados; si una
decisión se revierte o cambia, se crea un ADR nuevo que marca al anterior como
*Reemplazado por*.

## Convención

- Formato: [MADR](https://adr.github.io/madr/) simplificado (ver [plantilla](0000-plantilla.md)).
- Nombre de archivo: `NNNN-titulo-en-kebab-case.md` con numeración incremental de 4 dígitos.
- Idioma: español (igual que el resto de la documentación).
- Estados posibles: `Propuesto`, `Aceptado`, `Reemplazado por NNNN`, `Obsoleto`.
- Al tomar una decisión arquitectónica, crear el ADR **en el mismo lote** del código
  (regla de `CONTRIBUTING.md` sobre documentación obligatoria).

## Índice

| ADR | Título | Estado |
|---|---|---|
| [0001](0001-eliminar-redis-usar-ef-y-memorycache.md) | Eliminar Redis: usar EF Core y IMemoryCache | Aceptado |
| [0002](0002-autenticacion-jwt-agentes-y-superadmin.md) | Autenticación JWT para agentes y super administradores | Aceptado |
| [0003](0003-autorizacion-basada-en-roles.md) | Autorización basada en roles por políticas | Aceptado |
| [0004](0004-configuracion-de-conectores-de-ia-en-base-de-datos.md) | Configuración de conectores de IA en base de datos | Aceptado |
| [0005](0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md) | Gateway de IA in-process con entitlements y cuotas por plan | Aceptado |
| [0006](0006-convencion-de-endpoints-sin-parametros-en-la-ruta.md) | Convención de endpoints sin parámetros en la ruta | Aceptado |
| [0007](0007-catalogo-de-planes-con-integridad-referencial.md) | Catálogo de planes con integridad referencial | Aceptado |
| [0008](0008-binding-de-tenant-al-principal-autenticado.md) | Binding del tenant al principal autenticado | Aceptado |
| [0009](0009-search-path-por-interceptor-de-conexion.md) | search_path por interceptor de conexión | Aceptado |
