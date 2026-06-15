# scripts

Utilidades de desarrollo/despliegue.

## `deploy-iis.ps1`

Publica y despliega `Mimo.Api` y `Mimo.Admin.Api` a IIS local (desarrollo). Se auto-eleva
(UAC) porque escribir en `C:\inetpub` y configurar IIS requieren administrador.

```powershell
# desde la raíz del repo
.\scripts\deploy-iis.ps1
```

Qué hace por cada API: `dotnet publish` (Release) → staging en `%TEMP%` → fija el App Pool en
"Sin código administrado" → detiene sitio/pool → copia a la ruta física del sitio
(preservando `appsettings.Production.json`) → reinicia.

Bindings actuales (desarrollo): `mimo.api` → http://localhost:4431, `mimo.admin.api` → http://localhost:4432.

### Configuración de Producción (una sola vez, fuera del repo)

El despliegue **no** incluye secretos. La cadena de conexión y `Jwt:Key` se definen en un
`appsettings.Production.json` que vive **solo** en la carpeta de cada sitio en IIS y que el
script preserva (no lo sobrescribe ni lo borra):

1. Copia [`appsettings.Production.template.json`](appsettings.Production.template.json) a:
   - `C:\inetpub\mimo.api\appsettings.Production.json`
   - `C:\inetpub\mimo.admin.api\appsettings.Production.json`
2. Reemplaza los valores `CAMBIAR` por la contraseña real de PostgreSQL y una `Jwt:Key`
   de al menos 32 caracteres.

> Bajo IIS el entorno es `Production` por defecto, así que se aplica `appsettings.Production.json`.
> En desarrollo con `dotnet run` (entorno `Development`) la configuración se toma de user-secrets.

## Prerrequisitos (una sola vez)

- PostgreSQL con la extensión **pgvector** instalada.
- IIS habilitado + **.NET Hosting Bundle** (ASP.NET Core Module).
- Sitios `mimo.api` y `mimo.admin.api` creados en IIS apuntando a sus carpetas en `C:\inetpub`.
