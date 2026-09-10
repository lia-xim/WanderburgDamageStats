# Prüfung des Empfehlungsmodells – 10.09.2026

Geprüfter Stand: Quellcode 0.3.1 und lokale IL2CPP-Metadaten sowie ausgewählte native Methoden der installierten Wanderburg-Version. Der aktuelle BepInEx-Log bestätigt das Laden von 0.3.1; er enthält beim Prüfen noch keine `Overall model card`-Zeilen. Die tatsächliche Zuordnung der Vorschauobjekte zu angebotenen Karten ist damit weiterhin nicht live bestätigt.

## Ergebnis

Das Modell ist derzeit eine grobe Schätzung für einzelne Schadenskanäle, keine belastbare Empfehlung für den insgesamt besten Build. Beim Ram ist ein konkreter Bedeutungsfehler nachweisbar: Angezeigter Flat Damage ist nicht der vollständige Trefferschaden. Die allgemeinen Feldnamen bedeuten je nach Modul Unterschiedliches.

Die Untersuchung erfasst Kartenstruktur und zahlreiche Effektklassen. Sie ist kein vollständiger Export aller tatsächlich freigeschalteten Karten mit ihren vier Seltenheitswerten. Vorhandene Felder beweisen allein weder eine erreichbare Karte noch einen aktiven Effekt; dafür müssen Upgrade-Assets, Voraussetzungen und Prefabs zusammen ausgelesen werden.

## Aktuelle Berechnung

`BuildCoachCore.AssessProjected` berechnet:

`Faktor = Verhältnis angezeigter Schaden × Munitionsverhältnis^a × Cooldownverhältnis^b`

- Auto: a = 0,8 und b = 1.
- Aktiv: a = 0,6 und b = 0,65.
- Build-Zuwachs: `(Faktor − 1) × bisheriger Schadensanteil des gewählten Kanals`.
- Duration, Size und Speed sind nur Hinweise.
- Der Text-Fallback addiert einzelne Prozentänderungen, mit Gewicht 1 für Damage und 0,4 für Charges/Cooldown.

Diese Gewichte sind Heuristiken im Mod-Code, keine aus dem Spiel nachgewiesenen Formeln. Fallback und Vorschau können daher dieselbe Karte unterschiedlich bewerten. Der Fallback begrenzt einzelne relative Gewinne auf +600 %, das Vorschauverhältnis auf Faktor 20. Eine Veränderung von 0 auf einen positiven Wert liefert in der Vorschau Faktor 1. Negative Gesamtgewinne werden auf 0 gesetzt.

## Konkreter Nachweis: Ram

In `Module2Ramme.OnTriggerEnter` (RVA 0x726150) lässt sich dieser Teil der Trefferberechnung nachvollziehen:

`Basis = (generalBalanceDamageMult × damageOverSpeedCurve(Geschwindigkeit) × passiveAmmo + passiveFlatDamageAdded) × frontSlotDamageFactor`

Im Fury-Modus wird zusätzlich mit `currentActiveBaseDamage + flatDamageAdded` multipliziert. Anschließend folgen weitere situationsabhängige Artefaktfaktoren und Begrenzungen; die obige Formel ist ausdrücklich nur der nachvollzogene Kern vor der endgültigen Schadensauflösung.

Belege in `../work/inspect/audit-0x726150.asm`: 0x180726680–0x18072675b liest Geschwindigkeit, wertet die Kurve aus, multipliziert `passiveAmmo`, addiert Flat Damage und wendet den Fury-Faktor an. Weitere Zugriffe betreffen unter anderem Nahkampf- und Vollleben-Boni. `EnableRamCollisionChain` wird für den passenden Spezialzweig aufgerufen.

Folgen für das Modell:

