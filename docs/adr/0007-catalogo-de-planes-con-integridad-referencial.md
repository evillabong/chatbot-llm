# 0007. Catálogo de planes con integridad referencial

- Estado: Aceptado
- Fecha: 2026-06-14
- Decisores: evill

## Contexto

`Tenant.Plan` era un string libre sin validación, y `AiPlanPolicy.PlanCode` lo referenciaba
por igualdad de string **sensible a mayúsculas**. Consecuencia: un tenant con plan `"Pro"` y
una política `"pro"` no coincidían y el gateway de IA caía **silenciosamente** en modo
permisivo (sin aplicar entitlements ni cuotas). El control por plan del [ADR 0005](0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md)
era, por tanto, frágil.

## Decisión

Introducir un **catálogo de planes** (`Plan`, esquema public) con el código como clave
estable, e imponer **integridad referencial**:

- `Plan.Code` es clave alternativa única (minúsculas canónicas vía `Plan.NormalizeCode`).
- `Tenant.Plan` → FK a `Plan.Code` (`OnDelete: Restrict`): un tenant no puede tener un plan inexistente.
- `AiPlanPolicy.PlanCode` → FK a `Plan.Code` (`OnDelete: Cascade`).
- Al crear un tenant, el código de plan se normaliza y se valida contra el catálogo (400 si no existe).
- El gateway compara el plan de forma **insensible a mayúsculas**, eliminando el desajuste silencioso.
- `PlanSeeder` siembra planes por defecto (`free`, `pro`) para que el aprovisionamiento funcione de inmediato.

## Consecuencias

### Positivas

- El control por plan deja de fallar en silencio: o aplica la política, o el plan ni siquiera existe (error explícito).
- Catálogo de planes como base para futura gestión por el SuperAdmin (límites, facturación).

### Negativas / Costos

- La administración del catálogo de planes (endpoints CRUD del SuperAdmin) queda pendiente;
  por ahora se siembran `free` y `pro` y se editan en BD.
- `Tenant.Plan` sigue siendo un string (el código) con FK, no una navegación; se prioriza
  minimizar el churn sobre el modelo existente.

## Alternativas consideradas

- **Solo comparar insensible a mayúsculas** (sin catálogo): arregla el desajuste pero deja
  los planes como texto libre sin integridad; descartado por ser un medio-arreglo.
- **Enum de planes en código**: rígido; obliga a redeploy para cambiar la oferta de planes.
