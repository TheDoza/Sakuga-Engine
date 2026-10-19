# Burst

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** every fighter (engine logic + universal state 18, stamped automatically) · **Rollback-safe:** Yes (the gauge is saved with each fighter, all integer math)

## What it does
Press **A+B+C+D while getting hit** (or the **H** macro button) (ground or air, including wall and ground bounces and other hard knockdown hits) to Burst. Not while lying on the floor (Hit_Down) or getting up (Hit_GetUp). It costs a **full Burst gauge**. A Burst:
- does **0 damage** and is **invincible** from frame 1 to 31 (throws included);
- comes out on frame 6, is active for 6 frames, then has 43 frames of recovery (−24 if blocked);
- **launches** the opponent far away. They can't air-tech and land in a soft knockdown (ground tech allowed), so both reset to neutral;
- can be blocked by a **standard guard or a barrier**. If the opponent sees it coming, they block and punish the recovery.

Burst is **disabled** (the icon is crossed out) while:
- you're being thrown (until you've recovered);
- you were hit by a Burst (until you've recovered);
- the opponent's super flash has you frozen;
- you've been knocked out.

## Burst gauge
The round icon under each portrait. It fills clockwise with a 0–9 number (tens of percent) in the middle, turns orange with "BURST" when full, and gets a red X when Burst is disabled.
| Rule | Value |
|---|---|
| Gauge size | 10,000 |
| Start | full at the start of the **match**; it **carries over between rounds** (Burst late in round 1 and you start round 2 low) |
| Passive refill, health ≥ 55 % | +5 per frame (≈ 33 s from empty) |
| Passive refill, 40 % < health < 55 % | +7.5 per frame (≈ 22 s) |
| Passive refill, health ≤ 40 % | +10 per frame (≈ 17 s) |
| Getting hit (not chip) | +100 + damage × 10 × (how much the combo has been scaled down) |

The passive refill runs at **half BBCF speed** (`PassiveSpeedPercent` = 50: BBCF's +10 / +15 / +20 per frame, halved) and keeps running during hitstop. The hit formula is BBCF's: the first hit of a combo (no scaling yet) gives 100, and long, heavily scaled combos give back much more. Set `GainMode` to **Simple** for 100 + damage × 10 per hit, or **PassiveOnly** for no gain from hits.

**Changing the refill speed:** open `Fighters/Shared/Burst/BurstSettings.tres` in the Inspector.
- **Passive refill:** `PassiveSpeedPercent`. Now 50 (half BBCF): about 33 s from empty at full health. 25 = about 67 s, 100 (BBCF) = about 17 s. The per-health-band amounts are `HighHealthGain` / `MidHealthGain` / `LowHealthGain` (10 / 15 / 20 before the percent).
- **Gain from getting hit** is separate: `GainMode`, `HitGainBase`, `HitGainDamageMultiplier`.

## How it works (flow)
```
in a hit reaction (not knocked down / thrown), gauge full, A+B+C+D held
   ─► 18 Burst: gauge → 0, face the opponent, stop in place, burst VFX
        frames 0–30: no hurtbox (invincible)
        frames 5–10: big MID hitbox around the burster (blockable standing, crouching, in the air, by barrier)
        frame 11+: falls if in the air
        ends (55 frames) ─► 0 Idle (on the ground) / 6 Falling (in the air)
hit by it ─► 24 Hit_Air (KNOCKDOWN: no air tech) ─► lands ─► 26 Hit_Down (soft knockdown: ground tech OK); can't Burst back until recovered
```

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/Burst/BurstSettings.tres` | **the numbers** (gauge, refill, gain mode): edit in the Inspector |
| `Scripts/FunctionFighter/Mechanics/Burst.cs` | gauge, refill, gain from hits, locks, and the saved per-fighter state |
| `Scripts/FunctionFighter/Mechanics/BurstSettings.cs` | the settings resource (what each field means) |
| `Scripts/FunctionFighter/Mechanics/BurstReadyCondition.cs` | condition on the Burst move: full gauge, getting hit, not disabled |
| `Scripts/FunctionFighter/Mechanics/BurstSpendAction.cs` | Burst state OnEnter: empties the gauge, plays the VFX |
| `Scripts/FunctionFighter/Mechanics/BurstLockOpponentAction.cs` | Burst state OnHitConfirm: the opponent can't Burst back |
| `Scripts/FunctionFighter/UI/BurstGaugeHUD.cs` | the round gauge icon |
| `Fighters/Dummy/Universal/States/18_Burst.tres` + `AnimationData/18_Burst_AnimationData.tres` | the Burst state (timing, invincibility, hitbox) |
| `Fighters/Dummy/Hitboxes/9_Hitbox_Burst_Active.tres` | the Burst hitbox (size, launch, blockstun) |
| `Fighters/Shared/Universal/Moves/18_Burst.tres` + `Fighters/Shared/Inputs/33_Input_ABCD.tres` | the move (A+B+C+D held, priority 10) |
| `Fighters/Shared/VFX/VFX_06.tscn` | the burst explosion (entry 6, `Burst`, in `Shared_VFXList.tres`) |
| `Scenes/Instances/Sakuga_UI.tscn` → `HealthBars/P1Burst`, `P2Burst` | the gauge icons |

## Engine patches (search for `[FF PATCH: burst]`)
| File | What it changes |
|---|---|
| `SakugaActor.cs` | stores the Burst state; fills the gauge at match start only (not each round); refills it every frame, even in hitstop |
| `GameState.cs` | saves/loads the Burst state for rollback (key 17; `PassiveRemainder` keeps the half-speed fractions) |
| `SakugaParameters.cs` → `TakeDamage` | gauge gain when taking a hit (skipped for chip) |
| `HealthHUD.cs` | updates the gauge icons |
| `MoveSettings.cs`, `StanceManager.cs` → `CheckAllowedStateType` | `AcceptHardKnockdownHits`: the Burst move also works during hard knockdown hits (wall bounce...). Upstream only allowed moves in hits below HARD_KNOCKDOWN |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Gauge size | `BurstSettings.tres` | `MaxValue` | 10000 |
| **How fast the gauge comes back** | `BurstSettings.tres` | `PassiveSpeedPercent` (100 = BBCF, 50 = half, 25 = quarter) | 50 |
| Passive refill per health band | `BurstSettings.tres` | `High/Mid/LowHealthGain`, `High/LowHealthPercent` | 10 / 15 / 20 (before the speed %); 55 % / 40 % |
| Gain from hits | `BurstSettings.tres` | `GainMode`, `HitGainBase`, `HitGainDamageMultiplier` | BBCF, 100, 10 |
| Burst VFX | `BurstSettings.tres` | `BurstVFXIndex`, `BurstVFXHeight` | 6, 9000 |
| Startup / active / recovery | `18_Burst_AnimationData.tres` | hitbox `AtFrame` 5 → 11, `Duration` | 6 / 6 / 43 (55 total) |
| Invincibility | `18_Burst_AnimationData.tres` | the hurtbox comes back at `AtFrame` | 31 |
| Reach | `9_Hitbox_Burst_Active.tres` | `Center` / `Size` | (0, 9000) / (36000, 26000) |
| Launch distance / height | `9_Hitbox_Burst_Active.tres` | `Ground/AirHitKnockback`, `…KnockbackTime` | (−110000, 70000), 10 |
| Blockstun / block push | `9_Hitbox_Burst_Active.tres` | `Ground/AirBlockstun`, `…BlockKnockback` | 25, (−80000, 0) |
| Input | `33_Input_ABCD.tres` | `Buttons` | 15 (A+B+C+D, held) |
| Gauge icon colours | `Sakuga_UI.tscn` → `P1Burst` / `P2Burst` | `FillColor`, `FullColor`, `DisabledColor` | blue / orange / red |

## Adding it to another fighter
Stamp the fighter: state 18 and the Burst move come with the universal layout. Give the fighter its own Burst animation via the stamp tool's animation names (`Burst`).

## Known issues / gotchas
- Pressing A+B+C+D in neutral does a throw (B+C is in there). There's no Overdrive to put on that input, by design for now.
- Burst doesn't yet ignore super armor (no fighter has armor yet).
- Training Settings → **Burst: Infinite** keeps the gauge full, **Refill After Combo** refills it after each exchange. F11 (reset positions) doesn't refill it.

## Test result
All Burst checks pass (2026-10-12), on the Dummy and on a freshly stamped fighter:
| Check | Result |
|---|---|
| A+B+C+D in hitstun | Burst comes out, gauge emptied, opponent launched → Hit_Down |
| Gauge at 90 % | no Burst |
| A+B+C+D in neutral | no Burst |
| Attack during Burst startup | whiffs (invincible) |
| Opponent blocks / barriers | Burst blocked |
| Being thrown | can't Burst |
| Hit by a Burst | can't Burst back |
| Round reset | gauge keeps its value |
| Passive refill at 100 % / 50 % / 30 % health | +10 / +15 / +20 every 2 frames (half speed) |
| Gauge from a 400-damage hit, fresh / at 50 % scaling | +100 / +2,100 |

## Change log
| Date | Change |
|---|---|
| 2026-10-18 | Fixed: couldn't Burst during a wall bounce (hard knockdown hits blocked every move). Burst move: `AcceptHardKnockdownHits` on, `IgnoreStates` 26, 27. No Burst once KO'd |
| 2026-10-12 | Passive refill halved (`PassiveSpeedPercent` 50) |
| 2026-10-12 | Created |
