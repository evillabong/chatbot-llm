# Datos semilla de desarrollo

Ambas APIs siembran datos de demo **solo cuando `ASPNETCORE_ENVIRONMENT=Development`**.
La siembra es **idempotente** (por email/nombre/título): se puede arrancar varias veces sin duplicar
ni sobrescribir cuentas existentes. En otros entornos no se ejecuta.

> Estas credenciales son de desarrollo local, no secretos de producción (mismo criterio que la
> contraseña local de Postgres). Las cuentas reales se crean por los flujos normales
> (`/auth/setup`, aprovisionamiento de tenants, CRUD).

## Contraseña común

Todas las cuentas de demo usan la misma contraseña: **`Mimo123$`**.

## Mimo.Admin.Api (esquema `public`)

Sembrado por `SuperAdminSeeder` + `AiPlanPolicySeeder` al arrancar:

| Cuenta | Email | Rol |
|---|---|---|
| SuperAdmin de demo | `dev@mimo.local` | SuperAdmin |

- Políticas de IA por plan: `free` (1.000 req / 500K tokens al mes) y `pro` (50.000 req / 25M tokens).

## Mimo.Api (por cada tenant activo)

Sembrado por `TenantDevDataSeeder` en el esquema de cada tenant. Hoy hay `acme` y `beta`.
Los emails llevan el slug del tenant: `{cuenta}@{slug}.local`.

| Cuenta | Email (ej. acme) | Rol | Puede CRUD admin de tenant |
|---|---|---|---|
| Administrador demo | `tenantadmin@acme.local` | Administrador | ✅ (funcionarios, roles) |
| Supervisor | `supervisor@acme.local` | Supervisor | — |
| Soporte | `soporte@acme.local` | Soporte | — |
| Ventas | `ventas@acme.local` | Ventas | — |

Además: roles `Administrador` (del aprovisionamiento), `Supervisor`, `Soporte`, `Ventas`; categorías
`General`/`Soporte`/`Ventas` y 3 documentos de conocimiento de ejemplo (sin embedding).

> El admin original del aprovisionamiento (`admin@{slug}.local`) tiene una contraseña distinta
> (la que se usó al crear el tenant); para pruebas usa `tenantadmin@{slug}.local`.

## Probar el login en `Mimo.App`

1. `dotnet run --project src/Mimo.Api` (queda en `https://localhost:7209`, Development → CORS abierto).
2. `dotnet run --project src/Mimo.App`.
3. En la pantalla de login: **Organización** `acme`, **Correo** `tenantadmin@acme.local`,
   **Contraseña** `Mimo123$`.
