#requires -Version 5.1
<#
.SYNOPSIS
    Publica y despliega la capa TENANT de MIMO a IIS local: Mimo.Api (sitio mimo.api) y
    Mimo.App (sitio mimo.app, Blazor WebAssembly estático).

.DESCRIPTION
    - Mimo.Api: dotnet publish (Release) y copia preservando appsettings.Production.json del servidor.
    - Mimo.App: dotnet publish y copia espejo de wwwroot (incluye web.config con rewrite SPA).
    Se auto-eleva (UAC) porque escribir en C:\inetpub y configurar IIS requieren privilegios.

.PARAMETER Configuration
    Configuración de compilación. Por defecto: Release.

.PARAMETER ApiBaseUrl
    Si se indica, fija Api:BaseUrl en el appsettings.json publicado de Mimo.App (la URL del
    sitio mimo.api en este entorno). Si se omite, se despliega el valor del repo.

.EXAMPLE
    .\scripts\deploy-tenant.ps1 -ApiBaseUrl https://localhost:4431
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

Publish-AspNetApi -ProjectPath (Join-Path $repoRoot 'src\Mimo.Api\Mimo.Api.csproj') `
                  -Site 'mimo.api' -Configuration $Configuration -Stage $stage

Publish-WasmApp   -ProjectPath (Join-Path $repoRoot 'src\Mimo.App\Mimo.App.csproj') `
                  -Site 'mimo.app' -Configuration $Configuration -Stage $stage -ApiBaseUrl $ApiBaseUrl

Write-Host ''
Write-Host 'Despliegue TENANT completo. Verifica /health de mimo.api y la carga de mimo.app.' -ForegroundColor Green
