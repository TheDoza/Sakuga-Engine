# Tech Recovery & Wakeup

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** all (universal states 13, 14, 26, 27, 70, 71; numbers per fighter) · **Rollback-safe:** Yes (runs in `Locomotion`, saved with the fighter)

Modelled on BBCF. Frame 1 = the frame the fighter lands in **Hit_Down** (26).

## Knockdown timeline (soft knockdown)
| Frames | What can happen |
|---|---|
| **1–8** | **Emergency Tech**: A/B/C → Neutral Tech at once. A press up to **6 frames before landing** counts too, so you can press "as you hit the ground" without guessing |
| **9–23** | no actions (the attacker can keep hitting you; you could have escaped earlier) |
| **24–63** | wakeup options: **Neutral Tech** (A/B/C, no direction) · **Forward Roll** (6 + A/B/C) · **Back Roll** (4 + A/B/C) |
| **64** | forced Neutral Tech |

**Hold to tech:** holding A/B/C (plus a direction) techs on the first frame it's allowed, so you don't have to time it: in the air you Air Recover the moment the untechable part ends; holding through a landing is an Emergency Tech; holding from frame 9 on gives Neutral Tech / the roll (by direction) on frame 24.

There's no Quick Tech. **Emergency Tech is just Neutral Tech, available early**: same animation, same 32 invincible frames.

**Lying down is hittable** (BBCF): Hit_Down now has the lying hurtbox, so OTG hits connect (as air hits). Any hit on a fighter who could have teched (lying after a soft knockdown, or in the air after the untechable part) makes the combo **INVALID**: the combo counter turns red and shows INVALID with the hit number where it became invalid ("INVALID ⊘2"), in every mode. An OTG hit after a **hard** knockdown (throw, wall bounce) is a normal, valid hit. To make lying fighters invincible again, remove the hurtbox from `26_Hit_Down_AnimationData.tres`.

**Hard knockdowns** (every throw, wall bounce): no tech of any kind; the fighter gets up (Hit_GetUp) after 30 frames.

## Options
| Option | Input | Total | Details |
|---|---|---|---|
| Neutral Tech (state 14) | A/B/C | 32F | 1–32 fully invincible |
| Forward Roll (state 70) | 6 + A/B/C | 30F | 1–3 fully invincible · 1–17 throw invincible · 1–17 hit = air hit · 18–30 can guard, hit = crouching hit · 1–10 passes through the opponent |
| Back Roll (state 71) | 4 + A/B/C | 30F | 1–10 fully invincible · 1–17 throw invincible · same air / crouch rules · 1–10 passes through |
| Air Recovery (state 13) | 4 / 5 / 6 + A/B/C after the untechable part of an air hit | 15F | 1–15 fully invincible; drifts back / straight / forward |

From least to most risky: Neutral Tech > Back Roll > Forward Roll.

**Which hits are techable** is set on the attacker's hitbox (`GroundHitstunType` / `AirHitstunType`):

| Hitstun type | Air tech | Ground tech |
|---|---|---|
| 1 BASIC | yes, after the untechable part | — (no knockdown) |
| 2 KNOCKDOWN | no (always lands) | yes (the timeline above) |
| 3 HARD_KNOCKDOWN | no | no (gets up after 30 frames) |

## Knockdown test moves (Dummy)
Hold down + a button with P1 (keyboard: hold S).

| Move | State | What it tests |
|---|---|---|
| **2A** Test Launcher | 104 | air hit, air-techable after the untechable part |
| **2B** Test Air Knockdown | 105 | soft knockdown: Emergency Tech, rolls, Neutral Tech |
| **2C** Test Hard Sweep | 106 | low, hard knockdown: no tech, just gets up |
| **2D** Test Fast Knockdown | 107 | pops up and lands right away: wakeup / meaty practice |

In Training, the dummy can tech by itself (**Air Tech**, **Ground Tech**, **Tech Delay**) and reverse on wakeup: see Training.md.

## Files
| File | What it is |
|---|---|
| `Scripts/FunctionFighter/Mechanics/Locomotion.cs` | the knockdown timeline, Emergency Tech, forced tech, rolls, air recovery direction, invincibility |
| `Scripts/FunctionFighter/Mechanics/LocomotionSettings.cs` | the numbers (groups *Knockdown & wakeup*, *Neutral Tech*, *Wakeup rolls*, *Air Recovery*) |
| `Scripts/FunctionFighter/Mechanics/GroundTechCondition.cs` | move condition: wakeup window + direction |
| `Fighters/Shared/Universal/Moves/10_Ground_Recovery.tres`, `27_Forward_Roll.tres`, `28_Back_Roll.tres` | the wakeup moves (input 5X = A/B/C) |
| `Fighters/Dummy/Universal/States/70_Roll_Forward.tres`, `71_Roll_Back.tres` | the roll states |

## Engine patches (search `[FF PATCH: tech]` and `[FF PATCH: invalid combo]`)
| File | What it changes |
|---|---|
| `PhysicsBody.cs`, `PhysicsWorld.cs` | `IgnorePushbox`: a roll passes through the opponent |
| `SakugaActor.cs` → `HitDamage` | early roll frames are hit as airborne, late ones as crouching |
| `StanceManager.cs` → `CheckMoves` | hold-to-tech runs at the move-check point |
| `ComboCounter.cs` | the red INVALID label |

## Tuning (`<Name>_Locomotion.tres`)
| I want to… | Field (Dummy) |
|---|---|
| Emergency Tech window / buffer before landing | `EmergencyTechWindow` 8, `EmergencyTechBuffer` 6 |
| Longer "no actions" stretch | `WakeupLockUntil` 23 |
| Forced tech sooner / later | `ForcedTechFrame` 64 |
| Hard knockdown length | `HardKnockdownFrames` 30 |
| Neutral Tech | `NeutralTechFrames` 32, `NeutralTechInvulnFrames` 32 |
| Rolls | `RollFrames` 30, `RollMoveFrames` 20, `ForwardRollDistance` 22000, `BackRollDistance` 20000, `ForwardRollInvulnFrames` 3, `BackRollInvulnFrames` 10, `RollThrowInvulnFrames` 17, `RollGuardFrom` 18, `RollPassThroughFrames` 10 |
| Air Recovery | `AirTechFrames` 15, `AirTechInvulnFrames` 15, `AirTechForwardX` / `AirTechBackX` 15000 |

## Known issues / gotchas
- The Dummy has no roll or tech animations: rolls show the crouch pose. The stamp tool maps `Roll_Forward` / `Roll_Back` clip names for real characters.

## Test result
All 12 tech checks pass (2026-10-12): no input → lies to frame 64 then Neutral Tech; A pressed 4 frames before landing techs on frame 1; A on frame 6 techs; presses on 9–23 ignored, frame 27 works; forward roll passes through the opponent with the right invincibility, air/crouch hit rules and guard from 18; back roll moves away; throws can't be teched; air recovery forward is invincible and drifts forward; Training Emergency / Neutral +5 / back roll + wakeup reversal.

## Change log
| Date | Change |
|---|---|
| 2026-10-12 | Hold A/B/C to tech on the first possible frame |
| 2026-10-12 | Lying fighters are hittable (lying hurtbox on Hit_Down); INVALID combo shown on the combo counter |
| 2026-10-12 | BBCF knockdown timeline: Emergency Tech (with a pre-landing buffer), no-action frames, Neutral Tech 32F, forward / back rolls, forced tech at 64, directional air recovery 15F |
| 2026-10-11 | Created |
