# Corre «Una Ronda Más → Escena → Crear todo lo de Otoniel (Game.unity)» sin abrir Unity:
# obstáculos, efectos, 12 tramos, concursante y Tito (modelos de Kenney), GameSystems, Game.unity y Build Settings.
# Al final guarda una captura en Logs/Previews/game_preview.png.
# Unity tiene que estar CERRADO. Uso, desde la raíz del proyecto:
#   powershell -ExecutionPolicy Bypass -File tools/crear-escena-game.ps1
# Para además probarla en Play (choque, caída al agua, vidas):
#   powershell -ExecutionPolicy Bypass -File tools/crear-escena-game.ps1 -Probar
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [switch]$Probar
)

$ErrorActionPreference = "Stop"
$project = Split-Path -Parent $PSScriptRoot
$lockFile = Join-Path $project "Temp\UnityLockfile"

if (-not (Test-Path $UnityPath)) {
    Write-Host "No encuentro Unity 2022.3.62f2 en: $UnityPath" -ForegroundColor Red
    exit 1
}
if (Test-Path $lockFile) {
    try { [System.IO.File]::Open($lockFile, 'Open', 'ReadWrite', 'None').Close() }
    catch {
        Write-Host "Unity está abierto con este proyecto: usa el menú Una Ronda Más > Escena > Crear todo lo de Otoniel." -ForegroundColor Red
        exit 3
    }
}
New-Item -ItemType Directory -Force (Join-Path $project "Logs") | Out-Null

function Invoke-Unity([string]$method, [string]$logName, [bool]$quit) {
    $log = Join-Path $project "Logs\$logName"
    $arguments = @("-batchmode", "-projectPath", "`"$project`"", "-executeMethod", $method, "-logFile", "`"$log`"")
    if ($quit) { $arguments = @("-quit") + $arguments }
    Write-Host "Unity: $method ... log: $log"
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    $errors = Select-String -Path $log -Pattern "error CS\d+" | Select-Object -First 10
    if ($errors) {
        Write-Host "Errores de compilación:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_.Line }
        exit 1
    }
    Select-String -Path $log -Pattern "^(Validar tramos|Listo|Captura|SMOKE|OK  |REVISAR|FALLÓ|OK     )" | ForEach-Object { Write-Host $_.Line }
    return $process.ExitCode
}

$code = Invoke-Unity "GameSceneCreator.CreateAllAndCapture" "crear-escena-game.log" $true
if ($code -ne 0) { Write-Host "Unity terminó con código $code." -ForegroundColor Red; exit $code }

if ($Probar) {
    # Sin -quit: la prueba entra en Play y cierra Unity sola al terminar.
    $code = Invoke-Unity "GameSmokeTest.Run" "probar-game.log" $false
    if ($code -ne 0) { Write-Host "La prueba en Play falló (código $code). Revisa Logs\probar-game.log" -ForegroundColor Red; exit $code }
}
Write-Host "Listo. Abre Unity y dale Play en Assets/_Project/Scenes/Game.unity (o desde MainMenu)." -ForegroundColor Green
