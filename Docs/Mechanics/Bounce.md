# Wall & Ground Bounce

**Status:** ✅ Working (tested 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

A hitbox arms a bounce for `BounceTime` frames. Touching a wall or the floor while armed bounces the victim once.

- **Wall bounce (28 Hit_WallBounce):** from Hit_Air, Hit_Air_Weak or Hit_GroundBounce, touching a wall → X speed reversed × `BounceXIntensity`% → pop up (+50000) → lands into Hit_Down.
- **Ground bounce (29 Hit_GroundBounce):** falling onto the floor → Y speed reversed × `BounceYIntensity`% → lands into Hit_Down. Can still wall bounce.

| Test move | Input | What it does | Techable after? |
|---|---|---|---|
| Test Wall Bounce (state 62) | 5A | launches (-150000, 30000); wall bounce armed 60f at 70% | no (HARD_KNOCKDOWN) |
| Test Ground Bounce (state 63) | 5D | ground: pops up 70000; air: spikes -80000; ground bounce armed 90f at 80% | yes (KNOCKDOWN) |

| I want to… | Attack hitbox field |
|---|---|
| Arm a wall bounce | `BounceTime` > 0, `BounceXIntensity` > 0 |
| Arm a ground bounce | `BounceTime` > 0, `BounceYIntensity` > 0 |
| Stronger/weaker bounce | `BounceXIntensity` / `BounceYIntensity` (% of incoming speed) |
| Longer/shorter arm window | `BounceTime` |

Measured: Test Wall Bounce from 12,000 off the wall flew back about 45,000 units. Lower `BounceXIntensity` if too far.

## Wall stick
A hitbox with `WallStickFrames` > 0 arms a wall stick. If the victim reaches the stage wall (the corner, not the camera edge)
while still flying from that hit, they stop on the wall with no gravity for that many frames, then fall with normal gravity.
Hitstun is extended to the stick length + 30 so it can't be teched. Landing first, leaving hitstun or another hit clears it.
Code: `Scripts/FunctionFighter/Mechanics/WallStick.cs` (saved for rollback as SakugaActorState key 20).

Keep `BounceXIntensity` at 0 on a wall-stick hitbox, or the wall bounce happens instead.

| Move | Knockback | Stick |
|---|---|---|
| Basic Bro 22C~236C (Straight Punch) | (-90000, 25000), gravity 100000, Hit_KnockedBack | 20 frames |

| Date | Change |
|---|---|
| 2026-11-09 | Wall stick (`HitboxElement.WallStickFrames`) |
| 2026-10-11 | Created; Hit_Air and Hit_Air_Weak check for bounces before landing |
