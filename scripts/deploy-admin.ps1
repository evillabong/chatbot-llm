#requires -Version 5.1
<#
.SYNOPSIS
    Publica y despliega la capa ADMIN de MIMO a IIS local: Mimo.Admin.Api (sitio mimo.admin.api)
    y Mimo.Admin.App (sitio mimo.admin.app, Blazor WebAssembly estático).

.DESCRIPTION
    - Mimo.Admin.Api: dotnet publish (Release) y copia preservando appsettings.Production.json.
    - Mimo.Admin.App: dotnet publish y copia espejo de wwwroot (incluye web.config con rewrite SPA).
    Si el proyecto Mimo.Admin.App o su sitio IIS aún no existen, se omite con aviso (el script
    queda listo para cuando se cree la app de administración).
    Se auto-eleva (UAC) porque escribir en C:\inetpub y configurar IIS requieren privilegios.

.PARAMETER Configuration
    Configuración de compilación. Por defecto: Release.

.PARAMETER ApiBaseUrl
    Si se indica, fija Api:BaseUrl en el appsettings.json publicado de Mimo.Admin.App (la URL del
    sitio mimo.admin.api en este entorno). Si se omite, se despliega el valor del repo.

.EXAMPLE
    .\scripts\deploy-admin.ps1 -ApiBaseUrl https://localhost:4432
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$ApiBaseUrl
)

$ErrorActionPreference = 'Stop'

# ── Auto-elevación ────────────────────────────────────────────────────────────
. (Join-Path $PSScriptRoot '_deploy-lib.ps1')
if (-not (Test-Administrator)) {
    Write-Host 'Se requieren privilegios de administrador; elevando (UAC)...' -ForegroundColor Yellow
    $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"",'-Configuration',$Configuration)
    if ($ApiBaseUrl) { $argList += @('-ApiBaseUrl', $ApiBaseUrl) }
    Start-Process powershell -Verb RunAs -Wait -ArgumentList $argList
    return
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$stage    = Join-Path $env:TEMP 'mimo_publish'

Initialize-DataProtectionKeys

Publish-AspNetApi -ProjectPath (Join-Path $repoRoot 'src\Mimo.Admin.Api\Mimo.Admin.Api.csproj') `
                  -Site 'mimo.admin.api' -Configuration $Configuration -Stage $stage

Publish-WasmApp   -ProjectPath (Join-Path $repoRoot 'src\Mimo.Admin.App\Mimo.Admin.App.csproj') `
                  -Site 'mimo.admin.app' -Configuration $Configuration -Stage $stage -ApiBaseUrl $ApiBaseUrl

Write-Host ''
Write-Host 'Despliegue ADMIN completo. Verifica /health de mimo.admin.api y la carga de mimo.admin.app.' -ForegroundColor Green
