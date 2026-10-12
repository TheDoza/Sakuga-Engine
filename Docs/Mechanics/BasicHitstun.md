# Basic Hitstun

**Status:** ✅ Working (tested headless 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

## What it does
Ordinary hits put the Dummy in hitstun for exactly the hitbox's `GroundHitstun` / `AirHitstun` frames,
then return it to Neutral (ground) or Falling (air).

## How it works
- **Ground hits (30, 31, 32):** 8-frame states with `HitstunHold = 2`. The state freezes on frame 2 while hitstun runs, then plays its last 6 frames, so it ends exactly when hitstun ends. Change hitstun on the move, not the state.
- **Weak air hit (33):** falls with the knockback's gravity. Landing → Neutral. After hitstun it keeps tumbling; any attack button air-techs (see TechRecovery.md).
- **Strong air hit (10):** always lands into Hit_Down, then gets up.
- The attacker's hitbox picks the slot: `GroundHitReaction`, `CrouchHitReaction`, `AirHitReaction`.

## Dummy states
| State | Slot | Ends | Hurtbox |
|---|---|---|---|
| 30 Hit_Stand_Weak | 0 | hitstun ends → Neutral | Basic |
| 31 Hit_Stand_Strong | 1 | hitstun ends → Neutral | Basic |
| 32 Hit_Crouch | 2 | hitstun ends → Neutral | Crouch |
| 33 Hit_Air_Weak | 3 | lands → Neutral, or air tech after hitstun | Basic |
| 10 Hit_Air | 4–7, 11, 12 | lands → Hit_Down | Basic |

## Test results
| Test | Result |
|---|---|
| Test Mid on standing Dummy | Hit_Stand_Strong for exactly 18 frames after hitstop, then Neutral |
| Test Mid on airborne Dummy | Hit_Air_Weak, then Falling when hitstun ends |
| Slide Kick re-hit during slide | Hit_Air → Hit_Down → Hit_GetUp → Neutral |

## Tuning: "I want to change…"
| I want to… | File | Field |
|---|---|---|
| How long a move stuns | that move's attack hitbox | `GroundHitstun` / `AirHitstun` |
| Weak vs strong reaction | that move's attack hitbox | `GroundHitReaction` (0/1), `AirHitReaction` (3 weak, 4 knockdown) |
| Make air hits always knock down | `Stances/0_Dummy_Stance_Main.tres` | `HitReactions` slot 3 → 10 |

## Known issues
- Hit_Crouch now returns to Crouching (see Crouching.md).
- Tech recovery: see TechRecovery.md.
- Slot 14 (throw reaction) still → Neutral.

## Change log
| Date | Change |
|---|---|
| 2026-10-11 | Hit_Air_Weak no longer drops to Falling at hitstun end; it tumbles until it lands or techs |
| 2026-10-11 | States 30–33; slots 0–3, 11–13 remapped; Slide Kick mid-slide re-hit uses slot 4 |
