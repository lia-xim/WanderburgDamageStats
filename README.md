# Wanderburg Damage Stats

[Deutsch](README.de.md) | **English**

Wanderburg Damage Stats watches which weapons are actually dealing damage during your run. When an upgrade choice appears, it compares all three cards and shows which one is most likely to improve your current build.

This is an early alpha for Wanderburg EA 0.9.8 on Windows. It is an unofficial community project and is not affiliated with or endorsed by Randwerk.

## Quick installation

If you already have BepInEx installed for Wanderburg, you only need to copy one file:

1. Download the newest `WanderburgDamageStats` ZIP from the [GitHub Releases page](https://github.com/lia-xim/WanderburgDamageStats/releases/latest).
2. Extract the ZIP.
3. Copy this file:

   ```text
   BepInEx\plugins\WanderburgDamageStats.dll
   ```

4. Paste it into:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Wanderburg Game\BepInEx\plugins\
   ```

5. Start Wanderburg normally through Steam.

That’s it. **DAMAGE STATS** should appear in the upper-left corner during a run. Press **F8** at any time to hide or show it.

Installed Wanderburg in another Steam library? Open your Steam **Library**, right-click Wanderburg, and select **Manage → Browse local files**. The folder that opens is the correct game folder. From there, open `BepInEx\plugins` and paste the DLL inside.

## If you do not have BepInEx yet

The mod requires BepInEx 6 for **Unity IL2CPP, Windows x64**. Make sure you do not download the Mono build by mistake.

1. Install BepInEx using the [official guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html). We tested build `6.0.0-be.788+5b766a3` from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
2. Start Wanderburg once through Steam, then close it. BepInEx creates the folders it needs during this first launch.
3. Copy `WanderburgDamageStats.dll` into `BepInEx\plugins` as described above.

A shorter copy guide is also included in the release ZIP as `INSTALLATION.txt`.

## What you will see in the game

- During a run: actual damage per second over the last 20 seconds and each weapon’s share of that damage.
- On the upgrade screen: an evaluation of the three cards on offer.
- For normal numerical upgrades: a recommendation for the strongest option the coach can calculate.
- For special or legendary effects: **NUMBERS PICK** instead of pretending the recommendation is certain. Effects that are not fully modeled are marked **situational**.

## Disabling or removing the mod

Press **F8** if you only want to hide the overlay.

To disable the mod permanently, close the game and run `Disable-Mod.ps1` from the release package. Run `Enable-Mod.ps1` if you want to turn it back on later.

To remove it completely, close the game and delete only this file:

```text
BepInEx\plugins\WanderburgDamageStats.dll
```

## If the mod does not appear

- Check that the DLL is directly inside `BepInEx\plugins`.
- Start the game through Steam. In our test, launching `Wanderburg.exe` directly did not initialize Steamworks correctly, which can prevent Steam statistics and leaderboards from working for that run.
- Open `BepInEx\LogOutput.log` in the game folder and search for `Wanderburg Damage Stats`.
- Settings are stored in `BepInEx\config\io.github.lia-xim.wanderburg-damage-stats.cfg`. You can change the overlay scale and position there.

## How the recommendation works

Damage Stats polls Wanderburg’s existing `StatisticsQuery.CurrentRun` counters. It does not patch gameplay or statistics methods, and it does not change combat values or save data.

For normal upgrades, it calculates the relative numerical change. Damage and cooldown changes count directly. Charges, duration, size, and speed use conservative weights. The coach then combines the card’s effect with the affected weapon’s actual share of your damage. A result such as `≈ +12% build output` is an informed estimate for the current run, not a guarantee for every situation.

## Building from source

The complete source is in `src/`. You need a .NET 6-compatible SDK and a local Wanderburg installation with BepInEx interop files already generated.

```powershell
dotnet build ./src/WanderburgDamageStats.csproj -c Release
dotnet run --project ./tests/PreviewTests.csproj -c Release
```

`build.ps1` runs the build and tests, then creates an installable ZIP inside `artifacts/`. Pass a different game directory like this:

```powershell
./build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Wanderburg Game"
```

Game updates may change internal functions or text. This repository contains no game files or extracted assets. Our source code is available under the [MIT License](LICENSE).
