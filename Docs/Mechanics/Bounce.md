# Wall & Ground Bounce

**Status:** ✅ Working (tested headless 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

A hitbox arms a bounce for `BounceTime` frames. Touching a wall or the floor while armed bounces the victim once.

- **Wall bounce (39 Hit_WallBounce):** from Hit_Air, Hit_Air_Weak or Hit_GroundBounce, touching a wall → X speed reversed × `BounceXIntensity`% → pop up (+50000) → lands into Hit_Down.
- **Ground bounce (40 Hit_GroundBounce):** falling onto the floor → Y speed reversed × `BounceYIntensity`% → lands into Hit_Down. Can still wall bounce.

| Test move | Input | What it does | Techable after? |
|---|---|---|---|
| 13 Test Wall Bounce (state 41) | 5A | launches (-150000, 30000); wall bounce armed 60f at 70% | no (HARD_KNOCKDOWN) |
| 14 Test Ground Bounce (state 42) | 5D | ground: pops up 70000; air: spikes -80000; ground bounce armed 90f at 80% | yes (KNOCKDOWN) |

| I want to… | Attack hitbox field |
|---|---|
| Arm a wall bounce | `BounceTime` > 0, `BounceXIntensity` > 0 |
| Arm a ground bounce | `BounceTime` > 0, `BounceYIntensity` > 0 |
| Stronger/weaker bounce | `BounceXIntensity` / `BounceYIntensity` (% of incoming speed) |
| Longer/shorter arm window | `BounceTime` |

Measured: Test Wall Bounce from 12,000 off the wall flew back about 45,000 units. Lower `BounceXIntensity` if too far.

| Date | Change |
|---|---|
| 2026-10-11 | Created; Hit_Air and Hit_Air_Weak check for bounces before landing |
