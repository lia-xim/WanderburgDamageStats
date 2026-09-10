# Prediction model 0.6.0

This is an offensive-output estimator, not a solved optimal-build policy. No rarity bonus is added: rarity already changes the actual offered stats.

| Model | Calculation | Proof / assumptions |
|---|---|---|
| Ram | `(balance × speed curve × passive multiplier + flat damage) × slot factor × Fury multiplier when active` | Core traced in the installed native collision method. Recent moving samples preserve speed/Fury correlation, but travel samples are not exact impact samples. Conditional artifact factors and target damage resolution are held constant. |
| Projectiles | damage per hit × projectile count | Crew Archer's passive damage assignment was traced. Other module firing/count mappings are estimates. Projectile lifetime is not multiplied into shot count. |
| Tick effects | damage per tick × duration / tick interval × count | Continuous exposure estimate; actual entry time, initial tick, burn refresh and targets leaving the field can differ. |
| Summons | damage per attack × units × lifetime / unit attack interval | Requires positive lifetime. Persistent summons with unknown lifetime remain unestimated. Replacement caps, travel and survival are uncertain. |
| Side Barracks passive | damage per unit × population cap / attack interval | Native FiringPassive replenishes to floor(round(ammo) × artifact spawn factor), plus the existing special0A bonus. Duration -1 is persistent. Same survival and target access assumed. Replenishment cooldown does not directly scale standing DPS. |
| Side Barracks active | temporary unit damage × floor(active ammo × spawn factor) × active spawn points × duration / attack interval | Native active coroutine spawns reinforcements, buffs all allied lists and later despawns its temporary list. Spawn delays, casualties and the buffs on existing troops remain unresolved. |
| Cooldown | old cycle / new cycle, preserving observed idle time | Active/auto events are observed separately. Trigger events are not proof of a successful hit. With fewer than two triggers, maximum-use timing is a conditional scenario. Overlapping recharge and recharge after an attack are compared. |
| Projectile follow-ups | trigger chance × authored damage × projectile count | Reads chain lightning (one target), arrow/mortar rain, stars, cones, extra explosions and lightning strikes. Runtime overrides, trigger conditions and coverage are explicitly uncertain. No unsupported recursive proc chain is invented. |
| Build impact | sum of channel damage shares × each channel's relative change | Both channels are evaluated. Native module totals are assigned to a sole modeled damage attack or conditionally split by attack output and observed triggers. Missing triggers use maximum-use timing. Mixed channels include alternative allocations as sensitivity scenarios. Unsupported models or a zero baseline for a new source prevent numerical comparison. Existing modifiers are not multiplied into recorded damage a second time. |

Ram duration/cooldown and speed changes are sensitivity scenarios, not guaranteed extra collision damage. Fury can end on impact. The headline compares the same sampled behavior; the scenario spread explores other timing/speed assumptions and is not a statistical confidence interval or full bound.

## Unresolved factors remain visible

- Area coverage, piercing targets, target selection, hit chance and overkill.
- Burn stacking/refresh, percentage-health damage, execute thresholds and enemy resistances.
- Crowd control, health, repair and survival value.
- Module-specific legendary/special code and effect overrides not represented by prefab fields.
- Future new modules, artifacts and special/legendary unlock events outside the normal module-upgrade pool.

These factors do not receive fabricated percentages. The UI says `TENTATIVE DAMAGE PICK` when any offered card has unresolved factors. A card without a supported estimate remains visible as `not estimated`; it is not assigned zero damage benefit. A tentative lead is not a recommendation to ignore the unestimated cards.

## Validation

The future planner is a sampled, receding-horizon policy, not a complete game-tree search. Default: five future normal upgrade menus, configurable 1–8, up to 64 paired paths per current card. The computation budget targets 2 seconds of measured search work, stopping after a complete trial with at least 16 trials. This is a soft budget; the minimum batch and a native call can exceed it. Work is yielded after individual offer evaluations, with a roughly 3 ms frame budget; native call time is not preemptible.

