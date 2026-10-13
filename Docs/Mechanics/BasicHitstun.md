# Basic Hitstun

**Status:** ✅ Working (tested 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

## What it does
Ordinary hits put the Dummy in hitstun for exactly the hitbox's `GroundHitstun` / `AirHitstun` frames,
then return it to Idle (ground) or Falling (air).

## How it works
- **Ground hits (20, 21, 22):** 8-frame states with `HitstunHold = 2`. The state freezes on frame 2 while hitstun runs, then plays its last 6 frames, so it ends exactly when hitstun ends. Change hitstun on the move, not the state.
- **Weak air hit (23):** falls with the knockback's gravity. Landing → Idle. After hitstun it keeps tumbling; A/B/C + a direction air-techs (see TechRecovery.md).
- **Strong air hit (24):** always lands into Hit_Down (the knockdown / tech timeline, see TechRecovery.md).
- The attacker's hitbox picks the slot: `GroundHitReaction`, `CrouchHitReaction`, `AirHitReaction`.

## Dummy states
| State | Slot | Ends | Hurtbox |
|---|---|---|---|
| 20 Hit_Stand_Weak | 0 | hitstun ends → Idle | Basic |
| 21 Hit_Stand_Strong | 1 | hitstun ends → Idle | Basic |
| 22 Hit_Crouch | 2 | hitstun ends → Crouching | Crouch |
| 23 Hit_Air_Weak | 3 | lands → Idle, or air tech after hitstun | Basic |
| 24 Hit_Air | 4–7, 11, 12 | lands → Hit_Down | Basic |

## Test results
| Test | Result |
|---|---|
| Test Mid on standing Dummy | Hit_Stand_Strong for exactly 18 frames after hitstop, then Idle |
| Test Mid on airborne Dummy | Hit_Air_Weak, then Falling when hitstun ends |
| Slide Kick re-hit during slide | Hit_Air → Hit_Down → tech → Idle |

## Tuning: "I want to change…"
| I want to… | File | Field |
|---|---|---|
| How long a move stuns | that move's attack hitbox | `GroundHitstun` / `AirHitstun` |
| Weak vs strong reaction | that move's attack hitbox | `GroundHitReaction` (0/1), `AirHitReaction` (3 weak, 4 knockdown) |
| Make air hits always knock down | `Stances/0_Dummy_Stance_Main.tres` | `HitReactions` slot 3 → 24 |

## Known issues
- Training's frame meter shows hitstun in yellow; the data panel shows the hit's frame data and advantage.

## Change log
| Date | Change |
|---|---|
| 2026-10-11 | Hit_Air_Weak no longer drops to Falling at hitstun end; it tumbles until it lands or techs |
| 2026-10-11 | States 30–33; slots 0–3, 11–13 remapped; Slide Kick mid-slide re-hit uses slot 4 |
