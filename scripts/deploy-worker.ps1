#requires -Version 5.1
<#
.SYNOPSIS
    Publica y despliega Mimo.Worker como Servicio de Windows (ADR 0017).

.DESCRIPTION
    Mimo.Worker aloja los procesos de background de BD/HTTP (entrega de webhooks y cierre por
    inactividad), separados de Mimo.Api. No es un sitio IIS: corre como Servicio de Windows.
    - dotnet publish (Release) y copia preservando appsettings.Production.json del servidor.
    - Crea el servicio si no existe; si existe, lo detiene, actualiza binarios y lo reinicia.
    - Comparte el anillo de Data Protection con las APIs (para descifrar los secretos HMAC).
    Se auto-eleva (UAC) porque crear/administrar servicios y escribir en Archivos de programa lo requieren.

.PARAMETER Configuration
    Configuración de compilación. Por defecto: Release.

.PARAMETER ServiceName
    Nombre del Servicio de Windows. Por defecto: "MIMO Worker".

.PARAMETER InstallDir
    Carpeta de instalación de los binarios. Por defecto: C:\Program Files\MIMO\Worker.

.EXAMPLE
    .\scripts\deploy-worker.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$ServiceName   = 'MIMO Worker',
    [string]$InstallDir    = (Join-Path $env:ProgramFiles 'MIMO\Worker')
)

$ErrorActionPreference = 'Stop'

function Test-Administrator {
    $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Anillo de llaves de Data Protection compartido con las APIs (descifra secretos HMAC; ADR 0010).
function Initialize-DataProtectionKeys {
    $dpKeys = Join-Path $env:ProgramData 'MIMO\dp-keys'
    New-Item -ItemType Directory -Force -Path $dpKeys | Out-Null
    icacls $dpKeys /grant 'IIS_IUSRS:(OI)(CI)M' /T | Out-Null
    Write-Host "Anillo de llaves Data Protection: $dpKeys"
}

# ── Auto-elevación ────────────────────────────────────────────────────────────
if (-not (Test-Administrator)) {
    Write-Host 'Se requieren privilegios de administrador; elevando (UAC)...' -ForegroundColor Yellow
    $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"",
                 '-Configuration',$Configuration,'-ServiceName',"`"$ServiceName`"",'-InstallDir',"`"$InstallDir`"")
    Start-Process powershell -Verb RunAs -Wait -ArgumentList $argList
    return
}

$repoRoot   = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\Mimo.Worker\Mimo.Worker.csproj'
$stage      = Join-Path $env:TEMP 'mimo_publish\Mimo.Worker'
$exePath    = Join-Path $InstallDir 'Mimo.Worker.exe'

Write-Host "=== Worker $ServiceName ===" -ForegroundColor Cyan

# El worker descifra secretos cifrados por las APIs: mismo anillo de llaves Data Protection.
# Corre como LocalSystem (acceso a C:\ProgramData\MIMO\dp-keys). Si se cambia a una cuenta de
# menor privilegio, hay que conceder acceso a esa carpeta a dicha cuenta.
Initialize-DataProtectionKeys

Write-Host "Publicando ($Configuration)..."
dotnet publish $projectPath -c $Configuration -o $stage | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló para $projectPath" }

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped') {
    Write-Host "Deteniendo servicio existente..."
    Stop-Service -Name $ServiceName -Force
    $existing.WaitForStatus('Stopped', (New-TimeSpan -Seconds 30))
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
# Copia preservando appsettings.Production.json del servidor (secretos/cadena de conexión, fuera del repo).
robocopy $stage $InstallDir /E /R:2 /W:1 /NFL /NDL /NJH /NJS /XF appsettings.Production.json | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy falló (exit $LASTEXITCODE) para $ServiceName" }

if (-not $existing) {
    Write-Host "Creando servicio '$ServiceName' (LocalSystem, inicio automático)..."
    New-Service -Name $ServiceName -BinaryPathName "`"$exePath`"" -DisplayName $ServiceName `
                -StartupType Automatic -Description 'Procesos de background de MIMO (webhooks, inactividad).' | Out-Null
}

Start-Service -Name $ServiceName
Write-Host "Servicio '$ServiceName' desplegado e iniciado." -ForegroundColor Green
Write-Host 'Recuerda: appsettings.Production.json en el servidor debe tener ConnectionStrings:Default.' -ForegroundColor Yellow
