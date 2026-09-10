# Wanderburg Damage Stats – Changelog

## 0.6.0 - 2026-09-10 (alpha release)

- Rebuilds the HUD around a large total-DPS readout, individual weapon damage bars and a separate coach panel. Damage statistics stay first.
- F7 or the coach button toggles all recommendations, independently of the DPS meter. The choice is saved. F8 hides the full HUD; F9/the details button opens a scrollable model explanation below the meter.
- Adds a standing-army adapter for Side Barracks. Its passive duration of -1 means persistent units, not an invalid temporary summon. Damage, population and attack interval now produce a conditional estimate.
- Traces the passive replenishment cap, artifact spawn factor, active reinforcement waves per spawn point and upgrade refresh. A shorter replenishment cooldown is not treated as more steady army DPS.
- Keeps casualties, targeting, movement and ability buffs explicit as unresolved factors. Adds regression coverage for the user's 3→5.5 damage, 10→14 unit card.

## 0.5.1 - 2026-09-10 (local test build)

- Shows PARTIAL COMPARISON and lists each excluded current card when future planning cannot evaluate the entire offer. A modeled lead is not presented as a winner over unestimated cards.
- Releases completed simulation snapshots when the upgrade menu closes.
- The 0.5.0 live test completed three five-upgrade searches at 64 paths per modeled current card in 119–296 ms total measured search work; the longest slice was 8 ms. Fire Crew aggregate attribution now produced live estimates. Side Barracks remains unsupported.

## 0.5.0 - 2026-09-10 (local test build)

- Adds BUILD OUTLOOK: sampled future paths through five normal module upgrades, configurable from one to eight.
- Chains projected stats and unlocked upgrade pools on managed copies; flat damage and later multipliers compound.
- Uses actual tier weights and current luck probabilities with weighted offers, rarity and attribute selection. Game RNG and gameplay values are untouched.
- Compares average damage throughout each route, with separate immediate/final gains and simulated lead frequency. This is a bounded sampled policy, not a guaranteed optimal build or prediction of actual future cards.
- Unsupported special/legendary/custom-rarity draws remain excluded and labeled. Future modules, artifacts, survival and changing combat behavior are not modeled.
- Spreads searches over frames, caches projections and curve reads, and compares only balanced batches. F9 toggles detailed model explanations.
- Adds future-search regression and synthetic depth/performance fixtures; in-game validation of the new planner remains a separate gate.

## 0.4.2 - 2026-09-10 (local test build)

- Native statistics often expose only total damage per module. These totals now support a conditional channel split based on modeled attack output and observed triggers.
- A module with a single modeled damage attack, such as the tested Fire Crew, no longer waits for channel counters that the game does not provide.
- Mixed ability/auto weapons show sensitivity scenarios for different damage allocations. Missing triggers use an explicitly labeled maximum-use assumption; unsupported weapon models remain unestimated.
- Adds regression checks based on the live Fire Crew card (2 to 3 charges), aggregate damage, equal channel gains and unknown hit rates.
- Traces Fire Crew shot spacing as duration divided by charges; burn scaling stays explicitly unresolved.
- Adds an opt-in, once-per-start local loaded-asset catalog export for investigating build paths. Disabled by default; no raw game catalog is included in releases.

## 0.4.1 - 2026-09-10 (local test build)

- Fixes all cards showing "Matching game preview unavailable" in the live 0.4.0 test.
- Uses the actual upgrade asset, internal rarity, selected core/additional attributes and current module values. The visual showpiece is no longer treated as a future-stat snapshot.
- Computes future stats in managed records without spawning a clone or applying an upgrade to a game object.
- Includes the native cooldown floors, slot factors and diminishing-return size/speed formula.
- Visual showpieces were not a valid source for the complete rolled state; the 0.3.x preview claims were unverified and are superseded by this fix.
- Live log and screenshot confirmed both Ram estimates and all three cards' rolled future values. Fire Crew exposed a separate missing-channel-attribution issue addressed in 0.4.2.

## 0.4.0 - 2026-09-10 (local test build)

- Replaces the generic weighted-percent model with attack models for both channels.
- Corrects Ram flat damage, multiplier and Fury calculations using the native collision core.
- Samples Ram movement/Fury and observes active/auto triggers without patching combat methods.
- Models volleys, tick exposure, unit attack intervals and authored projectile proc expectations.
- Replaces arbitrary charge/cooldown weights with linear counts and measured idle-time timing scenarios.
- Removes the unsafe text fallback and "overall pick" claim. Unknown sources remain unestimated.
- Keeps negative gains; uses one ordering for the lead card and comparison list.
- Preserves descriptive card text, validates preview module IDs, rejects ambiguous duplicate modules, and logs model inputs.
- Keeps collecting observations while the HUD is hidden. Counts the first hit of newly appearing statistics records.
- Local build and pure model checks are separate from pending in-game validation.

## 0.3.1 - 2026-09-10

- Keeps damage multiplier changes as direct projected DPS factors through the game's future displayed-damage preview.
- Removes duration, size, range, and projectile speed from the numerical DPS multiplier.
- Shows those utility changes as context only.
- Logs whether each recommendation used Wanderburg's complete generated preview or the text fallback.

## 0.3.0 - 2026-09-10

- Reads each full game-generated upgrade preview and compares its complete future weapon state with the current state.
- Models damage, cooldown, charges, duration, size, and projectile speed together instead of scoring isolated card text.
- Calibrates the projected channel change against the damage distribution observed in the current run.
- Includes a driver breakdown and confidence label for manual or conditional effects.
- Presents the highest expected modeled outcome as the overall pick while keeping uncertainty visible in its explanation.

## 0.2.4 - 2026-09-10

- Shows the active mod version unobtrusively in the overlay header.
- Uses `DAMAGE PICK` when any offered card contains utility outside the DPS model.
- Calls out higher-rarity alternatives with unmodelled utility instead of presenting a lower-rarity damage card as universally best.
- Stops treating duration, size, range, and projectile speed as guaranteed DPS.
- Further discounts cooldown and charge gains because their value depends on player usage and encounter conditions.

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
