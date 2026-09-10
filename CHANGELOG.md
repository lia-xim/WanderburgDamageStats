# Wanderburg Damage Stats – Changelog

## 0.2.3 - 2026-09-10

- Parses numerical values wrapped in rarity-color markup, fixing Uncommon cards incorrectly shown as situational.
- Scores ability cooldown from observed ability damage rather than the weapon's entire damage share.
- Applies a conservative utilization factor to cooldown improvements.
- Keeps rarity out of the score because the rolled before/after values already reflect it.

## 0.2.2 - 2026-09-10

- Keeps numerical legendary and special cards in the ranking instead of discarding them as purely situational.
- Detects whether a special card affects the auto, active, or cooldown channel from the visible card UI.
- Shows the estimated total DPS after the recommended upgrade alongside the current DPS.
- Adds individually copyable Steam Guide sections under `steam-guide/`.

## 0.2.1 - 2026-09-10

- Changes the complete in-game overlay and configuration descriptions to English.
- Moves the upgrade recommendation above the weapon list so the suggested card remains visible.
- Detects visible upgrade cards even when the game's internal menu flag is delayed.
- Uses English decimal formatting throughout the overlay.

## 0.2.0 - 2026-09-10

- Ersetzt die reine Basiswert-Anzeige durch tatsächliche 20-Sekunden-DPS und Schadensanteile.
- Bewertet die drei Upgrade-Karten anhand ihrer realen Vorher-/Nachher-Werte und des aktuellen Builds.
- Kennzeichnet nicht vollständig modellierte Spezial- und Legendär-Effekte als situativ.
- Liest Kampfdaten über `StatisticsQuery.CurrentRun`, ohne Gameplay- oder Statistikmethoden zu patchen.
- Behält F8 als schnellen Schalter für das Overlay bei.

## 0.1.0 - 2026-09-10

- Erste lokale Machbarkeitsversion mit Basiswerten und Upgrade-Vergleich.