Each future decision compares the offered states and four independently sampled next menus per candidate. Actual rollout offers use a separate held-out random stream. This two-decision policy is repeated over the configured horizon. It does not exhaustively optimize all branches at that depth. Identical random streams across current-card branches reduce comparison noise but do not imply identical card offers when their pools differ.

The objective is the mean relative build-DPS gain over the immediate state and every subsequent upgrade state (equal weight per decision, not estimated real time). Final-state gain and the fraction of paired paths led by each current card are also shown. The route score selects the pick; simulated lead frequency is descriptive, not a calibrated probability or survival estimate. Intermediate damage matters, so the largest final number need not win.

Projection accumulates raw stats on managed records and reruns the existing attack model against frozen combat evidence. Normal upgrade successors, tier-based pool weights, current luck probabilities, rarity and selected attributes are modeled. Special/legendary upgrades and custom-rarity draws are excluded without redrawing their offer slot. An entirely unsupported menu holds the state constant. That is an incomplete scenario, not evidence of no benefit. Missing current cards and excluded future effects are labeled. Enemy mix, usage, damage shares, artifacts and mounted modules stay fixed. Ram Fury duration/cooldown/speed remain sensitivity scenarios, so this planner does not solve their tactical value.

`tests/FutureChecks.cs` checks unlocked successor paths, future synergy, equal pick counts, balanced sampling, deterministic replay, empty pools, invalid values, negative gains, ties, intermediate-vs-final objectives, weighted sampling and separation of planning/outcome RNG. Its Ram fixture verifies that later flat damage compounds with Fury. Synthetic timing fixtures do not prove native in-game performance.

`tests/ModelChecks.cs` checks algebraic fixtures including Ram's true flat-addition behavior, a Fury upgrade beating a flat upgrade in a suitable scenario, multiplicative combined upgrades, both-channel effects, cooldown idle time, ticks, summons, procs, negative changes and missing/invalid evidence. These checks do not validate every game's prefab mapping.

The runtime log writes `Model card` and `Model inputs card` records containing rolled-data availability, module ID, channel inputs, estimates and unresolved factors. Inspect those during a real upgrade menu before releasing this build publicly.

Live 0.4.1 validation on 2026-09-10 confirmed the roll integration: Ram Fury 1.6 to 1.7, Ram flat addition 0 to 45 and multiplier 4 to 4.7, and Fire Crew charges 2 to 3 matched the offered cards. Both Ram cards produced estimates in the log and user screenshot. Fire Crew correctly read its future values but lacked channel attribution; 0.4.2 addresses that separately. The aggregate-attribution change has regression coverage; its in-game validation is still pending.

Live 0.5.0 supersedes the pending Fire Crew attribution gate above: the 2→4 charge offer produced a +70% modeled build gain with a 70% native damage share. Three future searches completed at five future upgrades and 64 paths per supported root, taking 119–296 ms search work (maximum slice 8 ms). The HUD was visually inspected at 2560×1440. Side Barracks was excluded in that version; the persistent-army adapter in 0.6.0 adds separate regression coverage and requires its own live check.

Side Barracks native evidence (installed EA 0.9.8): FiringPassive.MoveNext 0x72D700, population calculation 0x72D802–0x72D88F; FiringActive.MoveNext 0x72BFC0, waves 0x72C136–0x72C269, buffs 0x72C2FB–0x72C3FF, despawn 0x72C0AF; SpawnUnit 0x710FD0, channel flat damage 0x711218–0x711254; upgrade refresh 0x72DE50 despawns/replenishes persistent units. The floor helper reaches `roundsd ...,9` at 0x51B090. Disassembly remains local, outside the source distribution.

Live 0.6.0: the redesigned HUD rendered in combat and the upgrade screen at 2560×1440. Its coach button disabled recommendations while retaining the DPS readout and bars; later toggles re-enabled the search. Repeated searches on the same paused offer returned identical results. Ram, Fire Crew and Side Ballista offers produced estimates, with a five-upgrade/64-path search taking 274–336 ms in the inspected examples. The Side Barracks regression passes locally but has not yet been observed on a new live Barracks offer. The long-details scroll limit is also not yet live verified.
