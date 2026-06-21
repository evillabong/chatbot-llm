<#
.SYNOPSIS
Publica MIMO (capa admin) localmente en IIS: Mimo.Admin.Api + Mimo.Admin.App.

.DESCRIPTION
Compila y publica:
- Backend ASP.NET Core (Mimo.Admin.Api) en C:\inetpub\mimo.admin.api con binding http://*:4432.
- Frontend Blazor WebAssembly (Mimo.Admin.App) en C:\inetpub\mimo.admin.app con binding http://*:8082.
- Artefactos intermedios en .\bin\release\iis-publish\mimo.admin.api y ...\mimo.admin.app.

El script crea/actualiza sitios y application pools de IIS (No Managed Code), respalda las
carpetas actuales y preserva appsettings.Production.json del API (secretos). En la WASM aplana
el contenido de wwwroot al sitio, escribe un web.config con los MIME del framework de Blazor
(.wasm/.webcil/.dll/.dat/.blat) + fallback SPA, y fija Api:BaseUrl. En el API inyecta
Cors:AllowedOrigins con el origen de la WASM. La cadena de conexion a PostgreSQL vive en el
appsettings.json del API (versionado); el script solo asegura una clave JWT de firma fuerte en
appsettings.Production.json del servidor (fuera del repo) y la preserva.

Los hostnames publicos (https://mimoadmapi.linkcorp.uk, https://mimoadm.linkcorp.uk) los sirve un
proxy inverso por delante; IIS escucha en http://localhost en los puertos indicados.

Requisitos:
- Ejecutar PowerShell como Administrador (modulo WebAdministration).
- IIS Static Content, Default Document y URL Rewrite (este ultimo para el fallback SPA).
- ASP.NET Core Hosting Bundle compatible con net10.0 y .NET SDK 10.

.EXAMPLE
.\scripts\publish-mimo-admin.ps1

.EXAMPLE
.\scripts\publish-mimo-admin.ps1 -SkipBuild
#>

[CmdletBinding()]
param(
    [string]$ApiSiteName = "mimo.admin.api",
    [string]$AppSiteName = "mimo.admin.app",
    [string]$ApiPath = "C:\inetpub\mimo.admin.api",
    [string]$AppPath = "C:\inetpub\mimo.admin.app",
    [int]$ApiPort = 4432,
    [int]$AppPort = 8082,
    [string]$ApiPublicUrl = "https://mimoadmapi.linkcorp.uk",
    [string]$AllowedAppOrigin = "https://mimoadm.linkcorp.uk",
    [string[]]$AdditionalAllowedOrigins = @(),
    [string]$Configuration = "Release",
    [string]$HistoryRoot = "C:\inetpub\history",
    [string]$PublishRoot = "",
    [switch]$SkipBuild,
    [switch]$NoBackup,
    [switch]$OverwriteApiSettings,
    [switch]$AllowMissingUrlRewrite
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
# appcmd devuelve códigos !=0 esperables (p. ej. detener un sitio inexistente); no convertirlos en
# excepción (PS 7.4+ lo haría con ErrorActionPreference=Stop). Los exit codes críticos se revisan a mano.
$PSNativeCommandUseErrorActionPreference = $false

# IIS se administra con appcmd.exe (no con el proveedor IIS:\, que no existe en PowerShell 7).
$AppCmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

# Proyectos de esta capa (relativos a la raiz del repo).
$ApiProjectRelative = "src\Mimo.Admin.Api\Mimo.Admin.Api.csproj"
$AppProjectRelative = "src\Mimo.Admin.App\Mimo.Admin.App.csproj"

function Write-Step { param([string]$Message) Write-Host ""; Write-Host "==> $Message" -ForegroundColor Cyan }

function Assert-Administrator {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Ejecuta este script en PowerShell como Administrador."
    }
}

function Assert-Command { param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "No se encontro '$Name' en PATH." }
}

function Invoke-Native { param([string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory = "")
    $previous = Get-Location
    if ($WorkingDirectory) { Set-Location $WorkingDirectory }
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) { throw "Fallo comando: $FilePath $($Arguments -join ' '). ExitCode=$LASTEXITCODE" }
    } finally { Set-Location $previous }
}

function Ensure-Directory { param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { New-Item -ItemType Directory -Path $Path -Force | Out-Null }
}

