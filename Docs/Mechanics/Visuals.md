# Visual Indicators (state flashes, advantage hitsparks, punish / reversal)

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** all, every mode · **Rollback-safe:** Yes (visual only; nothing here changes the simulation)

## State flashes (Fantasy Strike style)
A tint + outline on the fighter's model:

| Color | When |
|---|---|
| **Red** | got Counter Hit (interrupted), for 24 frames. **Orange** = Big Counter, **gold** = Functional Counter |
| **Purple** | got Punished (hit during the recovery of a move), 24 frames |
| **Cyan** | did a Reversal (a move on the very first possible frame), 24 frames, on the fighter who reversed |
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

## Punish and Reversal indicators
Same style as the counter indicators: a notification on the player's side, a flash bubble and a state flash.

| Indicator | When | Shows |
|---|---|---|
| **Punish** | a hit lands while the opponent is in the **recovery** frames of a move (a blocked or whiffed attack, a whiffed throw…). A hit during startup / active is a Counter instead. | "Punish" notification on the attacker's side, purple bubble + purple flash on the punished fighter, Training Data → Counter: **Punish** |
| **Reversal** | a move starts on the **very first frame** the fighter can act after blockstun, hitstun, a knockdown / getup, a tech or a roll. Counts attacks, throws, jumps, super / double jumps, air dashes, run, step dash, backdash and barrier. | "Reversal" notification on that player's side, cyan bubble + cyan flash on the fighter who reversed |

- **Punish is visual only:** damage and hitstun don't change (a counter hit still gets its bonus).
- **Counter Hit Recovery** moves (see Counter.md): a hit in their recovery is a Counter. The red counter bubble and flash show (not the purple ones), and both the "Counter" and "Punish" notifications appear.
- **Reversal is strict:** one frame late doesn't count; neither does attacking from neutral, while just holding guard, or after a Burst. Getting up and standing still ends the chance. After a Neutral Tech, air recovery or roll the first possible frame is one frame after Idle appears (those states end from code), and that frame counts.
- All modes. Training: Display → **Punish Indicator** / **Reversal Indicator** turn each off. In Training the dummy's own reversal popup ("REVERSAL 5B Test Mid") still shows too.
- Rollback-safe: the "was in a reaction" flag is saved with the fighter (`CounterState` keys 10–12) and notifications are part of the match state.

| I want to change… | Where | Current |
|---|---|---|
| Notification text / which entry | `HitNotifs.tres` (2 "Punish", 13 "Reversal"); indexes in `CounterSettings.tres` → `PunishNotificationIndex` / `ReversalNotificationIndex` | 2 / 13 |
| Flash bubble | `Fighters/Shared/VFX/VFX_10.tscn` (punish) / `VFX_11.tscn` (reversal); indexes `PunishVFXIndex` / `ReversalVFXIndex` (-1 = none) | 10 / 11 |
| State flash colors | `StateFlash.cs` → `PunishColor` / `ReversalColor` | purple / cyan |
| Which states count as a reversal action | `Indicators.cs` → `ActionStates` (any COMBAT state always counts) | jumps, air dashes, barrier, run, step dash, backdash |

## Combo counter: damage and invalid hit
Under the combo counter, the **combo damage** (gold number, BBCF style). When the combo turns INVALID, the INVALID label sits on the same row with a **circle-slash icon and the hit number** where the combo became invalid ("INVALID ⊘2"). P1's row reads left to right from the left edge; P2's is mirrored toward the middle. All modes.

| I want to change… | Where | Current |
|---|---|---|
| Damage color / size | `ComboCounter.cs` → `DamageColor` / `DamageFontSize` | gold / 30 |
| Icon size / number size | `ComboCounter.cs` → `InvalidMark.Radius` / `InvalidMark.FontSize` | 11 / 26 |
| Gap from the screen edge | `ComboCounter.cs` → `P1Inset` | 22 |

The hit number is saved with the combo tracker (`CombatTracker.InvalidAtHit`, GameState key 11), so it's rollback-safe.

## Engine patches
| File | What it changes |
|---|---|
| `SakugaActor.cs` (hit / block) | `[FF PATCH: advantage]` reports each contact (hit or block); the old hit effect is skipped while the advantage hitsparks are on |
| `GameManager.cs` | `[FF PATCH: advantage]` measures recovery after both fighters move (to learn the exact value) |
| `GlobalFlags.cs` | `PARRY` frame property (for future parry states) |
| `SakugaActor.cs` (hit) | `[FF PATCH: indicators]` Sakuga's own "Punish" notification removed (Indicators shows it now, for any hit on a recovering opponent) |
| `GameManager.cs` | `[FF PATCH: indicators]` checks for reversals after both fighters move |
| `ComboCounter.cs`, `CombatTracker.cs`, `GameState.cs` | `[FF PATCH: combo display]` combo damage, the invalid hit number and its icon |

## Change log
| Date | Change |
|---|---|
| 2026-10-17 | Combo damage under the combo counter; "INVALID ⊘N" shows the hit where the combo became invalid. Counter Hit Recovery hits show red effects plus "Punish" |
| 2026-10-17 | Punish and Reversal indicators (notification, flash bubble, purple / cyan state flash, Training toggles) |
| 2026-10-12 | Fixed wrong sparks after combos (clean-exchange learning, attacker / defender parts learned separately, saved between sessions); first-time blocks exact; big sparks smaller (BigScale 2.0 → 1.65) |
| 2026-10-12 | Spark size shows how much advantage (±1 small → ±10 big; even unchanged) |
| 2026-10-12 | Advantage icons became hitsparks: spawn on impact, replace the old hit effect, no numbers |
| 2026-10-12 | Created: state flashes, advantage icons |