- `passiveAmmo` ist hier ein Schadensmultiplikator und keine Anzahl unabhängiger Schüsse. Die Potenz 0,8 ist hier sachlich falsch.
- Der Quotient der angezeigten Werte „1 → 31“ ist nicht der Quotient zweier vollständiger Ram-Treffer. Die große Common-Empfehlung ist deshalb nicht belastbar.
- Aktiv und Auto wirken zusammen. Ein Aktiv-Buff verändert Ram-Kollisionen; isolierte Kanalbewertung kann diese Wechselwirkung verfehlen.
- In `Module2Ramme.<FuryMode>d__41.MoveNext` (RVA 0x746b50) werden `activeAbilityDuration` und `activeAbilityCurrentSpeed` für einen zeitlich begrenzten Geschwindigkeitsbuff gelesen; ein Ram-Boost wird ebenfalls ausgelöst. „Projectile Speed“ ist bei diesem Modul somit kein reiner Projektil-Reisewert.
- Der Trefferpfad kann `EndFuryMode` aufrufen. Dauer darf deshalb auch hier nicht einfach linear als mehr Trefferzeit multipliziert werden.

Die Methoden `CalculateDisplayedActiveDamage`/`CalculateDisplayedPassiveDamage` (RVA 0x5C3FF0/0x5C41E0), die unsere `WeaponStats.Read` nutzt, liefern Anzeigewerte mit einigen Modulausnahmen. Ihre untersuchten nativen Pfade bilden diese Ram-Geschwindigkeitsformel nicht ab.

## Fehlende Faktoren

| Priorität | Faktor | Beleg im Spiel | Was dem Mod fehlt |
|---|---|---|---|
| P0 | Bedeutung pro Waffentyp | Ram: `passiveAmmo`, Geschwindigkeitskurve, Fury | Adapter für echte Trefferformeln statt einheitlicher Feldquotienten |
| P0 | Alle betroffenen Kanäle | `ModuleUpgrade` kann aktive und passive Werte sowie beide Cooldowns verändern | Vorher/Nachher beider Kanäle, inklusive Buff-Wechselwirkung |
| P0 | Korrekte Vorschau | interne Seltenheit, ausgewählte Kern-/Zusatzattribute, Effektwechsel | Laufzeitbeweis für Kartenindex, Modul-ID, tatsächlichen Roll und initialisierte Preview-Werte |
| P1 | Dauer und Tickfrequenz | `DamageZone2.tickInterval/tickDamage/duration`; `FlameZone.damageTick/directDamage/fireDuration`; Laser-Ticks | Effektive Tickzahl, Verweildauer des Gegners, Refresh-/Stackingregeln |
| P1 | Tatsächliche Angriffshäufigkeit | Cooldown, Salvenintervalle, Bereitschaft, Aktivierungsereignisse | Schüsse/Aktivierungen pro Zeit; Zeitpunkt des Cooldownstarts; manuelle Nutzung |
| P1 | Bedingte Cooldown-Boni | `ReadyActiveAutoAttackCooldown`, `NitroAutoAttackCooldown`, `StationaryAutoAttackCooldown` | Bereitschaft, Boost- und Stillstandsanteil statt fixer Exponenten |
| P1 | Mehrfachtreffer und Reichweite | `ProjectileV2.passThroughEnemies/aoeDamageRadius`; modulbezogene `customRange`; Chain Ram | Trefferquote, Ziele pro Angriff, Durchschläge, Kettenlänge und Gegnerdichte |
| P1 | Folgeeffekte | `ProjectileV2`: Flammenfeld, Kettenblitz, Zusatzexplosion, Pfeil-/Mörserregen, Auslösewahrscheinlichkeiten | Erwarteter Zusatzschaden je Auslösung und Zuordnung zum verursachenden Upgrade |
| P1 | Artefakte und Status-Synergien | `ArtifactSystem`: Typ-/Slotfaktoren, Schaden gegen verlangsamte Ziele, Brenndauer, Knockback-Kollisionsschaden | Bestehenden Bonus nicht doppelt zählen; neue Synergien und veränderte Auslösehäufigkeit berechnen |
| P1 | Beschworene Einheiten | Barracks: aktive/passive/legendäre Einheitenlisten, Anzahlen, Zielentfernungen | Einheitenanzahl × eigene Angriffe × Überlebens-/Angriffszeit |
| P2 | Zielabhängiger Schaden | `DamageZone2.percentageHealthDamagePerTick`; `ProjectileV2.executeHealthPercentageThreshold`; Agent-Resistenzen | Boss-/Gruppenszenario, Ziel-HP, Immunitäten und Overkill |
| P2 | Überleben und Kontrolle | Reparaturmodul; Slow, Stun, Fear, Knockback; Fahrzeug HP/Nitro | Überlebenswert und ermöglichtes Angreifen; keine erfundenen DPS-Prozente |
| P2 | Spätere Entwicklung | `ModuleUpgrade.addToPoolUpgrades`, Tier, Legendary-/Special-Verknüpfungen | Erreichbare Folge-Upgrades, vorhandener Build, Restlaufzeit und Angebotswahrscheinlichkeiten |

