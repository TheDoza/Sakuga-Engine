# Sliding Knockdown

**Status:** ✅ Working (tested 2026-10-05) · **Fighters:** Dummy · **Added:** 2026-10-05 · **Rollback-safe:** Yes (data only, no C# changes)

## What it does
A hit sends the opponent sliding backwards along the floor. While sliding they can still be hit,
and they **count as airborne**, so the next hit uses the attacker's *air* hit settings (it launches them).
If nobody hits them, they stop, lie down, and get up.

## How it works (flow)
```
Any hit with GroundHitReaction = 15
        │
        ▼
 25 Hit_GroundSlide ──(hitstun over AND fully stopped)──► 26 Hit_Down ──(tech, or forced tech at 64f)──► 14 Neutral Tech / rolls ──► 0 Idle
        │                                                     ▲
        └──(hit again → uses AIR settings)──► 24 Hit_Air ──(lands)──┘
```
- **Only from hitstun:** the state is a HIT_REACTION that can only be entered by being hit.
- **Counts as airborne:** `BaseStance = 2 (ON_THE_GROUND)`. Sakuga treats that stance as airborne even on the floor.
- **Slides:** the hit's knockback pushes at full speed, then the state's `MoveX (Velocity 0, UseInertia)` slows it to a stop.
- **Ending:** waits for *both* hitstun to finish and speed to reach 0, so long combos can't cut the slide short. A safety exit fires at frame 90.

## Files (all in `Fighters/Dummy/`)
| File | What it is |
|---|---|
| `Universal/States/25_Hit_GroundSlide.tres` | **The slide state** |
| `Universal/AnimationData/25_Hit_GroundSlide_AnimationData.tres` | Slide length cap (90f) + lying hurtbox |
| `Hitboxes/1_Hitbox_Lying.tres` | Low hurtbox used while sliding |
| `Universal/States/24_Hit_Air.tres` | Hit while airborne (incl. during slide); lands into Hit_Down |
| `Universal/States/26_Hit_Down.tres` | Lying down: lying hurtbox (hittable, an OTG hit here is an INVALID combo); the tech / wakeup timeline runs here (see Tech Recovery & Wakeup) |
| `Universal/States/27_Hit_GetUp.tres` | Getting up, **no hurtbox**, then Idle |
| `Special/States/60_SlideKick.tres` | Test move state (COMBAT) |
| `Special/AnimationData/60_SlideKick_AnimationData.tres` | Test move timing: active frames 8–11, 30f total |
| `Hitboxes/2_Hitbox_SlideKick_Active.tres` | **Test move's hit settings (most tuning lives here)** |
| `Special/Moves/11_SlideKick.tres` | Test move input (**5C**) |
| `Stances/0_Dummy_Stance_Main.tres` | `HitReactions` slot 15 → state 9; Slide Kick added to `Moves` |
| `Dummy_Data.tres` | States 9–13 and the two hitboxes added to the lists |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Make the slide go farther / shorter | `2_Hitbox_SlideKick_Active` (attack box) | `GroundHitKnockback` X | -100000 |
| Make the full-speed part last longer | `2_Hitbox_SlideKick_Active` | `GroundHitKnockbackTime` | 8 |
| Make the slide last longer (frames stuck) | `2_Hitbox_SlideKick_Active` | `GroundHitstun` | 34 |
| Change how hard a mid-slide hit launches | `2_Hitbox_SlideKick_Active` | `AirHitKnockback` (X, Y) | (-40000, 70000) |
| Change mid-slide launch gravity | `2_Hitbox_SlideKick_Active` | `AirHitKnockbackGravity` | 230000 |
| Change damage | `2_Hitbox_SlideKick_Active` | `BaseDamage` | 600 |
| Make *any* move cause a slide | that move's hitbox | `GroundHitReaction` + `CrouchHitReaction` = 15, `GroundHitstunType` = 2 | — |
| Change the sliding hurtbox size | `Hitboxes/1_Hitbox_Lying` | `Center` / `Size` | (0, 2000) / (12000, 4000) |
| Change how long they stay down | `<Name>_Locomotion.tres` | `ForcedTechFrame` (soft) / `HardKnockdownFrames` (hard) | 64 / 30 |
| Change get-up time | `27_Hit_GetUp_AnimationData` | `Duration` | 20 |
| Make lying fighters invincible again | `26_Hit_Down_AnimationData` | `Hitboxes` → remove the lying hurtbox | lying hurtbox |
| Change the safety cutoff | `9_..._GroundSlide_AnimationData` | `Duration` | 90 |

**Measured result with current values:** about 31,000 units of travel over 33 frames. A re-hit at slide frame ~17 launched to about 9,300 height.

## Adding it to another fighter
It's part of the universal layout (state 25), so the stamp tool copies it. See [UniversalLayout.md](UniversalLayout.md).
Set the fighter's `LyingHurtbox` in its stamp config so the slide uses that fighter's lying hurtbox.

## Known issues / gotchas
- ⚠️ **Kaede has no slot 15** (reference only; not used in this project).
- **Ground throws can grab a sliding fighter.** The throw check only asks "on the ground?". Fix this when throws are added.
- **No visual yet.** The Dummy is a capsule and stays upright. Use **F10** (hitbox viewer) to see the low hurtbox.
- Slot 15 is a **project convention** we invented, not a built-in Sakuga slot. Keep the slot table in `README.md` updated.

## Change log
| Date | Change |
|---|---|
| 2026-10-05 | Created on Dummy with Slide Kick (5C) test move; tested in Godot 4.7 |
