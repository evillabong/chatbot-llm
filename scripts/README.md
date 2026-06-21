# scripts

Utilidades de desarrollo y despliegue.

## Despliegue a IIS local

Dos scripts **autocontenidos**, uno por capa. Cada uno publica el API (ASP.NET Core, ANCM) y su
app (Blazor WebAssembly estática), crea/actualiza el sitio y el App Pool ("Sin código administrado"),
respalda lo anterior en `C:\inetpub\history`, preserva el `appsettings.Production.json` del API
(secretos) e inyecta el CORS y la URL pública. Ejecutar en **PowerShell como Administrador**.

Los hostnames públicos (`https://…linkcorp.uk`) los sirve un proxy inverso por delante; IIS escucha
en `http://localhost` en los puertos indicados.

### `publish-mimo.ps1` — capa tenant
`Mimo.Api` → `mimo.api` (`:4431`) · `Mimo.App` → `mimo.app` (`:8081`).

```powershell
# desde la raíz del repo, la primera vez pasa la contraseña de Postgres:
.\scripts\publish-mimo.ps1 -DbPassword 100
# redepliegues posteriores (preserva appsettings.Production.json):
.\scripts\publish-mimo.ps1
# reutilizar binarios ya publicados:
.\scripts\publish-mimo.ps1 -SkipBuild
```
Valores por defecto: `Api:BaseUrl` de la WASM = `https://mimoapi.linkcorp.uk`; CORS del API =
`https://mimo.linkcorp.uk`. Ajustables por parámetro (`-ApiPublicUrl`, `-AllowedAppOrigin`,
`-AdditionalAllowedOrigins`, puertos, rutas, etc.).

### `publish-mimo-admin.ps1` — capa admin
`Mimo.Admin.Api` → `mimo.admin.api` (`:4432`) · `Mimo.Admin.App` → `mimo.admin.app` (`:8082`).

```powershell
.\scripts\publish-mimo-admin.ps1 -DbPassword 100
```
Por defecto: `Api:BaseUrl` = `https://mimoadmapi.linkcorp.uk`; CORS = `https://mimoadm.linkcorp.uk`.

Qué hace cada script:
- **API:** `dotnet publish` (Release) → copia a la ruta del sitio **excluyendo** `appsettings.Production.json`
  → en el primer despliegue crea ese archivo (cadena de conexión con `-DbPassword` + `Jwt:Key`
  generada; lo preserva después) → inyecta `Cors:AllowedOrigins` en el `appsettings.json` publicado.
- **App WASM:** `dotnet publish` → **aplana** `publish\wwwroot` al sitio → escribe un `web.config`
  con los MIME del framework de Blazor (`.wasm`/`.webcil`/`.dll`/`.dat`/`.blat`) y el fallback SPA →
  fija `Api:BaseUrl` en el `appsettings.json` publicado.
- Asegura el anillo de **Data Protection** compartido (`C:\ProgramData\MIMO\dp-keys`, ADR 0010).

> Las APIs corren en **Production** por defecto bajo IIS; **no** fijes `ASPNETCORE_ENVIRONMENT=Development`
> (rompería el CORS restringido y no cargaría `appsettings.Production.json`).

### `deploy-worker.ps1` — Mimo.Worker (Servicio de Windows)
El worker de background (entrega de webhooks, cierre por inactividad; ADR 0017) **no es IIS**: corre
como Servicio de Windows. Se mantiene aparte porque no es API ni front.

```powershell
.\scripts\deploy-worker.ps1
```

## Regeneración de los SDK Kiota (desarrollo)

- `generate-apiclient.ps1` → [`Mimo.Api.Sdk`](../src/Mimo.Api.Sdk) desde el OpenAPI vivo de `Mimo.Api`.
- `generate-admin-apiclient.ps1` → [`Mimo.Admin.Api.Sdk`](../src/Mimo.Admin.Api.Sdk) desde `Mimo.Admin.Api`.

Requieren la herramienta global `kiota`.

## Prerrequisitos (una sola vez)

- PostgreSQL con la extensión **pgvector**; base `mimo` (usuario `postgres`).
- IIS + **.NET Hosting Bundle** (ASP.NET Core Module), **Static Content**, **Default Document** y
  **URL Rewrite** (este último para el fallback SPA de las WASM).
- No hace falta crear los sitios a mano: los scripts los crean si no existen.
