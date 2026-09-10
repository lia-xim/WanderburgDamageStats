param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Wanderburg Game',
    [string]$Version = '0.3.0'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\WanderburgDamageStats.csproj'
$tests = Join-Path $root 'tests\PreviewTests.csproj'
$gameAssembly = Join-Path $GameDir 'BepInEx\interop\Assembly-CSharp.dll'

if (!(Test-Path -LiteralPath $gameAssembly)) {
    throw "BepInEx-Interop fehlt unter '$GameDir'. Wanderburg nach der BepInEx-Installation einmal starten."
}

dotnet build $project -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'Mod-Build fehlgeschlagen.' }

dotnet run --project $tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests fehlgeschlagen.' }

$stage = Join-Path $root "artifacts\WanderburgDamageStats-$Version"
$pluginDir = Join-Path $stage 'BepInEx\plugins'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'src\bin\Release\net6.0\WanderburgDamageStats.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md'),(Join-Path $root 'README.de.md'),(Join-Path $root 'INSTALLATION.txt'),(Join-Path $root 'LICENSE'),(Join-Path $root 'scripts\Enable-Mod.ps1'),(Join-Path $root 'scripts\Disable-Mod.ps1') -Destination $stage -Force

$zip = Join-Path $root "artifacts\WanderburgDamageStats-$Version.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
Write-Output "Release erstellt: $zip"