function Backup-Directory { param([string]$Path, [string]$Name, [string]$Stamp)
    if ($NoBackup -or -not (Test-Path -LiteralPath $Path)) { return }
    $backup = Join-Path $HistoryRoot "$Name-$Stamp"
    Write-Host "Respaldando $Path -> $backup"
    Ensure-Directory $HistoryRoot
    robocopy $Path $backup /MIR /R:2 /W:2 /NFL /NDL /NP | Out-Null
    if ($LASTEXITCODE -gt 7) { throw "Fallo robocopy al respaldar $Path. ExitCode=$LASTEXITCODE" }
}

function Copy-PublishedDirectory { param([string]$Source, [string]$Destination, [string[]]$ExcludeFiles = @())
    Ensure-Directory $Destination
    $args = @($Source, $Destination, "/MIR", "/R:2", "/W:2", "/NFL", "/NDL", "/NP")
    if ($ExcludeFiles.Count -gt 0) { $args += "/XF"; $args += $ExcludeFiles }
    robocopy @args | Out-Null
    if ($LASTEXITCODE -gt 7) { throw "Fallo robocopy al copiar $Source -> $Destination. ExitCode=$LASTEXITCODE" }
}

function Test-IisGlobalModule { param([string]$Name)
    $found = (& $AppCmd list module "$Name" /text:name 2>$null)
    return -not [string]::IsNullOrWhiteSpace($found)
}

function Assert-FrontendIisRequirements {
    $missing = @()
    if (-not (Test-IisGlobalModule "StaticFileModule"))     { $missing += "Static Content" }
    if (-not (Test-IisGlobalModule "DefaultDocumentModule")) { $missing += "Default Document" }
    $hasRewrite = Test-IisGlobalModule "RewriteModule"
    if (-not $hasRewrite) { $missing += "URL Rewrite" }
    if ($missing.Count -eq 0) { return }
    $msg = "Faltan componentes IIS para el frontend: $($missing -join ', ')."
    if ($missing.Count -eq 1 -and -not $hasRewrite -and $AllowMissingUrlRewrite) {
        Write-Host "$msg Se continua por -AllowMissingUrlRewrite; las rutas SPA pueden fallar al refrescar." -ForegroundColor Yellow
        return
    }
    throw "$msg Instala/habilita esos componentes (o usa -AllowMissingUrlRewrite solo para una publicacion temporal)."
}

function Write-BlazorWebConfig { param([string]$Path)
    $hasRewrite = Test-IisGlobalModule "RewriteModule"
    $rewrite = if ($hasRewrite) {
@'
    <rewrite>
      <rules>
        <rule name="SPA fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
'@
    } else { "" }

    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <defaultDocument enabled="true">
      <files><clear /><add value="index.html" /></files>
    </defaultDocument>
    <staticContent>
      <remove fileExtension=".blat" /><mimeMap fileExtension=".blat" mimeType="application/octet-stream" />
      <remove fileExtension=".dat" /><mimeMap fileExtension=".dat" mimeType="application/octet-stream" />
      <remove fileExtension=".dll" /><mimeMap fileExtension=".dll" mimeType="application/octet-stream" />
      <remove fileExtension=".webcil" /><mimeMap fileExtension=".webcil" mimeType="application/octet-stream" />
      <remove fileExtension=".wasm" /><mimeMap fileExtension=".wasm" mimeType="application/wasm" />
      <remove fileExtension=".json" /><mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".woff" /><mimeMap fileExtension=".woff" mimeType="application/font-woff" />
      <remove fileExtension=".woff2" /><mimeMap fileExtension=".woff2" mimeType="application/font-woff" />
    </staticContent>
    <httpCompression>
      <dynamicTypes>
        <add mimeType="application/octet-stream" enabled="true" />
        <add mimeType="application/wasm" enabled="true" />
      </dynamicTypes>
    </httpCompression>
$rewrite
  </system.webServer>
</configuration>
"@
    if (-not $hasRewrite) { Write-Host "URL Rewrite ausente: web.config sin fallback SPA (refrescar rutas internas puede dar 404)." -ForegroundColor Yellow }
    [System.IO.File]::WriteAllText((Join-Path $Path "web.config"), $content, [System.Text.UTF8Encoding]::new($false))
}

