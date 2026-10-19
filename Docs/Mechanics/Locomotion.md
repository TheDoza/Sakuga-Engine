# Locomotion (run, skid, step dash, backdash, jumps)

**Status:** ✅ Working (tested 2026-10-12) · **Fighters:** all (Dummy is the template; numbers per fighter) · **Rollback-safe:** Yes (`LocomotionState` is saved in `GameState`, key 19)

## What it does
| Action | Input | How it behaves |
|---|---|---|
| **Run** | 66, keep holding 6 (or 3) | Starts fast and speeds up. `6356` runs (the second 6 comes back from a roll), `6536` doesn't. |
| **Run Skid** | let go of 6 (or hold 4) while running | Slides to a stop. **Can't guard** during it, and **66 / 44 can't cancel it** (no chaining runs by mashing 6). Attacks, throws, jumps and Barrier (Barrier Brake) still cancel it. |
| **Barrier Brake** | Barrier (back + A+B) while running or skidding | Cancels the run/skid straight into Barrier and keeps the full speed, fading out over 20 frames (about 6,000–7,000 units at top speed). Pressing back first (skid) then A+B works too. |
| **Running momentum** | any attack, throw or Barrier out of a run, skid or step dash | Keeps the current speed, fading over `SlideFrames`. Getting hit, blocking, or a throw connecting ends the slide. A jump out of a skid keeps the skid's speed too. |
| **Step Dash** *(alternative to Run)* | 66 | A fixed-distance hop forward. Can't guard or cancel until `StepDashCancelFrame`. |
| **Backdash** | 44 | Fully invincible early, throw-invincible a bit longer, fixed distance, can't be cancelled, vulnerable recovery at the end. |
| **Jump** | 7 / 8 / 9 | 4-frame **prejump**: still on the ground, can't guard, throw and Burst invincible. Normal jumps **don't** turn around if you cross over the opponent. |
| **Dash jump** | jump out of a run or step dash | Carries `JumpMomentumPercent` of the run speed, so it goes farther. |
| **Super Jump** | 2, then 7 / 8 / 9 (within 10 frames) | Higher and faster. Turns to face the opponent in the air. Can still double jump or air dash. Faint blue afterimages trail the fighter. |
| **Double Jump** | 7 / 8 / 9 in the air (a fresh press) | Turns to face the opponent. Out of a normal jump, it goes whichever way you choose. |
| **Double jump after a dash jump** | | Keeps forward momentum whatever you press: same direction = 100%, straight up (8) = 60%, against the momentum = 25% (it still goes that way, just less far). After a 669 cross-up, **9** keeps full momentum, **7** (toward the opponent, against the momentum) gets the reduced amount. |
| **Air dash / air backdash** | 66 / 44 in the air | Uses the same air-action pool. White speed lines stream past the fighter. |

**Air actions:** the Dummy (and Basic Bro) have **one shared air action**: a double jump *or* an air dash. Future fighters can get more (see Tuning).

## Landing recovery (BBCF)
Landing from the air has recovery, in 2 kinds (`[FF PATCH: landing]`):

| Kind | What happens | Where it's set |
|---|---|---|
| **Standard** | For the first **5 frames** after landing (the first grounded frame is frame 1), forward / back walk, all jumps, super jumps and backdash are locked. Attacks, guarding, run and step dash work at once; doing any of them unlocks everything (land, run for 1F, then jump works). Getting hit ends it. | `<Name>_Locomotion.tres` → `LandingRecoveryFrames` 5 |
| **Special** (a move's **+XL**, e.g. Jin's air Hishouken 49+8L) | The fighter can't act **or guard** for X frames after landing. It overrides the standard one, still applies if the move already ended or was cancelled before landing, and is cleared by getting hit or replaced by another move that sets it. The frame meter shows it as recovery. | the air move: add **SetLandingRecoveryAction** (`Frames` = X) to its events, usually OnTick with an AtFrame condition for the frame it starts applying (Jin's j.236D: from frame 4). `Frames` 0 clears it (for a move with its own landing state). |

Runs and attacks buffered before landing come out on the landing frame.

**The frame before landing counts as grounded** (BBCF). On the last airborne frame of a jump or air move (next frame the fighter would touch the floor), a hit gives a **standing ground hit** (ground hitstun and knockback, the fighter is put on the floor) and **ground throws can grab** them. Air throws still work on that frame too. Blocking isn't changed (an air block stays an air block). Only fighters jumping / falling or doing an air attack count, not ones already in hitstun, blockstun or a locked state, so juggles are unchanged. `<Name>_Locomotion.tres` → `PreLandingFrameGrounded` (on).

