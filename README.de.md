# Wanderburg Damage Stats

**Deutsch** | [English](README.md)

Wanderburg Damage Stats schaut während deines Runs, welche Waffen tatsächlich den Schaden machen. Sobald du ein Upgrade wählen darfst, vergleicht die Mod die drei Karten und zeigt dir, welche davon deinen aktuellen Build voraussichtlich am stärksten verbessert.

Das Projekt ist eine frühe Alpha für Wanderburg EA 0.9.8 unter Windows. Es ist ein inoffizielles Community-Projekt und nicht mit Randwerk verbunden.

## Installation – ganz kurz

Wenn du BepInEx bereits für Wanderburg installiert hast, brauchst du nur eine Datei zu kopieren:

1. Lade `WanderburgDamageStats-0.2.0.zip` bei den [GitHub-Releases](https://github.com/lia-xim/WanderburgDamageStats/releases) herunter.
2. Entpacke das ZIP.
3. Kopiere diese Datei:

   ```text
   BepInEx\plugins\WanderburgDamageStats.dll
   ```

4. Füge sie hier ein:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Wanderburg Game\BepInEx\plugins\
   ```

5. Starte Wanderburg ganz normal über Steam.

Das war’s. Im Run sollte links oben **DAMAGE STATS** erscheinen. Mit **F8** kannst du die Anzeige jederzeit ein- und ausblenden.

Du hast Wanderburg in einer anderen Steam-Bibliothek installiert? Öffne in Steam deine **Bibliothek**, klicke Wanderburg mit der rechten Maustaste an und wähle **Verwalten → Lokale Dateien durchsuchen**. Der Ordner, der sich öffnet, ist der richtige. Von dort gehst du weiter zu `BepInEx\plugins`.

## Wenn du BepInEx noch nicht hast

Der Mod braucht BepInEx 6 für **Unity IL2CPP, Windows x64**. Nimm nicht versehentlich die Mono-Version.

1. Installiere BepInEx nach der [offiziellen Anleitung](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html). Getestet wurde Build `6.0.0-be.788+5b766a3` von [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
2. Starte Wanderburg einmal über Steam und schließe es wieder. BepInEx legt dabei seine Ordner an.
3. Kopiere anschließend `WanderburgDamageStats.dll` wie oben beschrieben nach `BepInEx\plugins`.

Eine noch kürzere Kopieranleitung liegt im Release-ZIP als `INSTALLATION.txt`.

## Was du im Spiel siehst

- Im Run: tatsächliche DPS der letzten 20 Sekunden und Schadensanteil jeder montierten Waffe.
- Im Upgrade-Menü: eine Bewertung der drei angebotenen Karten.
- Bei normalen Zahlen-Upgrades: eine Empfehlung für die stärkste berechenbare Karte.
- Bei Spezial- oder Legendär-Effekten: **Zahlensieger** statt einer vorgetäuschten sicheren Empfehlung. Noch nicht vollständig modellierte Effekte heißen **situativ**.

## Mod abschalten oder entfernen

Drücke **F8**, wenn du nur die Anzeige ausblenden möchtest.

Für eine dauerhafte Deaktivierung schließt du zuerst das Spiel und startest dann `Disable-Mod.ps1` aus dem Release-Paket. `Enable-Mod.ps1` schaltet den Mod später wieder ein.

Zum vollständigen Entfernen löschst du bei geschlossenem Spiel nur diese Datei:

```text
BepInEx\plugins\WanderburgDamageStats.dll
```

## Falls der Mod nicht erscheint

- Prüfe, ob die DLL wirklich direkt in `BepInEx\plugins` liegt.
- Starte das Spiel über Steam. Beim direkten Start von `Wanderburg.exe` wurde Steamworks in unserem Test nicht richtig initialisiert; dadurch können Steam-Statistiken und Leaderboards für diesen Run ausfallen.
- Öffne `BepInEx\LogOutput.log` im Spielordner und suche nach `Wanderburg Damage Stats 0.2.0`.
- Die Einstellungen liegen in `BepInEx\config\io.github.lia-xim.wanderburg-damage-stats.cfg`. Dort kannst du Größe und Position des Fensters ändern.

## Wie die Empfehlung berechnet wird

Damage Stats liest Wanderburgs vorhandene `StatisticsQuery.CurrentRun`-Zähler in kurzen Abständen aus. Die Mod patcht keine Gameplay- oder Statistikmethode und verändert weder Kampfwerte noch Save-Daten.

Für normale Upgrades berechnet er die relative Änderung. Schaden und Cooldown zählen direkt. Ladungen, Dauer, Größe und Geschwindigkeit erhalten vorsichtige Gewichtungen. Danach verbindet der Coach den Karteneffekt mit dem tatsächlichen Schadensanteil der betroffenen Waffe. Eine Anzeige wie `≈ +12% Build-Output` ist deshalb eine begründete Schätzung für diesen Run, keine Garantie für jede Spielsituation.

## Selbst bauen

Der Quellcode liegt vollständig unter `src/`. Du brauchst ein .NET-6-kompatibles SDK und eine lokale Wanderburg-Installation mit erzeugten BepInEx-Interop-Dateien.

```powershell
dotnet build ./src/WanderburgDamageStats.csproj -c Release
dotnet run --project ./tests/PreviewTests.csproj -c Release
```

`build.ps1` führt Build und Tests aus und erstellt danach das installierbare ZIP unter `artifacts/`. Bei einem anderen Spielpfad kannst du ihn so angeben:

```powershell
./build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Wanderburg Game"
```

Nach Spielupdates können sich interne Funktionen und Texte ändern. Dieses Repository enthält keine Spieldateien oder extrahierten Assets. Der eigene Quellcode steht unter der [MIT-Lizenz](LICENSE).
