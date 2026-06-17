# 0015. Autenticación por API key y superficie de integración

- Estado: Aceptado
- Fecha: 2026-06-17
- Decisores: evill

## Contexto

El módulo de Interoperabilidad expone una superficie para que los sistemas del cliente (ERP, CRM,
e-commerce, portales) se integren con la plataforma. El corte 1 ([pendings #29](../pendings/README.md))
ya permite **generar y administrar API keys** (alta/baja por el `TenantAdmin`, hash SHA-256, prefijo
visible). Falta el mecanismo que permite **usar** esas claves: un sistema externo no tiene un usuario
ni un JWT, y no debería conocer el slug del tenant ni enviarlo por `X-Tenant-Slug`.

La autenticación de usuarios usa **JWT** y el tenant se liga al token
([ADR 0008](0008-binding-de-tenant-al-principal-autenticado.md)); el `TenantResolutionMiddleware`
resuelve el tenant a partir del claim `tenant_slug` (o de `X-Tenant-Slug` en flujos anónimos) y fija
el `search_path` del esquema ([ADR 0009](0009-search-path-por-interceptor-de-conexion.md)).
La pregunta de diseño: ¿cómo autenticar por API key reutilizando esa maquinaria sin abrir un camino
de acceso cross-tenant ni mezclar credenciales de máquina con las de usuario?

## Decisión

- **Esquema de autenticación propio `ApiKey`** (`ApiKeyAuthenticationHandler`), independiente del
  JWT y **no** marcado como esquema por defecto. La clave se presenta en la cabecera **`X-Api-Key`**.
  El handler hashea la clave (SHA-256, mismo criterio que el alta) y la busca en `public.api_keys`;
  si está activa y su tenant también, emite un `ClaimsPrincipal` con el claim **`tenant_slug`** (más
  `tenant_id`, `api_key_id`, `api_key_name` y `auth_method=api_key`).
- **El tenant se deriva exclusivamente de la clave.** Como el principal lleva `tenant_slug`, el
  `TenantResolutionMiddleware` existente fija el esquema igual que con el JWT —sin tocar el
  middleware—. La API key **no** viaja por `X-Tenant-Slug`.
- **Política de autorización `Integration`** que acepta únicamente el esquema `ApiKey` y exige el
  claim `auth_method=api_key`. Los endpoints de la API pública la declaran; los de gestión de claves
  siguen bajo JWT/`TenantAdmin`. Así un JWT no sirve para la API de integración y una API key no
  sirve para la consola ni para administrar claves.
- **Superficie pública versionada en la ruta**: `/integration/v1/*` (`IntegrationApiEndpoints`).
  Corte 2 incluye `GET /me` (verificación de credenciales), `POST /conversations` (iniciar/retomar) y
  `GET /conversations/detail`. El versionado permite evolucionar el contrato sin romper integraciones.
- **`LastUsedAt` best-effort y _throttled_** (se reescribe a lo sumo cada minuto) para registrar uso
  sin un `UPDATE` por petición.

## Consecuencias

### Positivas

- Reutiliza la resolución de tenant y el aislamiento por esquema ya existentes: no hay un segundo
  camino que fijar el `search_path`. **Verificado E2E:** una conversación creada vía API key persiste
  en `tenant_<slug>.conversations` y el tenant se deriva solo de la clave.
- Separación estricta de credenciales: JWT → consola/gestión; API key → integración. **Verificado:**
  ausencia de clave, clave inválida, clave revocada y **JWT** contra `/integration/v1/*` devuelven 401.
- Revocar la clave corta el acceso de inmediato (se valida `IsActive` en cada petición).

### Negativas / Costos

- Un hash + lookup a `public.api_keys` por petición de integración. Es un índice único sobre
  `key_hash`; si el volumen lo exige, se podrá cachear el resultado (TTL corto, como el tenant).
- La API pública comparte por ahora el **mismo documento OpenAPI** que el resto; un documento OpenAPI
  dedicado a la superficie de integración queda como refinamiento.
- **Scopes/permisos por clave** aún no se aplican: toda clave válida puede usar toda la API pública.
  Pendiente del módulo (granular por endpoint/acción).

## Pendiente relacionado

- Webhooks salientes (corte 3): suscripción a eventos, firma HMAC, reintentos y bitácora.
- Scopes por clave y rotación; documento OpenAPI dedicado. Ver [docs/pendings #29](../pendings/README.md)
  y [docs/anexo-tecnico-integraciones.md](../anexo-tecnico-integraciones.md) §6.bis.
