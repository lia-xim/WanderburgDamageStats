# Können wir zukünftige Karten auslesen?

Untersucht am 10.09.2026: native Methoden der lokal installierten GameAssembly.dll, IL2CPP-Metadaten und Unity-6-Dokumentation. Dies ist eine statische Untersuchung. Es wurden keine Karten neu gezogen, keine RNG-Zustände verändert und keine Gameplay-Methoden ausgeführt.

## Ergebnis

Die normalen Modul-Upgrades werden bei GenerateSelectionForUpgradingModule neu ausgewählt. Im untersuchten Pfad gibt es keine vorausgefüllte Liste der nächsten drei oder fünf Kartenmenüs. Kartenauswahl, normale Seltenheit und Attributauswahl verwenden UnityEngine.Random. Auch Projektilinitialisierung enthält Aufrufe desselben globalen Zufallsgenerators.

Der Zufallszustand ist prinzipiell auslesbar. Er bestimmt jedoch die nächsten Zufallszahlen, nicht die späteren Karten unabhängig vom restlichen Spiel. Zusätzliche Aufrufe zwischen zwei Upgrade-Menüs verschieben den Zustand. Außerdem verändert die aktuelle Kartenwahl den zukünftigen Kartenpool. Für eine exakte mehrstufige Vorhersage müssten auch die relevanten zukünftigen Aktionen, Zufallsaufrufe und Zustandsänderungen reproduziert werden.

## Native Belege

| Schritt | Nachweis |
|---|---|
| Pool aufbauen | ModuleSelection.GenerateSelectionForUpgradingModule, RVA 0x5CAEF0: liest moduleUpgradePool, entfernt doppelte Kandidaten, prüft vorhandenes Modul und bereits installierte Upgrades. |
| Auswahlgewichte | 0x5CB34F–0x5CB398 sowie 0x5CB446–0x5CB484: upgradeModuleRarities[upgradeTier - 1]. Das Feld ModuleUpgrade.rarity ist hier nicht die verwendete Auswahlgewichtung. |
| Drei Karten ziehen | 0x5CB496 ruft UnityEngine.Random.Range auf; 0x5CB538 entfernt den gewählten Kandidaten aus der temporären Liste. Wiederholung bis zu drei Karten: gewichtete Ziehung ohne Zurücklegen. |
| Seltenheit | ModuleSelection.Roll, RVA 0x5CE990: CalculateLuckWeights, danach UnityEngine.Random.Range bei 0x5CE9F7. |
| Sonderfall Seltenheit | GenerateSelectionForUpgradingModule bei 0x5CB769: bei aktivierter Überschreibung wird stattdessen RNGNeeds.ProbabilityList<int>.PickValue verwendet. Dessen Seed-/Auswahlregeln sind hier noch nicht vollständig untersucht. |
| Attribute | SetAttributesAndUpgradeButton: RandomRangeInt bei 0x5CF58F und 0x5CF74F für Kern-/Zusatzattribut-Auswahl. |
| Reroll | TryRerollCurrentModuleUpgradeSelection bei 0x5D1C3F ruft denselben Generator erneut auf. |
| Pool nach Kauf | UpgradeChosen bei 0x5D2442 ruft AdvanceUpgradePool. Dort wird die gekaufte Karte entfernt. Normale Nachfolger werden hinzugefügt, sofern nicht bereits installiert/im Pool und sofern die Modul-ID passt. Spezialkarten durchlaufen diesen normalen Nachfolgerzweig nicht; bei legendären Karten wird die korrespondierende Alternative entfernt. |
| Fremde RNG-Verbraucher | ProjectileV2.StartSetup enthält Random.Range bei 0x639303, 0x639349 und 0x639368. Diese Aufrufe sind bedingt, zeigen aber die gemeinsame Verwendung durch Karten- und Projektilcode. Ihre konkrete Häufigkeit im aktuellen Run wurde nicht live gemessen. |

Disassembly-Auszüge liegen lokal unter ../work/inspect/rng-*.asm und ../work/inspect/setchoice.asm.

Unity bestätigt den global geteilten Zustand und die Möglichkeit zum Auslesen/Sichern:

- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Random.html
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Random-state.html

## Was für den Mod realistisch ist

**Bereits erzeugtes Menü:** genaue Karten, Seltenheit und Attribute auslesen; das tut der Mod bereits.

**Unmittelbare hypothetische Ziehung:** aus einer Kopie des RNG-Zustands und des Pools grundsätzlich rekonstruierbar, wenn sämtliche beteiligten RNG-Verfahren, Aufrufreihenfolgen und UI-Nebenwirkungen nachgebildet sind. Noch nicht implementiert oder live bewiesen. Dies ist keine Garantie für das nächste Menü nach weiterem Gameplay.

**Drei bis fünf Entscheidungen voraus:** einen Entscheidungsbaum mit Zufallsknoten aufbauen. Jede heutige Wahl verändert einen simulierten Build und Kartenpool. Künftige Angebote werden mit den tatsächlichen Gewichten, Luck-Werten und Attributregeln verteilt. Der simulierte Spieler entscheidet jeweils erst anhand des dann vorliegenden Angebots. Die Bewertung umfasst aktuelle Leistung und spätere Entwicklung; Überleben benötigt eigene belastbare Größen.

Für größere Tiefe können Stichproben und begrenzte Suchbreite den vollständigen Baum ersetzen. Welche Tiefe in einer kurzen UI-Pause sinnvoll erreichbar ist, muss gemessen werden. Mehr Tiefe behebt keine falschen Waffenmodelle oder unbekannten Synergien.

**Festgelegte zukünftige Angebote:** ein eigener, vom übrigen Spiel isolierter Karten-Zufallsgenerator könnte vorhersehbare Ziehungen ermöglichen. Das wäre eine Gameplay-Änderung, keine reine Vorhersage des unveränderten Spiels, und wurde nicht eingebaut.

## Konsequenz für die Empfehlung

Ein Ziel wie „höchster erwarteter Build-Nutzen nach fünf Picks“ ist mit probabilistischer Vorausplanung sinnvoll. Eine Aussage wie „diese fünf Menüs werden sicher kommen“ ist durch die untersuchte Architektur nicht gedeckt. Selbst bei bekannten Karten braucht die Bewertung noch korrekte Schadens-, Synergie- und Überlebensmodelle.
