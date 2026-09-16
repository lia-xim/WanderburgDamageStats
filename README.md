# Wanderburg Damage Stats

[Deutsch](README.de.md) | **English**

Wanderburg Damage Stats watches which weapons are actually dealing damage during your run. When an upgrade choice appears, it compares all three cards and shows which one is most likely to improve your current build.

This is an early alpha for Wanderburg EA 0.9.8 on Windows. It is an unofficial community project and is not affiliated with or endorsed by Randwerk.

## Quick installation

If you already have BepInEx installed for Wanderburg, you only need to copy one file:

1. Download the newest `WanderburgDamageStats` ZIP from the [GitHub Releases page](https://github.com/lia-xim/WanderburgDamageStats/releases).
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

That’s it. **DAMAGE STATS** should appear in the upper-left corner during a run. **F7** toggles the upgrade coach; **F8** hides or shows the whole HUD. You can also click the coach button below the damage bars.

Installed Wanderburg in another Steam library? Open your Steam **Library**, right-click Wanderburg, and select **Manage → Browse local files**. The folder that opens is the correct game folder. From there, open `BepInEx\plugins` and paste the DLL inside.

## If you do not have BepInEx yet

The mod requires BepInEx 6 for **Unity IL2CPP, Windows x64**. Make sure you do not download the Mono build by mistake.

1. Install BepInEx using the [official guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html). We tested build `6.0.0-be.788+5b766a3` from [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
2. Start Wanderburg once through Steam, then close it. BepInEx creates the folders it needs during this first launch.
3. Copy `WanderburgDamageStats.dll` into `BepInEx\plugins` as described above.

A shorter copy guide is also included in the release ZIP as `INSTALLATION.txt`.

## What you will see in the game

- A compact, translucent damage meter during the run, hidden during starting loadout selection.
- Total DPS and thin damage bars for each weapon stay at the center of the combat HUD.
- On upgrade screens, the meter gives way to short damage estimates above or below the cards. Unsupported cards say **Not estimated**; a partial comparison is labeled **PARTIAL LEAD**.
- **F7** toggles the optional coach; **F8** toggles the entire overlay.
- **F9** opens the detailed upgrade comparison. During combat it shows measured skill/auto DPS where the game supplies separate counters; otherwise it says **Channel split unavailable**. A separate damage-over-time counter is not implemented yet.
- The detailed planner compares possible paths through the next five normal upgrades. Immediate and future gains are relative to your current build, not guaranteed results.

## Planning ahead

The planner tries each supported card on a copy of your build. It samples future offers from your current module upgrade pool, follows unlocked upgrade chains, and compares damage throughout each path. It chooses the highest average modeled path score; the percentage underneath shows how often that card led the simulated comparisons. It is **not your chance of winning the run**.

The default horizon is **five future upgrade decisions**, with up to 64 simulated paths per current card. In the mod's configuration file, `[Planning] FutureUpgrades` accepts 1–8. Longer searches add uncertainty as well as work. Search work is spread across frames; the result updates as balanced batches finish.

These are possible offers, not a preview of the game's actual next cards. The planner uses its own random generator and never rerolls your cards or changes the game's RNG. Future choices use sampled lookahead without seeing the later outcome stream.

Normal module upgrades are modeled. Future new modules, artifacts, special/legendary effects, survival and changes in playstyle are outside this search. Unsupported draws are marked as excluded; they are not replaced with more favorable cards. Treat the planned pick as a damage estimate, especially when the current offer contains an unestimated card.

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

Damage Stats reads existing damage counters and observes module activation events. It does not change combat values or save data.

Version 0.7.0 compares both attack channels using the actual card asset, internal rarity and selected core/additional attributes. Ram uses its native collision formula, sampled movement speeds and observed Fury state. Other adapters estimate projectile volleys, timed damage ticks and summoned units. Side Barracks' passive army uses damage × standing population / attack interval; its cooldown replenishes missing units rather than multiplying the army's DPS. Cooldown calculations for repeated attacks preserve measured idle time; readable projectile procs use chance × damage × projectile count.

The game often reports only total damage per weapon. When both attacks are supported, the mod estimates their shares from attack output and observed triggers; a weapon with one modeled damage attack uses its module total. This inferred split is used only for predictions, is labeled in model details and may differ from actual hit rates. It is not presented as measured skill/auto DPS.

The overlay distinguishes a damage pick from a **tentative** damage pick. Sensitivity scenarios show how timing and damage-share assumptions change the result; they are not guaranteed bounds. Missing card data, unsupported attack models and newly unlocked damage sources remain unestimated instead of receiving a fake zero score.

Area coverage, burn refresh, crowd control, impact speed and some prefab overrides still need individual models. Planning compounds the supported damage model; it cannot resolve those missing factors. There is no claim of a guaranteed best overall pick. See [model coverage](MODEL_COVERAGE.md) for the assumptions and remaining limits.

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