function Set-JsonProperty { param($Object, [string]$Name, $Value)
    if ($null -eq $Object.$Name) { $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value -Force }
    else { $Object.$Name = $Value }
}

# Inyecta Cors:AllowedOrigins (anidado) en el appsettings.json publicado del API.
function Set-CorsOrigins { param([string]$SettingsPath, [string[]]$Origins)
    if (-not (Test-Path -LiteralPath $SettingsPath)) { return }
    $json = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
    if ($null -eq $json.Cors) { Set-JsonProperty $json 'Cors' ([pscustomobject]@{}) }
    Set-JsonProperty $json.Cors 'AllowedOrigins' (@($Origins))
    $json | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $SettingsPath -Encoding UTF8
    Write-Host "Cors:AllowedOrigins => $($Origins -join ', ')"
}

# Fija Api:BaseUrl (la URL publica del API) en el appsettings.json de la WASM (lo consume el navegador).
function Set-ApiBaseUrl { param([string]$SettingsPath, [string]$Url)
    if (-not $Url -or -not (Test-Path -LiteralPath $SettingsPath)) { return }
    $json = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
    if ($null -eq $json.Api) { Set-JsonProperty $json 'Api' ([pscustomobject]@{}) }
    Set-JsonProperty $json.Api 'BaseUrl' $Url
    $json | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $SettingsPath -Encoding UTF8
    Write-Host "Api:BaseUrl => $Url"
}

# La cadena de conexion vive en appsettings.json (versionado). Aqui solo se asegura una clave JWT de
# FIRMA fuerte en appsettings.Production.json del servidor (fuera del repo); se preserva si ya existe.
function Ensure-JwtKey { param([string]$ApiDir)
    $prod = Join-Path $ApiDir 'appsettings.Production.json'
    if ((Test-Path -LiteralPath $prod) -and -not $OverwriteApiSettings) { Write-Host "Preservando $prod (clave JWT del servidor)."; return }
    $bytes = New-Object byte[] 36
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $key = ([Convert]::ToBase64String($bytes)) -replace '[+/=]', ''
    ([ordered]@{ Jwt = [ordered]@{ Key = $key } }) | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $prod -Encoding UTF8
    Write-Host "Creado $prod (clave JWT de firma generada; fuera del repo)."
}

function Ensure-AppPool { param([string]$Name)
    $exists = (& $AppCmd list apppool "$Name" /text:name 2>$null)
    if ([string]::IsNullOrWhiteSpace($exists)) { & $AppCmd add apppool /name:"$Name" | Out-Null }
    # No Managed Code (ANCM / estatico) + identidad del App Pool.
    & $AppCmd set apppool "$Name" /managedRuntimeVersion:"" /managedPipelineMode:Integrated /startMode:AlwaysRunning /processModel.identityType:ApplicationPoolIdentity | Out-Null
}

function Ensure-HttpSite { param([string]$Name, [string]$PhysicalPath, [int]$Port)
    $exists = (& $AppCmd list site "$Name" /text:name 2>$null)
    if ([string]::IsNullOrWhiteSpace($exists)) {
        & $AppCmd add site /name:"$Name" /physicalPath:"$PhysicalPath" /bindings:"http/*:${Port}:" | Out-Null
    } else {
        & $AppCmd set vdir "$Name/" /physicalPath:"$PhysicalPath" | Out-Null
        $bindings = (& $AppCmd list site "$Name" /text:bindings 2>$null)
        if ($bindings -notlike "*http/*:${Port}:*") {
            & $AppCmd set site "$Name" "/+bindings.[protocol='http',bindingInformation='*:${Port}:']" | Out-Null
        }
    }
    & $AppCmd set app "$Name/" /applicationPool:"$Name" | Out-Null
}

# Anillo de llaves de Data Protection compartido por las APIs (cifra/descifra API keys; ADR 0010).
function Initialize-DataProtectionKeys {
    $dp = Join-Path $env:ProgramData 'MIMO\dp-keys'
    Ensure-Directory $dp
    & icacls $dp /grant 'IIS_IUSRS:(OI)(CI)M' /T | Out-Null
    Write-Host "Anillo Data Protection: $dp (acceso IIS_IUSRS)."
}