## Step Dash instead of Run
A fighter has one or the other, never both. Run is the default (move slot 19).
To give a fighter the Step Dash: open its stance (`Stances/<Name>_Stance_Main.tres`) → **Moves** → slot **19** (`19_Run.tres`) → drag in `Fighters/Shared/Universal/Moves/19_StepDash_Alt.tres` instead. Nothing else changes; state 62 is already there for every fighter.

## Files
| File | What it is |
|---|---|
| `Scripts/FunctionFighter/Mechanics/Locomotion.cs` | everything above: modes, momentum, prejump, air-action pool, auto-facing |
| `Scripts/FunctionFighter/Mechanics/LocomotionSettings.cs` | the numbers (one resource per fighter) |
| `Scripts/FunctionFighter/Mechanics/LocomotionVisual.cs` | super jump afterimages and air dash speed lines (visual only) |
| `LocomotionAction.cs`, `DashInputCondition.cs`, `SuperJumpInputCondition.cs`, `AirActionCondition.cs` | the action / conditions the states and moves use |
| `Fighters/Dummy/Dummy_Locomotion.tres` | the Dummy's numbers (`Dummy_Data.tres` → **Locomotion**) |
| `Fighters/Dummy/Universal/States/60–69` | Run, Run_Skid, Step_Dash, Backdash, Super_Jump ×3, Double_Jump ×3 |
| `Fighters/Shared/Universal/Moves/19–26` | Run (or Step Dash), Backdash, Super Jump ×3, Double Jump ×3 |

## Engine patches (search for `[FF PATCH: locomotion]`)
| File | What it changes |
|---|---|
| `GlobalFlags.cs` | new frame properties `BURST_IMUNITY`, `NO_GUARD` |
| `SakugaActor.cs` | holds `Locomotion`, ticks it after the counter code, Burst can't hit `BURST_IMUNITY`, draws the visuals; `GraphicsNodes` getter |
| `StanceManager.cs` | `NO_GUARD` stops guarding; walking can't interrupt run / skid / dashes |
| `HitboxElement.cs` | `IsBurst` (ticked on `9_Hitbox_Burst_Active.tres`) |
| `DataContainer.cs` | the **Locomotion** slot |
| `GameState.cs` | saves `LocomotionState` (key 19) |
| `SakugaActor.cs` → `HitDamage`, `ThrowDamage` (`[FF PATCH: landing]`) | a fighter on its pre-landing frame (`Locomotion.LandsNextFrame`) is put on the floor and hit / grabbed as grounded |
| `StanceManager.cs` → `CheckMoves` (`[FF PATCH: landing]`) | landing recovery blocks locked moves |

## Tuning (`<Name>_Locomotion.tres`)
Velocities: 60,000 = 1,000 units per frame.

| I want to… | Field (Dummy value) |
|---|---|
| Faster / slower run | `RunStartSpeed` 30000, `RunAcceleration` 600, `RunMaxSpeed` 42000 (keep the start speed at or below the max: anything above it is cut to the max on the next frame) |
| Easier 66 / 44 | `DashInputWindow` 10 |
| Easier air dash (66 / 44 in the air) | `AirDashInputWindow` 6 (most frames each tap and the gap between may last; the first tap can be 9 / 7) |
| Longer skid | `SkidFrames` 14 |
| Landing recovery (standard) | `LandingRecoveryFrames` 5 |
| Frame before landing counts as grounded (hits / ground throws) | `PreLandingFrameGrounded` on |
| Air dash length | Not in the Locomotion file: each air dash state's **AnimationData → Duration** (Dummy: forward 20, back 16). States 7 and 8 have their own AnimationData files, so they can differ. |
| Let 66 / 44 cancel the skid (run again / backdash out of it) | `SkidDashCancel` off |
| Step dash | `StepDashFrames` 18, `StepDashDistance` 9000, `StepDashCancelFrame` 10 |
| Backdash | `BackdashFrames` 22, `BackdashDistance` 7500, `BackdashMoveFrames` 14, `BackdashInvulnFrames` 7, `BackdashThrowInvulnFrames` 12 |
| Jump | `PrejumpFrames` 4, `JumpVelocityY` 80000, `JumpForwardX` 18000, `JumpBackX` 12000 |
| Super jump | `SuperJumpVelocityY` 100000, `SuperJumpForwardX` 20000, `SuperJumpBackX` 14000, `SuperJumpInputWindow` 10 |
| Double jump | `DoubleJumpVelocityY` 68000, `DoubleJumpForwardX` 16000, `DoubleJumpBackX` 14000, `AirActionDelay` 4 |
| More air actions | `AirActions` 1 (pool), `MaxAirJumps` -1, `MaxAirDashes` -1 (-1 = only the pool limits it). **Air dash > double jump** (or the reverse): `AirActions` 2, `MaxAirJumps` 1, `MaxAirDashes` 1 |
| Back jump out of a run (how much run momentum a 7 keeps) | `BackJumpMomentumPercent` 0 (0 = a normal back jump; 100 = full forward carry, which cancels most of the back jump) |
| Dash jump momentum | `JumpMomentumPercent` 50, `DoubleJumpSameDirectionPercent` 100, `DoubleJumpNeutralPercent` 60, `DoubleJumpReversePercent` 25 |
| Running momentum slide (attacks, throws, Barrier) | `SlidePercent` 100, `SlideFrames` 20 |
| Afterimage / speed line look | `LocomotionVisual` → `AfterimageInterval`, `AfterimageFade`, `AfterimageColor`, `SpeedLineColor` |
| Movement dust / puffs on or off, run puff rate | `MovementEffects` on, `RunDustInterval` 12 (see Visuals.md → Movement effects) |

