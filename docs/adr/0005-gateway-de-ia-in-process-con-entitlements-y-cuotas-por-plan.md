# 0005. Gateway de IA in-process con entitlements y cuotas por plan

- Estado: Aceptado
- Fecha: 2026-06-14
- Decisores: evill

## Contexto

MIMO es multi-tenant: todos los tenants comparten `Mimo.Api` (esquema por tenant) y el
bot consulta el LLM por cada conversación. Necesitamos que:

1. Las credenciales del proveedor nunca salgan del límite de la plataforma.
2. Ningún tenant sepa qué modelo/proveedor se usa (opacidad).
3. La plataforma mida el uso de IA **por tenant** (estadísticas para el SuperAdmin).
4. El **plan** del tenant determine qué modelos puede usar y una **cuota** de consumo.

El flujo RAG (filtrar documentos por visibilidad → armar prompt → llamar al LLM →
detectar escalada) debe ejecutarse server-side en `Mimo.Api`, porque usa datos del
esquema del tenant y aplica la regla de privacidad ("la IA no decide visibilidad; el
sistema filtra antes"). La llamada al modelo es solo un paso de ese flujo.

Se evaluó dónde ubicar el control centralizado del LLM (el "AI Gateway").

## Decisión

Implementar el gateway como un **servicio de plataforma in-process dentro de `Mimo.Api`**
(`IAiGatewayService`), no como servicio HTTP separado ni dentro de `Mimo.Admin.Api`.

El gateway, por cada operación de IA (chat/embedding) y dado el `tenantId`:

1. Resuelve el **plan** del tenant y su política (`AiPlanPolicy`).
2. Verifica el **entitlement de modelo**: el conector activo debe estar entre los
   `AllowedProviders` del plan (si la política define alguno).
3. Verifica la **cuota** del periodo (solicitudes y/o tokens mensuales).
4. Construye el cliente del conector activo vía `LlmClientFactory` y ejecuta la llamada.
5. **Registra el uso** (`AiUsageRecord`: proveedor, modelo, operación, tokens) por tenant.

Modelo de datos (esquema `public`, gestionado por el SuperAdmin):

- `ai_plan_policies`: `plan_code`, `allowed_providers` (JSONB), `monthly_request_quota`,
  `monthly_token_quota` (`0` = ilimitado), `is_active`.
- `ai_usage_records`: append-only, índice `(tenant_id, created_at)` para agregación.

`ILlmClient` ahora devuelve también el uso de tokens (`LlmChatResult`/`LlmEmbeddingResult`)
para medición precisa; si un proveedor no lo reporta, el uso es cero y la cuota cae sobre
el conteo de solicitudes.

Si un plan no tiene política definida, el comportamiento es permisivo (conector activo,
sin cuota) para no romper tenants existentes; la restricción se activa al definir su
política desde el SuperAdmin.

## Consecuencias

### Positivas

- Sin host nuevo ni salto de red extra; menor complejidad operativa.
- Opacidad del modelo: los usuarios del tenant solo hablan con `Mimo.Api`; la config de IA
  vive en el esquema global y nunca se devuelve.
- Punto único para credenciales, métricas por tenant y límites por plan.
- Cambiar de proveedor sigue siendo un cambio de datos (ADR 0004).

### Negativas / Costos

- El control de IA vive en el proceso tenant-facing; un abuso de un tenant consume CPU/IO
  del proceso compartido (mitigable con las cuotas y, a futuro, rate limiting).
- Cada llamada de IA añade lecturas (plan/política/uso) y una escritura de uso sobre el
  esquema global; mitigado con índices y, si hiciera falta, caché corta de políticas.
- La administración de conectores/políticas y los reportes de uso (endpoints en
  `Mimo.Admin.Api`) se entregan como incremento siguiente; el motor de control ya queda
  operativo.

## Alternativas consideradas

- **Servicio dedicado `Mimo.Ai.Gateway`** (consumido server-to-server por `Mimo.Api`):
  máximo aislamiento y escalado independiente, descartado por el salto de red, la auth
  servicio-a-servicio y el costo operativo, sin beneficio claro al volumen actual.
- **Endpoints de IA dentro de `Mimo.Admin.Api`**: contradice la separación de planos —
  `Mimo.Admin.Api` es el plano de administración SuperAdmin en host separado, de alto
  privilegio y bajo volumen; volverlo tenant-facing y de alto tráfico aumenta la
  superficie de ataque y acopla el escalado.
