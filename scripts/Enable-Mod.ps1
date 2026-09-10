param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Wanderburg Game')
$ErrorActionPreference = 'Stop'
if (Get-Process Wanderburg -ErrorAction SilentlyContinue) { throw 'Bitte Wanderburg zuerst schließen.' }
$pluginPath = Join-Path $GameDir 'BepInEx\plugins\WanderburgBuildCoach.dll'
$disabledPath = "$pluginPath.disabled"
if (Test-Path -LiteralPath $pluginPath) { Write-Output 'Mod-DLL ist bereits aktiviert.'; exit }
if (!(Test-Path -LiteralPath $disabledPath)) { throw 'Keine deaktivierte Mod-DLL gefunden.' }
Move-Item -LiteralPath $disabledPath -Destination $pluginPath
Write-Output 'Wanderburg Build Coach aktiviert.'
