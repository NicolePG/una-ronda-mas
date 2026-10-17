# Corre «Una Ronda Más → Preparar todo (Nicole)» sin abrir Unity:
# TMP Essentials, materiales, Balance, premios, reglas, estrellas, GameSystems, Test_Nicole.unity y docs/BALANCE.md.
# Unity tiene que estar CERRADO. Uso, desde la raíz del proyecto:
#   powershell -ExecutionPolicy Bypass -File tools/preparar-todo.ps1
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$project = Split-Path -Parent $PSScriptRoot
$lockFile = Join-Path $project "Temp\UnityLockfile"
$log = Join-Path $project "Logs\preparar-todo.log"

if (-not (Test-Path $UnityPath)) {
    Write-Host "No encuentro Unity 2022.3.62f2 en: $UnityPath" -ForegroundColor Red
    exit 1
}
if (Test-Path $lockFile) {
    try { [System.IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None').Close() }
    catch {
        Write-Host "Unity está abierto con este proyecto: usa el menú Una Ronda Más > Preparar todo (Nicole)." -ForegroundColor Red
        exit 3
    }
}

New-Item -ItemType Directory -Force (Join-Path $project "Logs") | Out-Null
Write-Host "Preparando el proyecto con Unity en modo batch... log: $log"
$arguments = @("-batchmode", "-quit", "-projectPath", "`"$project`"",
    "-executeMethod", "SetupMenu.PrepareAll", "-logFile", "`"$log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
$errors = Select-String -Path $log -Pattern "error CS\d+" | Select-Object -First 10
if ($errors) {
    Write-Host "Errores de compilación:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host $_.Line }
    exit 1
}
if ($process.ExitCode -ne 0) {
    Write-Host "Unity terminó con código $($process.ExitCode). Revisa $log" -ForegroundColor Red
    exit $process.ExitCode
}

# TMP Essentials: en modo batch se importan con -importPackage (ImportPackage desde código no alcanza a terminar).
if (-not (Test-Path (Join-Path $project "Assets\TextMesh Pro\Resources"))) {
    $tmpPackage = Get-ChildItem (Join-Path $project "Library\PackageCache") -Filter "TMP Essential Resources.unitypackage" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*com.unity.textmeshpro@*" } | Select-Object -First 1
    if ($tmpPackage) {
        Write-Host "Importando TMP Essentials..."
        $tmpLog = Join-Path $project "Logs\importar-tmp.log"
        $tmpArgs = @("-batchmode", "-quit", "-projectPath", "`"$project`"", "-importPackage", "`"$($tmpPackage.FullName)`"", "-logFile", "`"$tmpLog`"")
        $tmpProcess = Start-Process -FilePath $UnityPath -ArgumentList $tmpArgs -Wait -PassThru -NoNewWindow
        if ($tmpProcess.ExitCode -ne 0 -or -not (Test-Path (Join-Path $project "Assets\TextMesh Pro\Resources"))) {
            Write-Host "No se pudieron importar los TMP Essentials. Revisa $tmpLog" -ForegroundColor Red
            exit 1
        }
    }
    else { Write-Host "No encontré el paquete de TMP Essentials en Library\PackageCache." -ForegroundColor Yellow }
}
Write-Host "Listo. Abre Unity y dale Play en Assets/_Project/Scenes/Test_Nicole.unity." -ForegroundColor Green