# ── Flujo ──────────────────────────────────────────────────────────────────────
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiProject = Join-Path $repoRoot $ApiProjectRelative
$appProject = Join-Path $repoRoot $AppProjectRelative
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
if ([string]::IsNullOrWhiteSpace($PublishRoot)) { $PublishRoot = Join-Path $repoRoot "bin\release\iis-publish" }
$apiPublish = Join-Path $PublishRoot $ApiSiteName
$appPublish = Join-Path $PublishRoot $AppSiteName

Assert-Administrator
Assert-Command "dotnet"
if (-not (Test-Path $AppCmd)) { throw "No se encontro appcmd.exe ($AppCmd). ¿IIS instalado?" }
Assert-FrontendIisRequirements

if (-not $SkipBuild) {
    Write-Step "Publicando API ($ApiSiteName)"
    Ensure-Directory $PublishRoot
    Remove-Item -LiteralPath $apiPublish -Recurse -Force -ErrorAction SilentlyContinue
    Invoke-Native "dotnet" @("publish", $apiProject, "-c", $Configuration, "-o", $apiPublish, "--nologo")

    Write-Step "Publicando WASM ($AppSiteName)"
    Remove-Item -LiteralPath $appPublish -Recurse -Force -ErrorAction SilentlyContinue
    Invoke-Native "dotnet" @("publish", $appProject, "-c", $Configuration, "-o", $appPublish, "--nologo")
} else {
    Write-Host "SkipBuild activo: se reutilizan artefactos en $PublishRoot"
}

# El contenido servible de la WASM esta bajo <publish>\wwwroot; se aplana al sitio + web.config propio.
$appContent = Join-Path $appPublish "wwwroot"
if (-not (Test-Path -LiteralPath $appContent)) { throw "No se encontro $appContent. ¿Falla la publicacion de la WASM?" }

Write-Step "Respaldando despliegue actual"
Backup-Directory -Path $ApiPath -Name $ApiSiteName -Stamp $stamp
Backup-Directory -Path $AppPath -Name $AppSiteName -Stamp $stamp

Write-Step "Deteniendo sitios y application pools"
foreach ($s in $ApiSiteName, $AppSiteName) { & $AppCmd stop site "$s" 2>$null | Out-Null }
foreach ($p in $ApiSiteName, $AppSiteName) { & $AppCmd stop apppool "$p" 2>$null | Out-Null }
Start-Sleep -Seconds 2

Write-Step "Copiando API (preservando appsettings.Production.json)"
$apiExcludes = @("appsettings.Production.json")
Copy-PublishedDirectory -Source $apiPublish -Destination $ApiPath -ExcludeFiles $apiExcludes
Ensure-JwtKey -ApiDir $ApiPath
Set-CorsOrigins -SettingsPath (Join-Path $ApiPath "appsettings.json") -Origins (@($AllowedAppOrigin) + $AdditionalAllowedOrigins | Where-Object { $_ } | Select-Object -Unique)

Write-Step "Copiando WASM (aplanando wwwroot + web.config)"
Copy-PublishedDirectory -Source $appContent -Destination $AppPath
Write-BlazorWebConfig -Path $AppPath
Set-ApiBaseUrl -SettingsPath (Join-Path $AppPath "appsettings.json") -Url $ApiPublicUrl

Write-Step "Configurando IIS"
Initialize-DataProtectionKeys
Ensure-AppPool -Name $ApiSiteName
Ensure-AppPool -Name $AppSiteName
Ensure-HttpSite -Name $ApiSiteName -PhysicalPath $ApiPath -Port $ApiPort
Ensure-HttpSite -Name $AppSiteName -PhysicalPath $AppPath -Port $AppPort

Write-Step "Iniciando sitios"
foreach ($p in $ApiSiteName, $AppSiteName) { & $AppCmd start apppool "$p" 2>$null | Out-Null }
foreach ($s in $ApiSiteName, $AppSiteName) { & $AppCmd start site "$s" 2>$null | Out-Null }

Write-Step "Resumen"
Write-Host "API  : http://localhost:$ApiPort  (publico: $ApiPublicUrl)"
Write-Host "App  : http://localhost:$AppPort  (origen CORS: $AllowedAppOrigin)"
Write-Host "Rutas: $ApiPath  |  $AppPath"
Write-Host "Las APIs corren en Production por defecto bajo IIS (no fijes ASPNETCORE_ENVIRONMENT=Development)." -ForegroundColor Yellow
