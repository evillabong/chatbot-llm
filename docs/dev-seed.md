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

## Negocio de demostración: AndinaShop (tenant `andinashop`)

`BusinessDemoSeeder` aprovisiona un tenant completo y coherente (tienda online de electrónica,
plan `pro`) con los cuatro actores. Se excluye del seeder genérico de tenants para mantenerlo
limpio. Contraseña común: **`Mimo123$`**.

**SuperAdmin (plataforma):** `dev@mimo.local` (en `Mimo.Admin.Api`).

**Admin del tenant:** `admin@andinashop.com` (rol Administrador).

**Funcionarios:**

| Nombre | Email | Rol |
|---|---|---|
| Laura Gómez | `laura.gomez@andinashop.com` | Ventas |
| Carlos Ruiz | `carlos.ruiz@andinashop.com` | Soporte |
| Sofía Martínez | `sofia.martinez@andinashop.com` | Envíos |
| Diego Herrera | `diego.herrera@andinashop.com` | Supervisor (ve todos los tickets) |

**Clientes (conversaciones con mensajes / tickets / encuesta):**

| Cliente | Canal | Caso | Estado | Atiende |
|---|---|---|---|---|
| Pedro Ramírez | WhatsApp | Consulta de envío | Resuelto por el bot (sin ticket) | — |
| Ana Torres | WebChat | Lead de compra (nevera 400L) | Asignado · prioridad alta | Laura (Ventas) |
| María González | WhatsApp | Estado de pedido #10532 | En progreso | Sofía (Envíos) |
| Juan Pérez | WebChat | Devolución de producto | Cerrado + encuesta ★5 | Carlos (Soporte) |

**Conocimiento:** políticas de envíos, devoluciones, métodos de pago, garantía y guía de
compatibilidad. **Configuración:** asignación automática balanceada, encuesta habilitada, horario
de atención y personalización del WebChat (bienvenida + color).

## Probar el login en `Mimo.App`

1. `dotnet run --project src/Mimo.Api` (queda en `https://localhost:7209`, Development → CORS abierto).
2. `dotnet run --project src/Mimo.App`.
3. En la pantalla de login, usa el negocio de demo: **Organización** `andinashop`,
   **Correo** `admin@andinashop.com`, **Contraseña** `Mimo123$`.
   (También sirve el tenant genérico `acme` con `tenantadmin@acme.local`.)