Bei Duration muss zwischen zusätzlicher Tickzahl, längerem Buff, längerer Projektilflugzeit und bloßer Verteilung einer festen Salve unterschieden werden. Bei Speed zwischen Projektilreise, Fahrzeugbuff und Angriffsgeschwindigkeit. Ein universeller Bonus oder ein universelles Ignorieren ist jeweils falsch.

## Weitere Verzerrungen im Mod

- Das 20-Sekunden-Fenster misst eine konkrete vergangene Kampfsituation. Ein seltener Aktivangriff oder frisch eingebautes Modul kann Anteil 0 haben, obwohl sein Upgrade künftig wertvoll ist.
- Unbekannte Kanalzuordnung liefert für Cooldown derzeit 0 statt „nicht bestimmbar“. Ungewissheit wird damit wie Wirkungslosigkeit behandelt.
- `ChoicePatch` behält nur Text mit numerischen Vorher/Nachher-Markierungen. Freie Effektbeschreibungen gehen für die Bewertung verloren.
- `ResolveModule` nimmt die erste passende Modul-ID; mehrere Instanzen desselben Modultyps und aggregierte Telemetrie brauchen eine geprüfte Zuordnung.
- Auswahl und angezeigte Rangliste verwenden unterschiedliche Sortierung: Die Rangliste schiebt bedingte Effekte pauschal nach hinten, obwohl sie die Auswahl gewinnen können.
- Die Tests bestätigen die implementierte Heuristik. Beispielsweise erwartet ein Test weiterhin, dass die Common-Ram-Karte vor der Epic-Karte liegt. Das beweist nicht die Richtigkeit im Spiel.
- `OVERALL PICK` und teilweise „high confidence“ versprechen mehr, als diese Datenbasis trägt.

## Noch nicht als aktive Kartenmechanik bestätigt

`ModuleUpgrade` besitzt Crit-, Lifesteal- und Execute-Upgradefelder unter „Future Stats“. Das ist kein Beweis für momentan verfügbare Crit-/Lifesteal-Karten. Execute existiert zusätzlich auf Projektilen; konkrete erreichbare Angebote und Prefab-Konfigurationen müssen separat bestätigt werden.

## Empfohlene Umsetzung

1. Zuerst Ram korrekt modellieren und die drei problematischen Angebote mit echten Vorher-/Nachher-Snapshots nachrechnen. Keine Seltenheits-Pauschalboni ergänzen.
2. Einen lesenden Katalog der Upgrade-Assets, Roll-Werte, Effekt-Prefabs und Modul-Spezialzustände exportieren. So lässt sich die Abdeckung tatsächlich je Karte zählen.
3. Waffenadapter für Einzelprojektil/Salve, Ram/Buff, Tickfläche/Strahl und beschworene Einheiten einführen. Gemeinsame Basis: Schaden pro Treffer, Treffer pro Angriff, tatsächliche Angriffe pro Zeit und Folgeeffekte.
4. Erst danach Artefakt-Synergien, Gegnergruppen/Bosse und spätere Upgradepfade einbeziehen. Unbekannte Effekte transparent als unbekannt behandeln und bei unvollständigem Vergleich keinen sicheren Gesamtsieger behaupten.

Dies ist eine Analyse. Der Laufzeitcode und die installierte Mod wurden während dieser Prüfung nicht verändert.
