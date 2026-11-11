# Counter Crash (guard cancel)

**Status:** 🧪 Testing (added 2026-11-07) · **Fighters:** Basic Bro (state 80) · **Rollback-safe:** Yes (state data only)

## What it does
While blocking (in blockstun), press **6 + A+B** to cancel the block into a fully invincible attack that knocks the attacker away.

| | |
|---|---|
| Input | 6 + A+B during blockstun (A and B together, or one held and the other pressed). 3 / 9 count as 6 |
| Cost | 50% super meter (5000) |
| Frame data | 10 startup, 4 active, 19 recovery (32 frames total) |
| Invincibility | Strike, throw, projectile and Burst invincible from frame 1 through the last active frame. Recovery is not invincible |
| On hit | 300 damage, knocks the attacker far away into a soft knockdown. No combo afterwards: it resets the pressure |
| On block | −9 (unsafe) |
| Function Fakeout | Can't be Fakeout-cancelled |

It only comes out in blockstun: not while just holding back, not on hit, not in the air.

## Files
| File | What it is |
|---|---|
| `Fighters/Shared/Universal/Moves/29_Counter_Crash.tres` | the move (input, meter cost, blockstun condition). Shared by every fighter |
| `Fighters/Shared/Inputs/47_Input_6AB.tres` | the 6 + A+B input |
| `Scripts/FunctionFighter/Mechanics/CounterCrashAction.cs` | ends the blockstun when Counter Crash starts (put on the state's OnEnter) |
| `Fighters/BasicBro/Universal/States/80_Counter_Crash.tres` | Basic Bro's state: invincibility, friction, heavy streak |
| `Fighters/BasicBro/Universal/AnimationData/80_Counter_Crash_AnimationData.tres` | 5C animation at speed 2.45, hitbox on frames 10–13 |
| `Fighters/BasicBro/Moves/Hitboxes/BB_Hitbox_CounterCrash_Active.tres` | damage, knockback, blockstun |

## Values to change
| Change | Where |
|---|---|
| Meter cost | move → Super Gauge Required |
| Startup / active / recovery | AnimationData → hitbox AtFrame (active = 9, recovery = 13) and Duration; keep the state's invincibility window (MaxFrame 12, frame 13 off) in step |
| Damage, knock-away distance, block advantage | the hitbox (Base Damage, Ground/Air Hit Knockback, Ground/Air Blockstun) |

## Adding it to another fighter
1. Make that fighter's state 80 (copy Basic Bro's state and give it its own AnimationData and hitbox).
2. Add `29_Counter_Crash.tres` to the fighter's stance Moves list.

A fighter without a state 80 must not have the move in its stance.

## Change log
| Date | Change |
|---|---|
| 2026-11-07 | Created: Basic Bro (5C animation) |