## Known issues / gotchas
- The Dummy has no run / dash / super jump clips yet: Run, Skid, Step Dash and Backdash show the Idle pose; super and double jumps show the Jump pose. The stamp tool already maps them to `Run`, `Run_Stop`, `Dash_Forward`, `Dash_Backwards` and the `Jump_*` clips for real characters.
- Movement states don't hold the move slot: anything you press (jump, attack, Barrier, air dash) can cancel a run or a super jump. The prejump, the backdash and the first part of the step dash can't be cancelled.
- **Ground walks and jumps read the stick relative to where the opponent is** (`SideCheck` = SIDE_RELATIVE on universal moves 0–4 and 21–23), because those states turn to face the opponent as they start. After a cross-up, still holding 9 (or 6) goes the way you hold: a back jump / walk back. (Before 2026-10-17 the move was picked with the old facing and then turned, so it went the opposite way.) Air moves (double jump, air dash) stay relative to the fighter's own facing.
- A Training recording that ends with forward held and is played back a few frames later can turn into a 66 (that's the real input rule).

## Test result
All 23 locomotion checks pass (2026-10-12): Barrier Brake from a run and from a skid, running throw slide, jump out of a skid, slide stops when hit; 66 runs and skids; 6356 runs, 6536 doesn't; the skid can't guard; Barrier Brake; running attack slide; prejump (4 grounded frames, throw/Burst invincible, can't guard); super jump goes higher; one shared air action; cross-up (normal jump keeps facing, double jump turns); dash-jump double jump momentum (9 full, 7 less, 8 drifts forward); dash jump goes farther; backdash invincibility; step dash in Run's slot; rollback save/load; afterimages and speed lines render.

## Change log
| Date | Change |
|---|---|
| 2026-10-18 | Movement effects (dust on jumps, landings, run, skid, step dash; puffs on air dash and double jump), per fighter `MovementEffects` / `RunDustInterval` |
| 2026-10-18 | Air dash input window is per fighter: `AirDashInputWindow` (6, same as before). Universal air dash moves now use `DashInputCondition` with **Air** ticked instead of the shared 66 / 44 input files |
| 2026-10-18 | Fixed: back jump out of a run didn't go back (the run's forward momentum outweighed it). New `BackJumpMomentumPercent` (0 = normal back jump) |
| 2026-10-17 | The frame before landing counts as grounded: ground hit reaction and ground throws (`PreLandingFrameGrounded`). Air dash back (state 8) has its own AnimationData (16F) and ends on Duration like the forward one |
| 2026-10-17 | Landing recovery: standard 5F (no walk / jump / backdash; attacks, guard, run allowed and unlock it) and special landing recovery per move (SetLandingRecoveryAction) |
| 2026-10-17 | Fixed: holding 9 / 6 after a cross-up went the opposite way (walk / jump / super jump moves now read input relative to the opponent) |
| 2026-10-17 | Run skid can no longer be cancelled by 66 / 44 (mashing 6 chained runs, worst with high run speeds); new `SkidDashCancel` setting to allow it per fighter |
| 2026-10-12 | Momentum: full speed fading over 20 frames; skid counts; throws slide; slide stops on hit / block |
| 2026-10-12 | Created: run, skid, Barrier Brake, step dash (alternative), backdash, prejump, super jump, double jump, momentum, air-action pool, visuals |
