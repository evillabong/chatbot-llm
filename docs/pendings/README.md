# Pendientes (backlog)

Registro de requerimientos detectados pero **no realizados aún**. Se va actualizando: cuando
algo no se pueda/deba hacer en el momento, se anota aquí con contexto suficiente para retomarlo.

Prioridad: 🔴 alta · 🟡 media · 🟢 baja.

| # | Pendiente | Prioridad | Contexto |
|---|---|---|---|
| 1 | **Respuestas tipadas en `Mimo.Admin.Api`** | 🟡 | Igual que el pass hecho en `Mimo.Api` (`.Produces<T>()`), para que el futuro cliente de la Admin API quede tipado. Hacer al construir `Mimo.Admin.App`. |
| 1b | **Respuestas tipadas en endpoints restantes de `Mimo.Api`** | 🟡 | Ya tipados: Auth, Agents, Roles, Documents. Faltan: Tickets, Conversations, InternalChat, Survey. Anotar `.Produces<T>()` y regenerar el cliente al abordar la consola de agente (Fase C). |
| 2 | **Vulnerabilidad NU1903** | 🔴 | `System.Security.Cryptography.Xml 9.0.5` (transitivo, vía Data Protection) con CVE de severidad alta. Fijar/actualizar a versión parcheada. |
| 3 | **Script de regeneración del cliente Kiota** | 🟡 | Automatizar: levantar `Mimo.Api` → obtener OpenAPI → `kiota generate`. Hoy es manual. Crear `scripts/generate-apiclient.ps1`. |
| 4 | **Binario de Tailwind en CI/clones** | 🟡 | `tools/tailwindcss.exe` está gitignorado (38 MB). `dotnet publish`/CI/un clon nuevo no tendrán CSS. Añadir fetch (script o target que lo descargue si falta). |
| 5 | **CORS para SignalR** | 🟡 | La consola de agente usará hubs; los websockets cross-origin requieren orígenes explícitos + `AllowCredentials` (el CORS actual `AllowAnyOrigin` no sirve con credenciales). |
| 6 | **Promover `develop` → `main` + push** | 🟡 | `v0.1.0` y `v0.2.0` están etiquetados en `develop`; `main` quedó atrás. Decidir publicación del release y push al remoto. |
| 7 | **Re-desplegar a IIS** | 🟡 | IIS corre código previo al cifrado de API keys y a varios fixes. Correr `scripts/deploy-iis.ps1` para actualizar `mimo.app`/`mimo.api`. |
| 8 | **Tests de integración con PostgreSQL real (Testcontainers)** | 🔴 | Cubrir aislamiento multi-tenant/`search_path`, dequeue `Serializable` de la cola, firmas de webhook y la ruta de compensación del provisioning. Bloqueado hoy por falta de Docker. |
| 9 | **Refresh tokens / revocación JWT** | 🟢 | ADR 0002: hoy no hay refresh; expiración → re-login. |
| 10 | **Gestión de SuperAdmins/agentes con rol elevado tras bootstrap** | 🟢 | Sólo existe el bootstrap del primer SuperAdmin (`/auth/setup`); falta crear más de forma autenticada. |
| 11 | **Validar `AllowedProviders` contra conectores existentes** | 🟢 | El upsert de política de plan acepta proveedores sin conector; hoy es laxo a propósito. |
| 12 | **Frontend fases B–E** | 🟡 | B: admin de tenant completo · C: consola de agente (Callbell, SignalR) · D: `Mimo.Admin.App` · E: WebChat embebible. |
| 13 | **Cifrado de API key bajo IIS** | 🟢 | El anillo de llaves de Data Protection compartido se configuró; falta verificar permisos/funcionamiento real bajo IIS tras el primer re-deploy. |
| 14 | **Variables de entorno en IIS para Development** | 🟡 | Para que las publicaciones locales reciban el CORS permisivo hay que setear `ASPNETCORE_ENVIRONMENT=Development` en el `web.config` de cada sitio. Como en Development NO cargan user-secrets bajo el App Pool, hay que inyectar también `ConnectionStrings__Default` (`Host=localhost;Port=5432;Database=mimo;Username=postgres;Password=100`) y `Jwt__Key` (misma clave en ambas APIs). Pendiente: hacerlo al re-desplegar (#7). |
