# Wanderburg Build Coach — 0.2.0

Ein lokaler, datengestützter Upgrade-Berater für Wanderburg EA 0.9.8, Unity 6000.0.63f1 / Windows x64 IL2CPP.

> Inoffizielles Community-Projekt. Nicht mit Randwerk verbunden oder von Randwerk unterstützt.

**Status:** frühe Alpha. Die normale Kartenbewertung funktioniert; Spezial- und Legendär-Effekte werden schrittweise als eigene Regeln ergänzt.

## Bedienung

- Im Run zeigt der Coach den tatsächlich ausgeteilten DPS der letzten 20 Sekunden und den Schadensanteil jeder montierten Waffe.
- Im Upgrade-Menü bewertet er die drei realen Angebote. Die Schätzung verbindet die Werteänderung der Karte mit dem Schadensanteil des betroffenen Aktiv-, Auto- oder Gesamtkanals.
- Die stärkste berechenbare Karte wird als **Empfehlung** markiert. Sobald eine Spezial- oder legendäre Karte nicht vollständig als Zahlenmodell abbildbar ist, heißt das Ergebnis bewusst **Zahlensieger** und die Spezialkarte bleibt als „situativ“ gekennzeichnet.
- **F8** blendet die Anzeige ein oder aus.
- Konfiguration: `BepInEx/config/local.wanderburg.damagehud.cfg`. `Scale`, `Left` und `Top` ändern Größe und Position beim nächsten Spielstart. Im Upgrade-Menü sitzt die Anzeige am linken Rand neben den Karten; im Run hält sie Abstand zur Fortschrittsleiste.

## Was die Empfehlung bedeutet

Der Coach liest Wanderburgs vorhandene `StatisticsQuery.CurrentRun`-Zähler in kurzen Abständen aus. Er patcht keine Gameplay- oder Statistikmethode. Aus den positiven Zählerdifferenzen entsteht ein Ringpuffer mit 20 Sekunden aggregierten Werten; der Mod verändert weder Kampfwerte noch Save-Daten. Falls Wanderburg die Kanal-Tags in den Statistik-IDs bereitstellt, trennt der Coach Aktiv- und Automatikschaden. Andernfalls bewertet er konservativ mit dem gesamten Modulanteil.

Für normale Zahlen-Upgrades berechnet der Coach die relative Änderung. Schaden und Cooldown zählen direkt, Ladungen, Dauer, Größe und Geschwindigkeit erhalten konservative Gewichte. Anschließend wird der Effekt mit dem tatsächlich betroffenen Schadensanteil multipliziert. `≈ +12% Build-Output` ist deshalb eine begründete Schätzung für den aktuellen Run, keine Garantie für jeden Gegner oder jede Spielsituation.

Die kosmetischen 3D-Vorschauobjekte sind keine verlässliche Quelle für Upgrade-Zahlen: Das Spiel erzeugt sie mit Seltenheit 0 und vereinfachten Attributen. Der Coach liest deshalb die originalen Vergleichszeilen der tatsächlichen Karten. Spezialeffekte werden erst empfohlen, wenn ihre individuelle Mechanik modelliert ist.

## Installation und Abschalten

Auf dem hier verwendeten PC sind BepInEx und die Mod-DLL bereits im Spielordner installiert.

Für eine spätere Neuinstallation:

1. BepInEx 6 **Unity IL2CPP Windows x64** nach der [offiziellen Anleitung](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html) installieren. Verwendeter Build: `6.0.0-be.788+5b766a3` von [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be).
2. Spiel einmal starten, damit `BepInEx/interop` erzeugt wird, und schließen.
3. `BepInEx/plugins/WanderburgBuildCoach.dll` aus dem [aktuellen GitHub-Release](https://github.com/lia-xim/WanderburgBuildCoach/releases/latest) in denselben Unterordner des Spiels kopieren.

Wanderburg anschließend immer über Steam starten. Ein direkter Start von `Wanderburg.exe` initialisiert in der getesteten Version Steamworks nicht; dadurch können Steam-Statistiken und Leaderboards für diesen Run ausfallen.

Nur diesen Mod abschalten: Spiel schließen und `Disable-Mod.ps1` aus dem Release-Paket ausführen. `Enable-Mod.ps1` aktiviert ihn wieder. BepInEx und andere Plugins bleiben erhalten. Alternativ F8 verwenden, um nur die Anzeige auszublenden.

## Quellcode und Prüfung

`src/` enthält den vollständigen Mod-Quellcode. Build mit einem .NET-6-kompatiblen SDK:

```powershell
dotnet build ./src/WanderburgBuildCoach.csproj -c Release
dotnet run --project ./tests/PreviewTests.csproj -c Release
```

`build.ps1` führt beide Schritte aus und erzeugt anschließend das installierbare ZIP unter `artifacts/`.

Bei abweichendem Spielpfad `-p:GameDir="D:\SteamLibrary\steamapps\common\Wanderburg Game"` an den Build anhängen. Referenzen werden aus der lokalen BepInEx-Installation gelesen. Spielcode und Spielassets sind nicht im Paket enthalten.

Nach Spielupdates können sich interne Funktionen und Texte ändern; die hier dokumentierte Version ist die getestete Grundlage.

## Lizenz

Der eigene Quellcode steht unter der [MIT-Lizenz](LICENSE). Wanderburg, seine Namen und Inhalte gehören den jeweiligen Rechteinhabern. Dieses Repository enthält keine Spieldateien oder extrahierten Assets.
