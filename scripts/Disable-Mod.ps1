param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Wanderburg Game')
$ErrorActionPreference = 'Stop'
if (Get-Process Wanderburg -ErrorAction SilentlyContinue) { throw 'Bitte Wanderburg zuerst schließen.' }
$pluginPath = Join-Path $GameDir 'BepInEx\plugins\WanderburgDamageStats.dll'
$disabledPath = "$pluginPath.disabled"
if (!(Test-Path -LiteralPath $pluginPath)) { Write-Output 'Mod-DLL ist bereits deaktiviert oder nicht vorhanden.'; exit }
if (Test-Path -LiteralPath $disabledPath) { throw 'Eine deaktivierte Kopie ist bereits vorhanden; keine Datei wurde überschrieben.' }
Move-Item -LiteralPath $pluginPath -Destination $disabledPath
Write-Output 'Wanderburg Damage Stats deaktiviert.'
