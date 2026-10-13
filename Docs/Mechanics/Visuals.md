# Visual Indicators (state flashes, advantage hitsparks)

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** all, every mode · **Rollback-safe:** Yes (visual only; nothing here changes the simulation)

## State flashes (Fantasy Strike style)
A tint + outline on the fighter's model:

| Color | When |
|---|---|
| **Red** | got Counter Hit (interrupted), for 24 frames. **Orange** = Big Counter, **gold** = Functional Counter |
| **Green** | parry: any state with the `PARRY` frame property (reserved; no parries yet. Barrier is not a parry and doesn't flash) |
| **Blue** | super armor up |
| **White** | strike invincible: `DAMAGE_IMUNITY` (backdash, rolls, techs, prejump-style invincibility) or an attack / locked state with no hurtbox (e.g. Burst) |

Colors, strength, outline width and the counter flash length are at the top of `Scripts/FunctionFighter/Mechanics/StateFlash.cs`. Training: Display → State Flashes.

## Advantage hitsparks
The hitspark itself tells you who recovers first. It replaces the old hit effect (the **block effect stays** and the spark plays on top of it, a little smaller on block). It's a white-hot core with a starburst of spikes, longer in the direction the hit was going, and embers thrown forward. **Color + shape** say who's ahead and **size** says by how much: ±1 frame is small, it grows evenly up to ±10 or more (3.3× the ±1 size); even (0) stays the normal size. Attacker's view:

| Spark | Meaning |
|---|---|
| **blue** burst with a double shockwave ring and a long cross | recover first by a lot (+4 or more), bigger the more plus |
| **light blue** burst with a ring and a cross | first by a little (+1 to +3), small |
| **yellow-orange** burst with a horizontal slash | same time (0) |
| **red** burst with an X | second by a little (−1 to −3), small |
| **dark red** X burst | second by a lot (−4 or less), bigger the more minus |

No numbers (the frame meter has those). It's always from the **attacker's** side: a move that's −1 on hit gives a small red spark even though it hit.

**How it knows on the impact frame:** advantage = (defender's stun + the frames after the stun before the defender can act) − the frames before the attacker can act. The stun is this hit's real stun (counter hits, combo decay included). The other two parts are learned from real exchanges, separately, so one move can't spoil another's spark:
- *Attacker recovery*, per move and per frame of the move it hit on (defaults to the frames left in the move's state).
- *Defender exit*, per reaction (Block_Stand_Exit 4 frames on block, 0 on a normal hit, the time lying down after a knockdown). Block exits are read from the universal exit states, so blocks are exact the first time.
Only **clean** exchanges teach it: if another hit lands, or the attacker cancels into another move before recovering, that sample is thrown away. (Before this fix a combo could teach it a wrong value, which is how a −1 hit showed a big blue spark.) Learned values are saved to `user://advantage_learned.cfg`; the key includes the move's length, but after big frame data changes you can delete that file (or call `Advantage.ResetLearning()`). Knockdown sparks depend on how the defender gets up (tech or not), so they show the last measured value: always a big plus unless they tech instantly.

All modes. In Training, Display → **Advantage Hitsparks** Off brings back the old hit effects. Look (colors, base size 62, `SmallScale` 0.5 at ±1 → `BigScale` 1.65 at ±`BigFrames` 10, lifetime 0.34 s): top of `Scripts/FunctionFighter/Mechanics/AdvantageHUD.cs`. Threshold: `Advantage.ALot` (4). If they don't look right, set the Training toggle Off to compare, or make `Advantage.SparksOn` return false to go back to the old hit effects everywhere.

## Engine patches
| File | What it changes |
|---|---|
| `SakugaActor.cs` (hit / block) | `[FF PATCH: advantage]` reports each contact (hit or block); the old hit effect is skipped while the advantage hitsparks are on |
| `GameManager.cs` | `[FF PATCH: advantage]` measures recovery after both fighters move (to learn the exact value) |
| `GlobalFlags.cs` | `PARRY` frame property (for future parry states) |

## Change log
| Date | Change |
|---|---|
| 2026-10-12 | Fixed wrong sparks after combos (clean-exchange learning, attacker / defender parts learned separately, saved between sessions); first-time blocks exact; big sparks smaller (BigScale 2.0 → 1.65) |
| 2026-10-12 | Spark size shows how much advantage (±1 small → ±10 big; even unchanged) |
| 2026-10-12 | Advantage icons became hitsparks: spawn on impact, replace the old hit effect, no numbers |
| 2026-10-12 | Created: state flashes, advantage icons |
