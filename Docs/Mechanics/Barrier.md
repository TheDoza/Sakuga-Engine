# Barrier

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** every fighter (engine logic + universal states 15–17, stamped automatically) · **Rollback-safe:** Yes (the gauge is saved with each fighter, all integer math; the shield is visual only)

## What it does
Hold **back + A+B** (or back + the **E** macro) to barrier (BBCF-style). It works **anytime you could act**, not only while blocking. **Once the barrier is up, A+B alone holds it:** you can let go of back and it stays up and still guards (standing, crouching with down held, or in the air), including through blockstun.
- **Neutral barrier:** back + A+B with nothing coming puts you in a barrier stance (standing, crouching with down-back, or in the air). A glowing shield appears in front of you and the gauge drains while you hold it. Let go of A+B or back and you're straight back to walking or crouching.
- **Barrier block:** if a hit lands while you're barriering (in the stance or holding A+B in blockstun), it:
  - takes **no chip damage**;
  - has the **same blockstun** as a normal block;
  - **doubles the pushback**: the extra distance is split between both fighters, and if one of them is cornered the other takes all of it;
  - flashes a cyan bubble on the blocker.
- Barrier also guards against **Burst**.

Standing barrier still loses to lows and crouching barrier to overheads, like normal blocking. Throws beat barrier.

