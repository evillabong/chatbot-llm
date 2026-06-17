# 0013. Configuración self-service del tenant en Mimo.Api

- Estado: Aceptado
- Fecha: 2026-06-17
- Decisores: evill

## Contexto

El `TenantAdmin` necesita configurar su propio tenant (mensajes de atención, asignación de
sesiones, encuestas, horario, personalización de canales). Esa configuración vive como **JSONB**
en `public.tenants.Configuration` (catálogo global), pero hasta ahora no existía endpoint para
leerla/editarla desde la API orientada al tenant (`Mimo.Api`). El `TenantResolutionMiddleware` ya
carga la configuración del tenant resuelto y la expone en `HttpContext`.

## Decisión

- **Nuevo recurso `/tenant/configuration` en `Mimo.Api`** (`GET` y `PUT`), sin parámetros en la
  ruta ([ADR 0006](0006-convencion-de-endpoints-sin-parametros-en-la-ruta.md)). Requiere rol
  Administrador (política `TenantAdmin`).
- **Tenant ligado al token** ([ADR 0008](0008-binding-de-tenant-al-principal-autenticado.md)): el
  endpoint opera sobre el tenant resuelto por el middleware; nunca acepta un id de tenant del
  cliente. Un admin solo lee/edita la configuración de SU tenant.
- **`Mimo.Api` escribe en el esquema `public`** (`tenants.Configuration`), no solo lo lee. Es la
  primera escritura de la API de tenant sobre el catálogo global; se acota a la fila del propio
  tenant y a la columna `Configuration`.
- **Reemplazo de objeto, no mutación:** la columna usa conversión de valor a JSONB, por lo que el
  `PUT` asigna un `TenantConfiguration` nuevo para que EF detecte el cambio. El `PUT` envía el
  objeto **completo** (round-trip): el cliente carga la config, edita un subconjunto y reenvía
  todo, preservando las secciones no editadas.
- **Invalidación de caché:** tras el `PUT` se elimina la entrada `mimo:tenant:{slug}` de
  `IMemoryCache` para que el middleware lea la configuración nueva en el siguiente request.
- **Bypass de OpenAPI en el middleware:** las rutas `/openapi*` dejan de exigir tenant (no son
  tenant-scoped), para poder regenerar el cliente Kiota contra el documento vivo.

## Consecuencias

### Positivas

- El admin de tenant autoconfigura su operación sin pasar por SuperAdmin.
- Contrato tipado (Kiota genera `TenantConfiguration`); regeneración automatizada con
  `scripts/generate-apiclient.ps1`.
- Coherente con el binding por token y la convención de rutas.

### Negativas / Costos

- Acoplamiento de `Mimo.Api` a la tabla global `tenants` para escritura (mitigado: ámbito acotado
  a la propia fila/columna).
- Los `int?` del modelo se serializan como `UntypedNode` en el cliente (limitación OpenAPI 3.0 con
  nullables); se preservan por round-trip pero aún no se editan desde la UI. Igual que la hora y
  los días de atención (`Time`/`int[]`). Ver `docs/pendings`.
- Los secretos de canal (tokens de WhatsApp/Telegram…) viajan en el JSON de configuración en
  claro; pendiente de cifrado análogo al de las API keys de IA ([ADR 0010](0010-cifrado-de-api-keys-de-conectores-de-ia.md)).
