# 0002. Autenticación JWT para agentes y super administradores

- Estado: Aceptado
- Fecha: 2026-06-13
- Decisores: evill

## Contexto

Ambas APIs tenían configurado `JwtBearer` pero ningún endpoint emitía tokens, el modelo
`Agent` no almacenaba contraseña y no existía el concepto de super administrador de
plataforma. Sin emisión de tokens, los claims que ya consumían `TicketHub`
(`agent_id`), el middleware de tenant (`tenant_slug`) y el admin API (`role`) nunca
podían poblarse.

## Decisión

Implementar autenticación basada en JWT firmados simétricamente (HMAC-SHA256):

- `Agent.PasswordHash` (PBKDF2 vía `Rfc2898DeriveBytes`, sin dependencias nuevas).
- Nuevo modelo `SuperAdmin` en el esquema `public` (transversal a tenants).
- `IPasswordHasher` e `IJwtTokenService` (Core) con implementación en Infrastructure.
- `POST /auth/login` en `Mimo.Api` (emite `agent_id`, `tenant_slug`, `role`) y en
  `Mimo.Admin.Api` (emite `role=SuperAdmin`).
- `POST /auth/setup` en el admin API para crear el primer super administrador
  (solo funciona si la tabla está vacía).
- La configuración (Issuer/Audience/Key/ExpiryMinutes) se enlaza desde la sección `Jwt`.

## Consecuencias

### Positivas

- Flujo de identidad completo y autónomo, sin proveedor de identidad externo.
- PBKDF2 evita añadir paquetes de hashing de terceros.

### Negativas / Costos

- HMAC simétrico exige custodiar bien la clave `Jwt:Key` (secreto compartido).
- Sin refresh tokens ni revocación: los tokens valen hasta expirar (mejora futura).

## Alternativas consideradas

- **Proveedor de identidad externo (Auth0/Entra)**: descartado por simplicidad y
  control en esta fase; reconsiderable al crecer.
- **Hashing con BCrypt/Argon2 (paquete externo)**: PBKDF2 de la BCL cumple sin
  dependencias adicionales.
