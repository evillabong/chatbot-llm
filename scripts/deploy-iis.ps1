#requires -Version 5.1
<#
.SYNOPSIS
    Publica y despliega las APIs de MIMO a IIS local (entorno de desarrollo).

.DESCRIPTION
    Para cada API:
      1. dotnet publish (Release) a una carpeta de staging en %TEMP%.
      2. Fija el App Pool del sitio en "Sin código administrado" (managedRuntimeVersion vacío).
      3. Detiene el sitio y su App Pool.
      4. Copia los archivos a la ruta física del sitio, PRESERVANDO appsettings.Production.json
         (que contiene la cadena de conexión y Jwt:Key y NO está en el repositorio).
      5. Reinicia App Pool y sitio.

    El script se auto-eleva (UAC) si no se ejecuta como administrador, ya que escribir en
    C:\inetpub y configurar IIS requieren privilegios elevados.

.PARAMETER Configuration
    Configuración de compilación. Por defecto: Release.

.EXAMPLE
    .\scripts\deploy-iis.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# ── Auto-elevación ────────────────────────────────────────────────────────────
$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host 'Se requieren privilegios de administrador; elevando (UAC)...' -ForegroundColor Yellow
    Start-Process powershell -Verb RunAs -Wait -ArgumentList `
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Configuration', $Configuration
    return
}

# ── Configuración ─────────────────────────────────────────────────────────────
$repoRoot = Split-Path -Parent $PSScriptRoot
$appcmd   = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$stage    = Join-Path $env:TEMP 'mimo_publish'

# Mapeo proyecto -> sitio de IIS. Los frontends/worker aún no existen (placeholders).
$apps = @(
    @{ Project = 'src\Mimo.Api';       Site = 'mimo.api' },
    @{ Project = 'src\Mimo.Admin.Api'; Site = 'mimo.admin.api' }
)

foreach ($a in $apps) {
    $proj = Join-Path $repoRoot $a.Project
    $out  = Join-Path $stage $a.Site
    Write-Host "=== $($a.Site) ===" -ForegroundColor Cyan

    Write-Host "Publicando ($Configuration)..."
    dotnet publish $proj -c $Configuration -o $out | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló para $($a.Project)" }

    $pool = (& $appcmd list app  "$($a.Site)/" /text:applicationPool).Trim()
    $path = (& $appcmd list vdir "$($a.Site)/" /text:physicalPath).Trim()
    if (-not $path) { $path = "C:\inetpub\$($a.Site)" }
    Write-Host "App Pool=[$pool]  Ruta=[$path]"

    if ($pool) { & $appcmd set apppool "$pool" /managedRuntimeVersion:"" | Out-Null }  # No Managed Code
    & $appcmd stop site "$($a.Site)" 2>&1 | Out-Null
    if ($pool) { & $appcmd stop apppool "$pool" 2>&1 | Out-Null }
    Start-Sleep -Seconds 2

    # Copiar SIN sobrescribir la config local de Producción (secretos) ni la de Development.
    robocopy $out $path /E /R:2 /W:1 /NFL /NDL /NJH /NJS /XF appsettings.Production.json appsettings.Development.json | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló (exit $LASTEXITCODE) para $($a.Site)" }

    if ($pool) { & $appcmd start apppool "$pool" 2>&1 | Out-Null }
    & $appcmd start site "$($a.Site)" 2>&1 | Out-Null
    Write-Host "Desplegado $($a.Site)." -ForegroundColor Green
}

Write-Host ''
Write-Host 'Despliegue completo. Verifica /health en el binding de cada sitio (p. ej. http://localhost:4431/health).' -ForegroundColor Green
