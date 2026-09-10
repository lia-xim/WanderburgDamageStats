# Anfängerhilfe und Vorausplanung – Untersuchung vom 10.09.2026

Status: Ein erster Zukunftsplaner ist in 0.5.0 implementiert. Er simuliert standardmäßig fünf weitere normale Modul-Upgrades; 1–8 sind einstellbar. Build-/Modelltests und der Live-Test des neuen Spieladapters werden getrennt geprüft.

## Datenbasis

Der lokale, einmalige Katalogexport erfasste 26 Modul-IDs, 458 Upgrade-Assets (130 passive, 108 Cooldown-, 100 aktive, 80 Spezial- und 40 legendäre Upgrades) und 120 Projektilobjekte. Das sind geladene Assets; ihre Existenz beweist weder die persönlichen Freischaltungen noch ihre Angebotswahrscheinlichkeit. Rohdaten bleiben lokal unter BepInEx/config/WanderburgDamageStats.catalog.json und werden nicht mit dem Mod-Paket verteilt.

Anfängerfreundliche Ansatzpunkte im Katalog:

- Archer Crew und Fire Crew greifen nahe Gegner automatisch an. Bei Fire Crew ist die Zielsuche zusätzlich im nativen FirePassiveProcess nachgewiesen.
- Electric Mage besitzt passive Kettenblitz-Upgrades und Betäubungseffekte.
- Force Mage besitzt unter anderem Auto-Slow, Auto-Knockback und Auto-Damage-Upgrades.
- Carpenters reparieren das Fahrzeug; ein DPS-Ranking kann ihren Wert nicht erfassen.
- Ram erfordert passende Fahrgeschwindigkeit und Kollisionsgelegenheiten. Fury-Nutzung und vorzeitiges Ende bei Kollisionen beeinflussen den Nutzen seiner Upgrades.

Eine plausible Anfängerstrategie ist daher automatischer Schaden mit Kontrolle und bedarfsgerechter Reparatur. Eine bestimmte Kombination als besten Build zu bezeichnen wäre ohne Vergleichsruns unbelegt.

## Ram: Warum Vorausplanung sinnvoll ist

Die passive und die aktive Ram-Linie besitzen jeweils fünf aufeinanderfolgende Upgrade-Assets. Die passive Linie addiert pro Common-Karte 30 Flat Damage; die aktive Linie addiert beim getesteten Rare-Roll 0,3 auf den Fury-Multiplikator. Seltenheit ist Bestandteil der tatsächlichen Werte, kein zusätzlicher Score-Bonus.

Die angezeigte 1 ist nicht der vollständige aktuelle Ram-Trefferschaden. Der native Kern lautet vor weiteren bedingten Effekten:

`(Geschwindigkeitsbasis × passiver Multiplikator + Flat Damage) × Slotfaktor × Fury-Faktor während Fury`

1,6 auf 1,9 erhöht denselben Fury-Treffer um 18,75 %. Späterer Flat Damage wird mitmultipliziert. Gleichzeitig hat die andere Route bereits eine zusätzliche Flat-Damage-Karte: Beide Startentscheidungen müssen gleich viele weitere Picks erhalten.

Ein ausdrücklich hypothetischer Vergleich nimmt auf beiden Wegen drei weitere Common-Flat-Karten, gleiche Geschwindigkeit, durchgehend Fury und Slotfaktor 1 an. Dauer- und Geschwindigkeitsänderungen der Rare-Karte sind hier ausgeklammert:

| Geschwindigkeitsabhängige Basis vor Flat Damage | Common zuerst: (Basis + 120) × 1,6 | Rare zuerst: (Basis + 90) × 1,9 |
|---|---:|---:|
| 50 | 272 | 266 |
| 100 | 352 | 361 |
| 200 | 512 | 551 |

Damit kann die Reihenfolge später kippen. Sie muss es nicht. Diese Tabelle ist kein prognostizierter Schaden des laufenden Runs.

## Umgesetzter Vergleich (Standard: fünf weitere Entscheidungen)

1. Jede angebotene Karte auf eine eigene Kopie des aktuellen Builds anwenden.
2. Den tatsächlich verbleibenden Upgrade-Pool und verknüpfte Nachfolger fortschreiben. Voraussetzungen, Spezial-/Legendär-Auswahl und fremde Modulreferenzen prüfen.
3. Zukünftige Angebote anhand der Spielgewichte und Seltenheitswahrscheinlichkeiten erzeugen. Alle Startentscheidungen unter denselben Zufallsannahmen vergleichen. Unbekannte Angebote nicht als garantiert verfügbar behandeln.
4. Nach jedem Angebot nur mit dem dann bekannten Menü entscheiden. Ein Simulator darf nicht die späteren Zufallsziehungen vorhersehen und damit frühere Picks optimieren.
5. Schaden während der Entwicklung sowie Endzustand betrachten. Frühe Schwäche und späterer Gewinn sind unterschiedliche Größen. Überleben und Reparatur benötigen ein eigenes Modell statt erfundener DPS-Zuschläge.
6. In der Anfängeransicht eine klare Empfehlung mit kurzem Grund zeigen. Sofortwirkung, Zukunftspotenzial und Annahmen in den Details lassen.

Umgesetzt: normale Auswahl-/Poolregeln, Verkettung auf Build-Kopien und ein mehrstufiger Stichproben-Simulator. Der Live-Test mit 0.5.0 hat drei Suchen über jeweils fünf Folge-Upgrades und 64 Wege pro modellierter Startkarte bestätigt (119–296 ms Rechenarbeit, längster Teilschritt 8 ms). Die englische Anzeige war im Upgrade-Menü lesbar. Side Barracks war dort noch ohne Berechnung. 0.6.0 ergänzt ein Modell für die dauerhafte Truppe und kennzeichnet unvollständig berechenbare Angebote als Teilvergleich. Der neue Barracks-Adapter ist durch Regressionstests geprüft; ein neuer Live-Test mit einer passenden Karte steht noch aus.

Noch offen sind Überlebenswert, die nicht unterstützten Waffen-/Spezialmodelle und kontrollierte Vergleichsruns zum tatsächlichen Nutzen der Empfehlungen. Die simulierte Folgeentscheidung schaut jeweils zwei Entscheidungen breit in die Zukunft (vier unabhängige Angebotsstichproben pro Kandidat); diese begrenzte Strategie wird über den gesamten Horizont wiederholt. Das ist kein vollständig gelöster Suchbaum. Details und Tests stehen in [MODEL_COVERAGE.md](MODEL_COVERAGE.md).

Nachtrag: Die normale Ziehung und Poolfortschreibung wurden inzwischen nativ untersucht. Karten entstehen beim Erzeugen des Menüs mit einem auch außerhalb der Kartenauswahl verwendeten Zufallsgenerator. Auswahlgewichte stammen aus upgradeModuleRarities nach Tier, nicht einfach aus dem rarity-Feld des einzelnen Assets. Befunde, Sonderfälle und Grenzen stehen in [RNG_INVESTIGATION.de.md](RNG_INVESTIGATION.de.md).
