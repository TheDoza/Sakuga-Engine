# Sliding Knockdown

**Status:** ✅ Working (tested headless 2026-10-05) · **Fighters:** Dummy · **Added:** 2026-10-05 · **Rollback-safe:** Yes (data only, no C# changes)

## What it does
A hit sends the opponent sliding backwards along the floor. While sliding they can still be hit,
and they **count as airborne**, so the next hit uses the attacker's *air* hit settings (it launches them).
If nobody hits them, they stop, lie down, and get up.

## How it works (flow)
```
Any hit with GroundHitReaction = 15
        │
        ▼
 9 Hit_GroundSlide ──(hitstun over AND fully stopped)──► 11 Hit_Down ──(30f)──► 12 Hit_GetUp ──(20f)──► 0 Neutral
        │                                                     ▲
        └──(hit again → uses AIR settings)──► 10 Hit_Air ──(lands)──┘
```
- **Only from hitstun:** the state is a HIT_REACTION that can only be entered by being hit.
- **Counts as airborne:** `BaseStance = 2 (ON_THE_GROUND)`. Sakuga treats that stance as airborne even on the floor.
- **Slides:** the hit's knockback pushes at full speed, then the state's `MoveX (Velocity 0, UseInertia)` slows it to a stop.
- **Ending:** waits for *both* hitstun to finish and speed to reach 0, so long combos can't cut the slide short. A safety exit fires at frame 90.

## Files (all in `Fighters/Dummy/`)
| File | What it is |
|---|---|
| `States/9_Dummy_Hit_GroundSlide.tres` | **The slide state** |
| `AnimationData/9_Dummy_Hit_GroundSlide_AnimationData.tres` | Slide length cap (90f) + lying hurtbox |
| `Hitboxes/1_Hitbox_Lying.tres` | Low hurtbox used while sliding |
| `States/10_Dummy_Hit_Air.tres` | Hit while airborne (incl. during slide); lands into Hit_Down |
| `States/11_Dummy_Hit_Down.tres` | Lying down, **no hurtbox** (can't be hit) |
| `States/12_Dummy_GetUp.tres` | Getting up, **no hurtbox**, then Neutral |
| `States/13_Dummy_SlideKick.tres` | Test move state (COMBAT) |
| `AnimationData/13_Dummy_SlideKick_AnimationData.tres` | Test move timing: active frames 8–11, 30f total |
| `Hitboxes/2_Hitbox_SlideKick_Active.tres` | **Test move's hit settings (most tuning lives here)** |
| `Moves/7_Dummy_SlideKick.tres` | Test move input (**5C**) |
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
| Change the sliding hurtbox size | `1_Hitbox_Lying` | `Center` / `Size` | (0, 2000) / (12000, 4000) |
| Change how long they stay down | `11_..._Hit_Down_AnimationData` | `Duration` | 30 |
| Change get-up time | `12_..._GetUp_AnimationData` | `Duration` | 20 |
| Make them hittable on the ground (OTG) | `11_..._Hit_Down_AnimationData` | `Hitboxes` → add `1_Hitbox_Lying` | none |
| Change the safety cutoff | `9_..._GroundSlide_AnimationData` | `Duration` | 90 |

**Measured result with current values:** about 31,000 units of travel over 33 frames. A re-hit at slide frame ~17 launched to about 9,300 height.

## Adding it to another fighter
1. Create that fighter's slide state: copy `9_Dummy_Hit_GroundSlide.tres`, point `AnimationData` at their own lying hurtbox + `Hit_Slide` animation, and set its `PlayStateAction StateIndex` to **their** Hit_Down state.
2. Add it to the end of their `States` list in `<Fighter>_Data.tres` and name the file with that position number.
3. In their stance, make `HitReactions` 16 long and set **slot 15** to the new state.
4. Make sure their air-hit slot (3) and down slot (9) point at working states.

## Known issues / gotchas
- ⚠️ **Kaede has no slot 15.** A slide hit on Kaede clamps to slot 14 = her **throw reaction**. Quick fix: add `25` (her `Hit_OnGround`, which already slides) as slot 15.
- **Ground throws can grab a sliding fighter.** The throw check only asks "on the ground?". Fix this when throws are added.
- **No visual yet.** The Dummy is a capsule and stays upright. Use **F10** (hitbox viewer) to see the low hurtbox.
- Slot 15 is a **project convention** we invented, not a built-in Sakuga slot. Keep the slot table in `README.md` updated.

## Change log
| Date | Change |
|---|---|
| 2026-10-05 | Created on Dummy with Slide Kick (5C) test move; tested headless in Godot 4.7 |
