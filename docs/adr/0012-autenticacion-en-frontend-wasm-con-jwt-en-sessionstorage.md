# 0012. Autenticación en el frontend WASM: JWT en sessionStorage e inyección por handler

- Estado: Aceptado
- Fecha: 2026-06-16
- Decisores: evill

## Contexto

`Mimo.App` (Blazor WASM, admin de tenant) necesita iniciar sesión contra `Mimo.Api`. El backend
emite un JWT en `/auth/login` y resuelve el tenant por el header `X-Tenant-Slug` (la ruta de
login es anónima; el cliente aún no tiene el claim `tenant_slug`; ver [ADR 0008](0008-binding-de-tenant-al-principal-autenticado.md)).
El cliente de API está generado con Kiota ([ADR 0011](0011-arquitectura-frontend-blazor-wasm-aislado.md)),
así que la inserción de credenciales no debe quedar dispersa en cada llamada.

## Decisión

- **Persistencia del token en `sessionStorage`** (no `localStorage`): la sesión vive mientras la
  pestaña esté abierta, acotando la ventana de robo del token. No hay refresh token (ver
  [ADR 0002](0002-autenticacion-jwt-agentes-y-superadmin.md)); al expirar se vuelve a login.
- **Estado de sesión en memoria (`SessionState`)** como fuente de verdad sincrónica del token,
  el `tenant_slug` y el `ClaimsPrincipal`. Se hidrata desde `sessionStorage` en el primer
  `GetAuthenticationStateAsync`.
- **Inyección de credenciales en un único `DelegatingHandler` (`AuthHeaderHandler`)** que añade
  `Authorization: Bearer` y `X-Tenant-Slug` a cada request. El cliente Kiota usa
  `AnonymousAuthenticationProvider`; el handler es el único punto de inserción.
- **`MimoAuthStateProvider : AuthenticationStateProvider`** integra la sesión con
  `AuthorizeRouteView`/`AuthorizeView`. Las páginas protegidas usan `[Authorize]`; las no
  autenticadas (login) usan `EmptyLayout` y quedan accesibles.
- **Los claims del JWT se leen en el cliente sin validar la firma** (solo para UI/ruteo). La
  validación criptográfica es responsabilidad exclusiva del backend.
- **Login pre-fija el tenant** (`SetPendingTenant`) para que la llamada anónima a `/auth/login`
  viaje con `X-Tenant-Slug`.

## Consecuencias

### Positivas

- Un solo lugar (handler) añade credenciales: nuevas llamadas Kiota quedan autenticadas sin
  código extra.
- `sessionStorage` reduce la persistencia del token frente a XSS comparado con `localStorage`.
- Integración idiomática con el sistema de autorización de Blazor.

### Negativas / Costos

- Sin refresh token, la expiración obliga a re-login (consistente con ADR 0002).
- Leer claims sin validar firma es aceptable solo porque el backend revalida; debe documentarse
  para no inducir a confiar en ellos del lado cliente.
- `sessionStorage` no comparte sesión entre pestañas (comportamiento deseado aquí).

## Pendiente

- CORS para SignalR (la consola de agente con hubs requiere orígenes explícitos +
  `AllowCredentials`); ver `docs/pendings`.
- `Mimo.Admin.App` reutilizará este patrón apuntando a `Mimo.Admin.Api`.