## Barrier gauge (BBCF numbers)
The gauge sits under each health bar ("BARRIER"). It's blue when healthy, yellow below half, and dark red in Danger. Every fighter starts each round full.
| Rule | Value |
|---|---|
| Gauge size | 10,000 |
| Starting barrier | −100 instantly |
| Holding barrier | −10 per frame after the first (not during hitstop) |
| Barrier-blocking a hit | −(hit's raw damage ÷ 2), at most −500 |
| Refill | starts 124 frames after you let go, +16 every 10 frames (keeps running during hitstop) |
| Empty gauge | **Danger**: no barrier, you take **1.5×** damage. The gauge refills from empty to full over **900 frames** (15 s), then barrier is back. |

Pressing back+A+B in Danger does nothing (no A/B normal comes out).

**Changing the refill speed:** open `Fighters/Shared/Barrier/BarrierSettings.tres` in the Inspector.
- **Speed:** `RegenAmount` every `RegenInterval` frames. Now 16 every 10 frames (1.6 per frame), about 104 s from empty to full. Doubling `RegenAmount` to 32 halves that.
- **Wait before it starts:** `RegenDelay`, frames after you stop barriering (now 124).
- **Danger** refills on its own clock: `DangerFrames` (now 900 = 15 s from empty to full).

**Barrier Brake:** Barrier out of a run or skid keeps the run's speed (see Locomotion).

## How it works (flow)
```
back + A+B held (ground) ─► 15 Barrier_Stand ─(let go of back, A+B still held)─► stays up
back + A+B held (ground) ─► 15 Barrier_Stand ─(down-back)─► 16 Barrier_Crouch ─(down released)─► 15
back + A+B held (air)    ─► 17 Barrier_Air ─(lands, still holding)─► 15
let go / gauge empty     ─► 15 → 0 Idle (walk resumes) · 16 → 11 Crouching · 17 → 6 Falling
hit while barriering     ─► blockstun: chip 0, gauge −(damage/2, max 500), extra push for both fighters, cyan flash
```
- The barrier moves use an **A+B hold** input (`34_Input_AB_Hold`); their condition needs back too, unless the barrier is already up (`Held`: set when barrier is active, cleared when A+B is let go or the gauge runs out). So after a blockstun they kick in again as long as A+B is still held.
- A barrier that's up guards without back: `StanceManager.GetBlockState` picks the block for the fighter's current stance (`GuardOverride.cs`).
- Barrier states 15–17 are MOVEMENT states, not BLOCKING. Sakuga throws a fighter out of BLOCKING states as soon as no attack is near. Blocking still works from them because you're holding back.
- Extra push = the hit's normal block knockback (its `Ground/AirBlockKnockback` speed × knockback time). It's added over 8 frames: half to the defender, half to the attacker. If the defender's back is to the wall, the attacker gets all of it; if the attacker's is, the defender gets it all.
- The shield is drawn every rendered frame from the barrier state. It never touches the simulation, so rollback can't desync it.

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/Barrier/BarrierSettings.tres` | **the numbers** and the shield scene: edit this in the Inspector |
| `Fighters/Shared/Barrier/BarrierShield.tscn` + `BarrierShield.gdshader` | the shield (a magic circle drawn by a shader, no textures; colours/speed on the material) |
| `Scripts/FunctionFighter/Mechanics/Barrier.cs` | gauge, Danger, pushback, flash, and the saved per-fighter state |
| `Scripts/FunctionFighter/Mechanics/BarrierSettings.cs` | the settings resource (what each field means) |
| `Scripts/FunctionFighter/Mechanics/BarrierVisual.cs` | puts the shield in front of a barriering fighter |
| `Scripts/FunctionFighter/Mechanics/BarrierHeldCondition.cs` | condition: back + A+B held and barrier available (used by the moves and states) |
| `Scripts/FunctionFighter/Mechanics/BarrierStanceAction.cs` | action in the barrier states' OnTick: "I'm barriering" so the gauge drains |
| `Fighters/Dummy/Universal/States/15_Barrier_Stand`, `16_Barrier_Crouch`, `17_Barrier_Air` (+ AnimationData) | the neutral barrier stances |
| `Fighters/Shared/Universal/Moves/14_Barrier_Guard` (stand), `15_Barrier_Guard_Crouch`, `16_Barrier_Guard_Air` | hold back+A+B → states 15/16/17 (priority 8, crouch 9; only when barrier is available) |
| `Fighters/Shared/Universal/Moves/17_Barrier_Fizzle` | back+A+B with no gauge / in Danger does nothing instead of an A/B normal (priority 7) |
| `Fighters/Shared/Inputs/34_Input_AB_Hold` (moves 14–16), `32_Input_4AB_Press` (Fizzle) | the inputs (`30_Input_4AB` and `31_Input_1AB` are no longer used) |
| `Scripts/FunctionFighter/Mechanics/GuardOverride.cs` | guard without back while the barrier is up |
| `Fighters/Shared/VFX/VFX_05.tscn` | the barrier-block flash (entry 5, `BarrierFlash`, in `Shared_VFXList.tres`) |
| `HitNotifs.tres` | notification 9 "Danger" |
| `Scenes/Instances/Sakuga_UI.tscn` → `HealthBars/P1Barrier`, `P2Barrier`, `P1BarrierLabel`, `P2BarrierLabel`, `P1Danger`, `P2Danger` | the gauges, BARRIER labels and DANGER labels |

## Engine patches (search for `[FF PATCH`)
| Patch | File | What it changes |
|---|---|---|
| barrier | `SakugaActor.cs` | stores the barrier state; resets it each round; refills it every frame (even in hitstop) and drains it after hitstop; `BlockHit` asks Barrier whether this block is a barrier block (no chip + extra push); `Render` draws the shield |
| barrier | `GameState.cs` | saves/loads the barrier state for rollback (key 16) |
| barrier | `SakugaParameters.cs` → `TakeDamage` | Danger damage multiplier (every kind of damage: hits, chip, throws) |
| barrier | `HealthHUD.cs` | draws the gauge (blue / yellow / dark red) and the DANGER label |
| barrier | `StanceManager.cs` → `GetBlockState` | a barrier that's up guards without back (shared with the training dummy guard) |
| move conditions | `MoveSettings.cs`, `StanceManager.cs` | moves get an optional **Conditions** list (FrameDataConditions that must all be true). The barrier moves use it so they can't come out in Danger. |

## Tuning: "I want to change…" (all in `Fighters/Shared/Barrier/BarrierSettings.tres`)
| I want to… | Field | Current |
|---|---|---|
| Gauge size | `MaxValue` | 10000 |
| Cost to start barrier | `ActivationCost` | 100 |
| Drain while holding | `HoldDrainPerFrame` | 10 (≈ 16 s non-stop) |
| Cost per barrier-blocked hit | `HitCostPercentOfDamage` / `HitCostCap` | 50 % of raw damage / 500 |
| Refill delay / speed | `RegenDelay` / `RegenAmount` every `RegenInterval` | 124 frames / 16 every 10 frames (≈ 104 s empty → full) |
| Danger length | `DangerFrames` | 900 (15 s) |
| Danger damage | `DangerDamagePercent` | 150 |
| Extra pushback | `ExtraPushbackPercent` | 100 (= double) |
| How fast the extra push happens | `PushFrames` | 8 |
| Block flash / its height | `FlashVFXIndex` / `FlashHeight` | 5 / 9000 |
| Shield scene / position | `ShieldScene` / `ShieldOffset` | `BarrierShield.tscn` / 3,500 in front, 9,500 up |
| Shield position while crouching | `CrouchShieldOffset` | 3,500 in front, 6,000 up (used in any crouching state: crouch barrier, crouch block) |
| Shield look | `BarrierShield.tscn` → material | `tint`, `core_tint`, `strength`, `spin_speed`; quad size 1.45 |
| Gauge colours | `Sakuga_UI.tscn` → `GameHUD_Background` (HealthHUD) → Barrier group | blue / yellow below 50 % / dark red |
| Danger notification | `DangerNotificationIndex` | 9 (-1 = none) |

The buttons are A+B (`Barrier.ButtonsHeld`) and back is the same "back" as blocking.

## Adding it to another fighter
Stamp the fighter: states 15–17 and the barrier moves come with the universal layout. The gauge and shield need nothing per fighter.

## Known issues / gotchas
- The training-mode combo display shows a hit's damage before the Danger ×1.5 multiplier.
- Training has Barrier: Normal / Infinite / Refill After Combo, and Dummy Barrier: Normal / Infinite / Danger (see Training Mode).
- The DANGER label sits under the gauge, where training mode also draws its info panel.
- While barriering you can't attack or jump; let go of A+B first.

## Test result
All barrier checks pass (2026-10-12), on the Dummy and on a freshly stamped fighter. The pushback numbers come from the Dummy's Test Mid (B): 400 damage, block knockback 4,000.
| Check | Result |
|---|---|
| Normal block | 40 chip, gauge unchanged |
| Barrier block | 0 chip, gauge −100 (start) −200 (hit) −10/frame held; attacker pushed 2,000, defender 2,000 further than normal |
| Defender in the corner | attacker pushed the full 4,000 |
| Blockstun | identical to a normal block |
| Barrier for 10 frames | −100, then −10 per frame |
| Neutral barrier, nothing coming | Barrier_Stand, stands still, shield shows, gauge drains |
| Let go of A+B | back to walking |
| Down-back, then let go of down | Barrier_Crouch → Barrier_Stand |
| Neutral barrier vs mid / standing vs low / crouching vs low | blocked, no chip / hit / blocked, no chip |
| Air barrier, land still holding | Barrier_Air → Barrier_Stand |
| Gauge empties while barriering | Danger, stance ends |
| Danger: chip / hit | 40 → 60 / 400 → 600 |
| Danger after 450 frames | still Danger, gauge half full |
| Danger ends | when full |
| Refill | nothing for 124 frames, then +16 every 10 frames |
| Rollback save/load | barrier state restored exactly |
| Throw vs barrier | throw wins |
| Burst vs barrier | barrier guards it |
| Let go of back, keep A+B | stays in Barrier_Stand, still blocks a mid with no chip, back to the stance after blockstun |
| Crouch barrier, let go of back | blocks a low |
| A+B with no back (no barrier up) | no barrier (the A normal comes out) |

## Change log
| Date | Change |
|---|---|
| 2026-10-12 | Created (barrier while blocking) |
| 2026-10-12 | Neutral barrier: states 15–17, hold inputs, Barrier Fizzle move, move-conditions patch; extra push based on knockback only |
| 2026-10-12 | A+B alone keeps a barrier up (and guarding) after letting go of back; input 34 |
| 2026-10-12 | BBCF gauge numbers (100 to start, 10/frame, damage/2 per hit, 124-frame refill delay, 900-frame Danger); shield visual; BARRIER label and blue/yellow/red gauge; guards Burst |
| 2026-10-20 | Shield drops to `CrouchShieldOffset` in crouching states (crouch barrier no longer clips into the fighter) |
