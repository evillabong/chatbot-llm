# 0008. Binding del tenant al principal autenticado

- Estado: Aceptado
- Fecha: 2026-06-14
- Decisores: evill

## Contexto

`TenantResolutionMiddleware` resolvía el tenant en este orden: header `X-Tenant-Slug` →
subdominio → claim `tenant_slug`, **sin verificar el slug contra el usuario autenticado**.
Las políticas de autorización solo validan `agent_id`/`role`, no el tenant.

Consecuencia (hueco de aislamiento): un agente autenticado del tenant A podía enviar
`X-Tenant-Slug: B`; el middleware fijaba el `search_path` al esquema de B y el agente operaba
sobre datos de B. El aislamiento real lo da el `search_path` (por slug), así que la fuente del
tenant para el código debe ser exactamente la misma que fija el `search_path`.

Se evaluó leer un claim `tenant_id` directamente en los endpoints, pero (a) los flujos
anónimos (WebChat, webhooks, encuestas) no tienen token y (b) leer el tenant de una fuente
distinta a la que fija el `search_path` arriesga divergencia.

## Decisión

El tenant se resuelve en el middleware (fuente única que también fija el `search_path`) con
**binding al principal**:

- Petición **autenticada** (claim `tenant_slug` presente): el tenant lo dicta el token.
  Si el cliente además pide otro slug por header/subdominio → **403** (intento cross-tenant,
  registrado en log).
- Petición **anónima**: se resuelve por header/subdominio/config como antes.

La decisión pura vive en `Mimo.Core.MultiTenancy.TenantResolver.Resolve(tokenSlug, clientSlug)`
(probada de forma aislada). El middleware solo extrae los slugs del `HttpContext`, aplica la
regla, y mantiene el tenant resuelto en `HttpContext.Items`.

No se añade un claim `tenant_id`: el slug basta y sigue siendo la llave del esquema; el
middleware obtiene el Id y valida `IsActive` en una consulta cacheada.

Los endpoints dejan de hacer `(Guid)context.Items["TenantId"]!` y usan accesores tipados
(`HttpContext.GetTenantId()`/`GetTenantSlug()`), que centralizan la lectura y fallan con un
mensaje claro si el tenant no fue resuelto.

## Consecuencias

### Positivas

- Cierra el acceso cross-tenant por header/subdominio en peticiones autenticadas.
- La fuente del tenant y la del `search_path` son la misma → sin divergencia.
- La regla de seguridad queda cubierta por pruebas unitarias puras.

### Negativas / Costos

- Un cliente autenticado que (mal)configure el header con otro tenant recibirá 403 en vez de
  ser atendido; es el comportamiento deseado, pero hay que documentarlo para integradores.
- El aislamiento de datos sigue dependiendo del `search_path`; esta decisión lo refuerza pero
  no sustituye pruebas de integración con PostgreSQL real (pendientes, requieren Docker).

## Alternativas consideradas

- **Leer un claim `tenant_id` en cada endpoint**: no cubre flujos anónimos y arriesga
  divergencia con el `search_path`. Descartada.
- **Ignorar en silencio el slug del cliente** cuando difiere del token: oculta intentos de
  acceso cruzado; se prefirió 403 explícito.
