# 0022. Refresh tokens y revocación de sesión (funcionarios)

- Estado: Aceptado
- Fecha: 2026-06-20
- Decisores: evill

## Contexto

La autenticación de funcionarios emite un **JWT de acceso** de vida corta
([ADR 0002](0002-autenticacion-jwt-agentes-y-superadmin.md)), que dejó como pendiente el refresh:
al expirar, el usuario tenía que **volver a iniciar sesión**, y no existía forma de **revocar**
sesiones. Es el pendiente #9.

## Decisión

- **Refresh token opaco** además del access token. En el login se emiten ambos; el refresh va en la
  respuesta (`LoginResponse.RefreshToken` + expiración).
- **Solo se guarda el hash** (SHA-256, vía `IApiKeyService`) del refresh token, nunca el valor en
  claro — mismo criterio que las API keys. Tabla `refresh_tokens` en el **esquema del tenant** (el
  funcionario es del tenant), con índice único por hash.
- **`POST /auth/refresh`** (anónimo, tenant por `X-Tenant-Slug` como el login): valida el refresh
  (existe, no expirado, no revocado), lo **rota** (revoca el actual y emite uno nuevo) y devuelve un
  access token nuevo con los roles actuales del funcionario. La **rotación** limita la ventana de uso
  de un token filtrado: reusar el anterior tras rotar → 401.
- **`POST /auth/logout`**: revoca el refresh token (cierre de sesión / revocación explícita).
- Vida del refresh configurable (`Jwt:RefreshTokenExpiryDays`, por defecto 7 días).

## Consecuencias

### Positivas

- Sesiones más largas sin re-login y con **revocación** real. **Verificado E2E:** login emite refresh;
  refresh rota (nuevo access + nuevo refresh, roles preservados); el refresh anterior queda inválido
  (401); logout revoca (401 posterior).
- Reutiliza el hashing existente y el aislamiento por esquema; el access token JWT no cambia.

### Negativas / Costos

- Alcance de este corte: **solo `Mimo.Api` (funcionarios)**. Falta el equivalente para SuperAdmin en
  `Mimo.Admin.Api` y el **cableado en el front** (guardar el refresh y renovar automáticamente al
  expirar el access) — quedan como seguimiento en `docs/pendings` #9.
- Los refresh tokens revocados/expirados se acumulan en la tabla; conviene una **limpieza periódica**
  (worker) más adelante.
