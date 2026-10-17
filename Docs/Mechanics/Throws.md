# Throws & Throw Reject

**Status:** ✅ Working (tested 2026-10-11) · **Fighters:** Dummy (template — stamps onto new fighters) · **Rollback-safe:** Yes (data + C# logic that only reads/writes state the engine already saves)

## What it does
Press **B+C** next to the opponent to throw them. **B+C** or **6+B+C** is a forward throw, **4+B+C** is a back throw (the opponent lands behind you; with your back to the corner you switch sides and they end up in the corner), and **B+C in the air** is an air throw.
A throw is one hit for 1,500 damage that always ends in a **hard knockdown**. Nothing can combo out of a throw, and you can't throw someone who's in hitstun or blockstun, so nothing combos into one either.
The fighter being thrown gets a green **!** over their head and has **15 frames** to press **B+C** and reject it. On a reject both fighters are invincible for 28 frames and recover together (no advantage). The thrower is pushed back about half a screen and the defender stays put.

Modelled on BBCF's normal throws and its green throw reject. Not included: the purple throw (no reject window), throw combos, counter-hit throws.

## Frame data
| Throw | Input | Damage | Startup | Active | Recovery | Notes |
|---|---|---|---|---|---|---|
| Forward | B+C / 6+B+C (ground) | 1,500 | 7 | 3 | 23 | opponent lands in front of you |
| Back | 4+B+C (ground) | 1,500 | 7 | 3 | 23 | opponent lands behind you |
| Air | B+C (air) | 1,500 | 7 | 3 | 23 + 3 landing | only grabs airborne opponents no more than 1,500 units below your feet |

Damage ignores combo scaling (always 1,500) and can KO. A ground throw only grabs a grounded opponent; an air throw only grabs an airborne one.
On a hit the thrower recovers about 59 frames before the defender stands up (hard knockdown: no tech).

## How it works (flow)
```
Thrower:  30 Throw_Forward ─(throw box touches)─► 31 Throw_Forward_Hit ──────────────► 0 Idle
          (32/33 back, 34/35 air; air whiff that lands → 36 Throw_Air_Landing → 0)
Victim:   ──────────────────────────────────────► 37 Hit_Thrown ─(frame 10 of the hit state)─► 38 Hit_Thrown_Launch ─(lands)─► 26 Hit_Down ─► 27 Hit_GetUp ─► 0
                                                    │ green "!" for 15 frames, B+C pressed:
Reject:   victim  → 55 Throw_Reject_Ground / 56 Throw_Reject_Air    (stays put / small hop)
          thrower → 57 Throw_Rejected_Ground / 58 Throw_Rejected_Air (pushed back)
```
- When the throw box touches the opponent's hurtbox, the engine freezes both fighters (throw hitstop = 16 frames: the grab frame + the 15-frame reject window) and plays hit reaction **slot 14** (`37 Hit_Thrown`) on the victim.
- During that freeze, the victim's stance `ThrowEscapeInput` (B+C) triggers the reject. The victim plays its stance's `Ground/AirThrowEscapeState` (55/56) and the thrower plays its `Ground/AirThrowRejectedState` (57/58).
- If nobody rejects, the thrower's hit state (31/33/35) uses **ThrowPivotAction** to hold the victim in place, then on frame 10 plays hit reaction **slot 16** (`38 Hit_Thrown_Launch`). That state deals the 1,500 damage (`AnimationDamageAction`, hitstun type HARD_KNOCKDOWN) and launches the victim away from the thrower.
- **No combos while thrown:** 37 and 38 have no hurtbox, so nothing can hit the victim during the throw. Lying down afterwards (26 Hit_Down) is hittable (an OTG hit after a hard knockdown counts as a valid hit, not INVALID).
- **Throw trade:** if both fighters' throw boxes connect on the same frame, both play their Rejected state and bounce apart.

## Files
| File | What it is |
|---|---|
| `Fighters/Dummy/Universal/States/30_Throw_Forward.tres` … `36_Throw_Air_Landing.tres` | thrower states (attempt + hit sequence for each throw, air-throw landing) |
| `Fighters/Dummy/Universal/States/37_Hit_Thrown.tres`, `38_Hit_Thrown_Launch.tres` | victim states (held + "!", then damage + launch) |
| `Fighters/Dummy/Universal/States/55_Throw_Reject_Ground.tres` … `58_Throw_Rejected_Air.tres` | reject states (55/56 defender, 57/58 thrower) |
| `Fighters/Dummy/Universal/AnimationData/<same numbers>_AnimationData.tres` | timing + hurtbox / throw box per frame |
| `Fighters/Dummy/Hitboxes/7_Hitbox_Throw_Active.tres` | ground throw box (HitboxType 4, `GroundThrow`) + hurtbox |
| `Fighters/Dummy/Hitboxes/8_Hitbox_AirThrow_Active.tres` | air throw box (`AirThrow`) + hurtbox |
| `Fighters/Shared/Universal/Moves/11_Throw_Forward.tres`, `12_Throw_Back.tres`, `13_Throw_Air.tres` | the three throw moves (shared by every fighter) |
| `Fighters/Shared/Inputs/29_Input_4BC.tres` | back + B+C input (forward uses `22_Input_BC.tres`) |
| `Fighters/Shared/VFX/VFX_04.tscn` | the green "!" (entry 4, key `ThrowAlert`, in `Fighters/Shared/Shared_VFXList.tres`) |
| `Fighters/Dummy/Stances/0_Dummy_Stance_Main.tres` | `ThrowEscapeInput` = B+C, escape states 55/56, rejected states 57/58, hit reaction slots 14 → 37 and 16 → 38 |

## Engine patches this relies on (search the code for `[FF PATCH`)
| Patch | File | What it changes |
|---|---|---|
| no throw combos | `SakugaActor.cs` → `ThrowDamage` | a fighter in a HIT_REACTION state or in blockstun can't be thrown. Holding back with no blockstun is still throwable. |
| throw reject | `SakugaActor.cs` → `ThrowEscape` / `ThrowEscapeAction`, `FighterStance.cs` | new stance fields `GroundThrowRejectedState` / `AirThrowRejectedState` for the thrower. When they're set, all reject movement comes from the states' data. Left at -1, Sakuga's original behaviour is kept (both fighters play the escape state and get pushed). |
| move priority | `StanceManager.cs` → `CheckMoves` | when several moves match on the same frame, the highest `Priority` wins (throws 6–7 beat the B and C normals at 5). Before this, the move lowest in the list always won and Priority was ignored here. |
| throw corner | `ThrowPivotAction.cs` → `Execute` | if a throw would put the victim past the wall, the victim is placed at the wall and the thrower moves out by the same amount. A back throw with your back to the corner switches sides; a forward throw into the corner keeps the throw's spacing. |

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Throw damage | `38_Hit_Thrown_Launch.tres` → OnEnter → AnimationDamageAction | `Damage` | 1500 |
| Let throws scale in combos | same action | `UseProrations` | off |
| Throw range | `7_Hitbox_Throw_Active.tres` → 2nd box | `Center` / `Size` | (4000, 5000) / (4000, 8000): grabs up to ~17,000 apart |
| Air throw height limit | `8_Hitbox_AirThrow_Active.tres` → 2nd box | bottom edge = Center.Y − Size.Y/2 | 8,500 → reaches 1,500 below your feet |
| Reject window | `7_` and `8_Hitbox` → 2nd box | `ThrowHitstop` (= window + 1) | 16 |
| Startup / active / recovery | `30_`, `32_`, `34_..._AnimationData.tres` | throw box `AtFrame`, `Duration` | 6 / 9 / 33 |
| How far the victim flies | `38_Hit_Thrown_Launch.tres` → OnTick at frame 1 | MoveX / MoveY `Velocity` | -35000 / 55000 |
| Knockdown length | `<Name>_Locomotion.tres` (every hard knockdown) | `HardKnockdownFrames` | 30 |
| When the victim is released | `31_`, `33_`, `35_..._Hit.tres` → OnTick `AtFrame 10` | `Frame` | 10 |
| Reject push on the thrower | `57_Throw_Rejected_Ground.tres` → OnEnter MoveX | `Velocity` | -130000 (≈32,000 units) |
| Reject length / invincibility | `55_`–`58_..._AnimationData.tres` | `Duration` | 28 ground, 16 air |
| Air reject hop | `56_` / `58_` → OnEnter MoveY | `Velocity` | 35000 |
| The "!" look / size / height | `VFX_04.tscn` (Label3D) / `37_Hit_Thrown.tres` → SpawnVFXAction | `font_size`, `modulate` / `TargetPosition` | 96, green / (0, 18500) |
| Reject button | `0_Dummy_Stance_Main.tres` | `ThrowEscapeInput` | `22_Input_BC.tres` |

## Adding it to another fighter
1. Stamp the fighter with `UniversalStampConfig` (see [UniversalLayout.md](UniversalLayout.md)). States 30–38 and 55–58, the three throw moves, slots 14/16 and the throw-escape fields are copied automatically.
2. Make the fighter's own throw boxes: copy the Dummy's `7_` and `8_Hitbox_*` into its Hitboxes folder, resize the hurtbox and throw box, then set **Ground Throw Box** / **Air Throw Box** on its stamp config and re-stamp.
3. To change throw timing or damage for one fighter only, edit its own `Universal/States` files after stamping (re-stamping resets them).

## Known issues / gotchas
- **Back throw in the corner** switches sides (2026-10-14, `[FF PATCH: throw corner]`). Fighters have pushboxes now (see [Pushbox.md](Pushbox.md)); the throw range is still measured between the throw box and the hurtbox.
- **The "!" stays its full 16 frames** even if the throw is rejected early (Sakuga VFX can't be cancelled from a state).
- Pressing B+C exactly on the frame the throw connects (before the grab registers) counts as your own throw attempt, not a reject. The 15 reject frames start the frame after the grab.
- On a reject, Sakuga's built-in "Escaped" notification (hit notification 4) shows on the rejecting player's side.
- **Not done yet:** counter-hit throws (BBCF) aren't in the game, there's no throw invulnerability after blockstun, and Kaede still lacks slot 16. Wakeup rolls are throw invincible (see TechRecovery.md).
- Throw moves are shared files: editing `Fighters/Shared/Universal/Moves/11_`–`13_` changes every fighter.

## Change log
| Date | Change |
|---|---|
| 2026-10-11 | Created: forward/back/air throws, green reject, "!" indicator, 3 engine patches |
| 2026-10-12 | "!" raised to 18,500 so it sits above the Dummy's head |
| 2026-10-14 | Back throw with your back to the corner switches sides (throw corner patch) |
