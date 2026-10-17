# Build WebGL desde la terminal y zip listo para itch.io (Fase 0 paso 9 y Fase 4).
# Unity tiene que estar CERRADO. Uso, desde la raíz del proyecto:
#   powershell -ExecutionPolicy Bypass -File tools/build-webgl.ps1
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$project = Split-Path -Parent $PSScriptRoot
$webglModule = Join-Path (Split-Path -Parent $UnityPath) "Data\PlaybackEngines\WebGLSupport"
$lockFile = Join-Path $project "Temp\UnityLockfile"
$log = Join-Path $project "Logs\build-webgl.log"
$buildFolder = Join-Path $project "Builds\WebGL"
$zip = Join-Path $project "Builds\UnaRondaMas_WebGL.zip"

if (-not (Test-Path $UnityPath)) {
    Write-Host "No encuentro Unity 2022.3.62f2 en: $UnityPath" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $webglModule)) {
    Write-Host "Falta el módulo WebGL Build Support. Unity Hub > Installs > 2022.3.62f2 > Add modules > WebGL Build Support." -ForegroundColor Red
    exit 2
}
if (Test-Path $lockFile) {
    try { [System.IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None').Close() }
    catch {
        Write-Host "Unity está abierto con este proyecto. Ciérralo y vuelve a correr el script." -ForegroundColor Red
        exit 3
    }
}

New-Item -ItemType Directory -Force (Join-Path $project "Logs") | Out-Null
Write-Host "Haciendo la build WebGL (tarda varios minutos)... log: $log"
$arguments = @("-batchmode", "-quit", "-projectPath", "`"$project`"", "-buildTarget", "WebGL",
    "-executeMethod", "WebGLBuildTools.BuildFromCommandLine", "-logFile", "`"$log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($process.ExitCode -ne 0) {
    Write-Host "La build falló (código $($process.ExitCode)). Revisa $log" -ForegroundColor Red
    exit $process.ExitCode
}
if (-not (Test-Path (Join-Path $buildFolder "index.html"))) {
    Write-Host "No apareció Builds\WebGL\index.html. Revisa $log" -ForegroundColor Red
    exit 4
}

# Zip con index.html en la raíz y rutas con "/" (Compress-Archive de PowerShell 5 usa "\" y itch no lo entiende).
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
if (Test-Path $zip) { Remove-Item $zip -Force }
$archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    Get-ChildItem $buildFolder -Recurse -File | ForEach-Object {
        $entry = $_.FullName.Substring($buildFolder.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry) | Out-Null
    }
}
finally { $archive.Dispose() }

$sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Listo: $zip ($sizeMb MB). Súbelo a itch.io (ver docs/ENTREGA.md)." -ForegroundColor Green
