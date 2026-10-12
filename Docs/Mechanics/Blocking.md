# Blocking

**Status:** ✅ Working (tested headless 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (engine patch uses already-saved state)

## What it does
The Dummy blocks standing (hold back), crouching (hold down-back) and in the air (hold back).
Blockstun lasts the hitbox's `GroundBlockstun` / `AirBlockstun` frames (needs the engine patch below).

## How it works
- **Which block:** the stance's `BlockReactions` are checked in order: stand (input 4, blocks high + mid), crouch (input 1, blocks mid + low), air (input 4, blocks everything).
- **Proximity guard:** a `PROXIMITY_BLOCK` box (HitboxType 2) on the attacker puts a holding-back defender into Enter before the hit lands. Give it the same `HitType` as the attack, or crouch-blockers won't react.
- **Flow:** Enter (4f) → Blocking (hold) → on a blocked hit, Blockstun (8f animation) → Blocking until blockstun runs out and no proximity box touches → Exit (4f) → Neutral. Air blockers who land go to Blocking_Stand.
- **Guard break:** `GuardCrush = true` on a hitbox sends the blocker to GuardBreak for 40 frames. Not tested yet; no move uses it.

## Dummy states
| Stance | Enter | Blockstun | Exit | Blocking (hold) | Guard break |
|---|---|---|---|---|---|
| Stand | 14 | 15 | 16 | 17 | 18 |
| Crouch | 19 | 20 | 21 | 22 | 23 |
| Air | 24 | 25 | 26 | 27 | 28 |

Crouch block uses `Hitboxes/3_Hitbox_Crouch.tres`. No crouch idle yet, so crouch block exits to Neutral.

## Test moves
| Move | Input | Type | Blockstun | Chip | Block pushback |
|---|---|---|---|---|---|
| Test Mid (state 29) | 5B | MID | 14 | 40 | -40000 for 6f |
| Slide Kick (state 13) | 5C | LOW | 16 | 60 | -50000 for 8f |

Tested: stand block stops Test Mid; stand block loses to Slide Kick; crouch block stops both; air block stops Test Mid and lands into Blocking_Stand.

## Tuning: "I want to change…"
| I want to… | File | Field |
|---|---|---|
| Change blockstun for a move | that move's attack hitbox | `GroundBlockstun` / `AirBlockstun` |
| Change block pushback | that move's attack hitbox | `GroundBlockKnockback` / `AirBlockKnockback` (+ `…Time`) |
| Change chip damage | that move's attack hitbox | `ChipDamage` (`ChipDeath` = can chip kill) |
| Make a move overhead or low | attack hitbox **and** its proximity box | `HitType` (0 high, 1 mid, 2 low) |
| Change what each block stance stops | `Stances/0_Dummy_Stance_Main.tres` → `BlockReactions` | `BlockType` (1 high + 2 mid + 4 low) |
| Change block enter/exit time | `14/16/19/21/24/26_..._AnimationData` | `Duration` |

## Engine patch — `Scripts/SakugaEngine/Components/SakugaActor.cs`
Search for `[FF PATCH`.
| Patch | Upstream problem | Fix |
|---|---|---|
| blockstun | `PreTick` cleared blockstun every frame in BLOCKING states, so it ended with hitstop | Keep `BlockStun` until the hitstun timer runs out |
| crouch block | `BlockHit` used `IsGroundState()`, so crouch-blocking used AIR block values | Use `!IsAirState()` |
| block knockback Y | `BlockHit` passed X knockback as Y velocity | Pass `.Y` |

## Known issues
- Blockstun isn't shown in training-mode frame data yet.
- Exit adds 4 frames after blockstun where the blocker can't act.
- After any upstream Sakuga merge, re-check the three `[FF PATCH` lines.

## Change log
| Date | Change |
|---|---|
| 2026-10-11 | Stand/crouch/air block on Dummy; Test Mid (5B); proximity + block values on Slide Kick; blockstun engine patch |
