# 0004. Configuración de conectores de IA en base de datos

- Estado: Aceptado
- Fecha: 2026-06-14
- Decisores: evill

## Contexto

La configuración del LLM (DeepSeek) vivía en `appsettings` y se inyectaba al cliente en
tiempo de registro. Eso impedía cambiar de proveedor o de parámetros sin redeploy, y
acoplaba el código a un único proveedor.

## Decisión

Modelar la configuración de IA a nivel de plataforma en la tabla `ai_connectors`
(esquema `public`), con la configuración específica de cada proveedor en una columna
**JSONB** (`LlmConnectorSettings`). Mantener la abstracción `ILlmClient` y resolver la
implementación concreta por `Provider` (`AiProviders`).

- `LlmClientFactory` construye el cliente concreto a partir de un `AiConnector`.
- `DeepSeekClient` recibe `LlmConnectorSettings` en runtime (ya no lee `appsettings`).
- `AiConnectorSeeder` migra la sección `DeepSeek` de `appsettings` a la BD al arrancar.

## Consecuencias

### Positivas

- Cambiar de parámetros/proveedor es un cambio de datos, no de código.
- Añadir un proveedor = implementar `ILlmClient` + constante + caso en la fábrica.

### Negativas / Costos

- La clave de API queda en la BD (JSONB). Debe protegerse el acceso al esquema `public`
  y considerar cifrado a futuro.

## Notas

Extendido por [ADR 0005](0005-gateway-de-ia-in-process-con-entitlements-y-cuotas-por-plan.md),
que introduce el gateway in-process, los entitlements por plan y la medición de uso por
tenant sobre esta base.
