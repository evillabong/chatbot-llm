# scripts

Utilidades de desarrollo/despliegue.

## Despliegue a IIS local

El despliegue está separado por **capa de arquitectura**. Cada script publica un API
(ASP.NET Core, hospedado por ANCM) y su app (Blazor WebAssembly, contenido estático). Ambos
se auto-elevan (UAC) porque escribir en `C:\inetpub` y configurar IIS requieren administrador.
La lógica común vive en [`_deploy-lib.ps1`](_deploy-lib.ps1) (no se ejecuta directamente).

### `deploy-tenant.ps1` — capa tenant

`Mimo.Api` → sitio `mimo.api` · `Mimo.App` → sitio `mimo.app`.

```powershell
# desde la raíz del repo
.\scripts\deploy-tenant.ps1
# fijando la URL del API en el front publicado:
.\scripts\deploy-tenant.ps1 -ApiBaseUrl https://localhost:4431
```

### `deploy-admin.ps1` — capa admin

`Mimo.Admin.Api` → sitio `mimo.admin.api` · `Mimo.Admin.App` → sitio `mimo.admin.app`.

```powershell
.\scripts\deploy-admin.ps1 -ApiBaseUrl https://localhost:4432
```

> `Mimo.Admin.App` aún no existe; el script lo **omite con aviso** hasta que se cree.

Qué hace cada uno:
- **API:** `dotnet publish` (Release) → staging en `%TEMP%` → App Pool "Sin código administrado"
  → detiene sitio/pool → copia a la ruta física **preservando** `appsettings.Production.json` y
  `appsettings.Development.json` del servidor → reinicia.
- **App WASM:** `dotnet publish` → copia espejo (`/MIR`) de `publish\wwwroot` (incluye el
  `web.config` con rewrite SPA y los MIME de `.wasm`/`.dat`). Con `-ApiBaseUrl` reescribe
  `wwwroot/appsettings.json` (`Api:BaseUrl`) antes de copiar.

## `generate-apiclient.ps1`

Regenera el cliente Kiota [`Mimo.Api.Sdk`](../src/Mimo.Api.Sdk) desde el OpenAPI vivo de
`Mimo.Api`: levanta la API en Development, descarga `openapi/v1.json`, la detiene y ejecuta
`kiota generate`. Requiere la herramienta global `kiota`.

```powershell
.\scripts\generate-apiclient.ps1
```

## `generate-admin-apiclient.ps1`

Igual que el anterior pero para la **admin API**: regenera [`Mimo.Admin.Api.Sdk`](../src/Mimo.Admin.Api.Sdk)
(namespace `Mimo.Admin.Api.Sdk`, clase `MimoAdminApiClient`) desde el OpenAPI de `Mimo.Admin.Api`.

```powershell
.\scripts\generate-admin-apiclient.ps1
```

## Configuración de Producción (una sola vez, fuera del repo)

El despliegue **no** incluye secretos. La cadena de conexión y `Jwt:Key` se definen en un
`appsettings.Production.json` que vive **solo** en la carpeta de cada sitio de API en IIS y que el
script preserva (no lo sobrescribe ni lo borra):

1. Copia [`appsettings.Production.template.json`](appsettings.Production.template.json) a:
   - `C:\inetpub\mimo.api\appsettings.Production.json`
   - `C:\inetpub\mimo.admin.api\appsettings.Production.json`
2. Reemplaza los valores `CAMBIAR` por la contraseña real de PostgreSQL y una `Jwt:Key`
   de al menos 32 caracteres (la **misma** en ambas APIs).

> Bajo IIS el entorno es `Production` por defecto, así que se aplica `appsettings.Production.json`.
> En desarrollo con `dotnet run` (entorno `Development`) la configuración se toma de user-secrets.

## Prerrequisitos (una sola vez)

- PostgreSQL con la extensión **pgvector** instalada.
- IIS habilitado + **.NET Hosting Bundle** (ASP.NET Core Module) y, para las apps WASM, el módulo
  **URL Rewrite** de IIS.
- Sitios creados en IIS apuntando a sus carpetas en `C:\inetpub`: `mimo.api`, `mimo.app`,
  `mimo.admin.api`, `mimo.admin.app`.
