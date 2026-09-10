# Wanderburg Damage Stats

**Deutsch** | [English](README.md)

Wanderburg Damage Stats schaut während deines Runs, welche Waffen tatsächlich den Schaden machen. Sobald du ein Upgrade wählen darfst, vergleicht die Mod die drei Karten und zeigt dir, welche davon deinen aktuellen Build voraussichtlich am stärksten verbessert.

Das Projekt ist eine frühe Alpha für Wanderburg EA 0.9.8 unter Windows. Es ist ein inoffizielles Community-Projekt und nicht mit Randwerk verbunden.

## Installation – ganz kurz

Wenn du BepInEx bereits für Wanderburg installiert hast, brauchst du nur eine Datei zu kopieren:

1. Lade das neueste `WanderburgDamageStats`-ZIP bei den [GitHub-Releases](https://github.com/lia-xim/WanderburgDamageStats/releases) herunter.
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

Das war’s. Im Run sollte links oben **DAMAGE STATS** erscheinen. **F7** schaltet die Empfehlungen um, **F8** die gesamte Anzeige. Du kannst auch den Coach-Button unter den Schadensbalken anklicken.

Du hast Wanderburg in einer anderen Steam-Bibliothek installiert? Öffne in Steam deine **Bibliothek**, klicke Wanderburg mit der rechten Maustaste an und wähle **Verwalten → Lokale Dateien durchsuchen**. Der Ordner, der sich öffnet, ist der richtige. Von dort gehst du weiter zu `BepInEx\plugins`.

## Wenn du BepInEx noch nicht hast

Der Mod braucht BepInEx 6 für **Unity IL2CPP, Windows x64**. Nimm nicht versehentlich die Mono-Version.

1. Installiere BepInEx nach der [offiziellen Anleitung](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html). Getestet wurde Build `6.0.0-be.788+5b766a3` von [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
2. Starte Wanderburg einmal über Steam und schließe es wieder. BepInEx legt dabei seine Ordner an.
3. Kopiere anschließend `WanderburgDamageStats.dll` wie oben beschrieben nach `BepInEx\plugins`.

Eine noch kürzere Kopieranleitung liegt im Release-ZIP als `INSTALLATION.txt`.

## Was du im Spiel siehst

- Große Gesamt-DPS, darunter Schadensbalken und DPS je Waffe. Diese Werte stehen immer über den optionalen Empfehlungen.
- Im Upgrade-Menü: eine Bewertung der drei angebotenen Karten.
- **BUILD OUTLOOK** vergleicht mögliche Wege durch die nächsten fünf normalen Modul-Upgrades, einschließlich späterer Kombinationen aus Flat Damage und Multiplikatoren.
- **NOW → AFTER UPGRADES** zeigt den sofortigen Schadensgewinn und den geschätzten späteren Build. Beide Angaben beziehen sich auf deinen jetzigen Build.
- **F9** öffnet die ausführliche Sofortbewertung und die noch unsicheren Faktoren. Manche Spezialkarten oder Effekte bleiben ohne Schätzung.
- Mit **F7** oder dem Coach-Button wird die Mod zum reinen Damage-Meter. Die Einstellung bleibt gespeichert. Lange Details lassen sich separat scrollen.

## Ein paar Entscheidungen vorausdenken

Der Planer probiert jede unterstützte Karte auf einer Kopie deines Builds aus. Danach zieht er mögliche Folgeangebote aus deinem aktuellen Modul-Pool, berücksichtigt freigeschaltete Folge-Upgrades und vergleicht den Schaden entlang der Wege. Der Pick mit dem höchsten durchschnittlichen Verlaufsscore wird vorgeschlagen. Die Prozentzahl darunter sagt, wie oft diese Karte in den simulierten Vergleichen vorne lag. Das ist **keine Gewinnchance für deinen Run**.

Standard sind **fünf weitere Upgrade-Entscheidungen**, mit bis zu 64 simulierten Wegen pro aktueller Karte. In der Konfigurationsdatei kannst du unter `[Planning]` den Wert `FutureUpgrades` auf 1 bis 8 setzen. Mehr Tiefe kostet Rechenzeit und erhöht die Unsicherheit. Die Suche läuft in kleinen Schritten über mehrere Frames; die Anzeige aktualisiert sich nach vollständig verglichenen Durchläufen.

Das sind mögliche Angebote, keine Vorschau auf die tatsächlich nächsten Karten. Der Planer nutzt einen eigenen Zufallsgenerator und verändert weder deine Karten noch den Zufallszustand des Spiels. Auch die simulierten Folgeentscheidungen kennen spätere Ziehungen nicht im Voraus.

Die Suche umfasst normale Modul-Upgrades. Neue Waffen, zukünftige Artefakte, Spezial-/Legendär-Effekte, Überleben und Änderungen deines Spielstils werden noch nicht geplant. Nicht unterstützte Ziehungen werden als ausgeschlossen gekennzeichnet und nicht durch günstigere Karten ersetzt. Der Vorschlag bleibt eine Schadensschätzung – besonders dann, wenn eine der angebotenen Karten nicht berechnet werden kann.

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
- Öffne `BepInEx\LogOutput.log` im Spielordner und suche nach `Wanderburg Damage Stats`.
- Die Einstellungen liegen in `BepInEx\config\io.github.lia-xim.wanderburg-damage-stats.cfg`. Dort kannst du Größe und Position des Fensters ändern.

## Wie die Empfehlung berechnet wird

Damage Stats liest vorhandene Schadenszähler und beobachtet Aktivierungsereignisse der Module. Kampfwerte und Save-Daten werden nicht verändert.

Version 0.6.0 vergleicht beide Angriffskanäle anhand der tatsächlich gezogenen Kartenwerte einschließlich interner Seltenheit und ausgewählter Zusatzattribute. Der Ram nutzt seine native Kollisionsformel, gemessene Bewegungsgeschwindigkeiten und den beobachteten Fury-Zustand. Weitere Adapter schätzen Projektilsalven, Schadensticks und beschworene Einheiten. Die dauerhafte Truppe von Side Barracks wird mit Schaden × Einheitenzahl / Angriffstakt berechnet. Ihr Cooldown füllt fehlende Einheiten nach und wird nicht einfach als mehr Armee-DPS gerechnet. Bei wiederholten Angriffen berücksichtigt der Cooldown-Vergleich gemessene Wartezeiten; auslesbare Projektil-Folgeeffekte gehen mit Auslösewahrscheinlichkeit × Schaden × Projektilzahl ein.

Das Spiel meldet häufig nur den Gesamtschaden je Waffe. Wenn beide Angriffe unterstützt werden, schätzt die Mod ihre Anteile anhand des Angriffsschadens und beobachteter Aktivierungen. Bei nur einem modellierten Schadensangriff wird der Waffengesamtschaden verwendet. Diese Zuordnung ist gekennzeichnet und kann von den tatsächlichen Trefferanteilen abweichen.

Die Anzeige unterscheidet zwischen Schadensempfehlung und **vorläufiger** Schadensempfehlung. Die Szenarien zeigen den Einfluss verschiedener Timing- und Schadensanteil-Annahmen, keine garantierten Grenzen. Fehlende Kartendaten, nicht unterstützte Angriffsmodelle und neue Schadensquellen werden nicht mit null Prozent gleichgesetzt.

Flächenabdeckung, Brenneffekte, Kontrolleffekte, Aufprallgeschwindigkeit und manche Effekt-Overrides brauchen weiterhin eigene Modelle. Die Vorausplanung kombiniert die unterstützten Schadensfaktoren, kann fehlende Faktoren aber nicht ersetzen. Die Mod behauptet deshalb keinen garantiert besten Gesamt-Pick. Details stehen in [MODEL_COVERAGE.md](MODEL_COVERAGE.md).

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
