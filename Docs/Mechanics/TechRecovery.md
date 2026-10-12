# Tech Recovery

**Status:** ✅ Working (tested headless 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

After a techable hit, any attack button (A, B or C) recovers. Hard knockdowns can't be teched.

| What | Move → state | Window | Invincible | Result |
|---|---|---|---|---|
| Air tech | 11 Air Recovery → 37 | Hit_Air_Weak after hitstun ends | first 10 of 16 frames | small hop → Falling, or Neutral on landing |
| Ground tech (quick rise) | 12 Ground Recovery → 38 | anywhere in 11 Hit_Down | first 10 of 14 frames | stands up → Neutral |

## Which hits are techable (attacker's `GroundHitstunType` / `AirHitstunType`)
| Hitstun type | Air tech | Ground tech |
|---|---|---|
| 1 BASIC | yes, after hitstun | — |
| 2 KNOCKDOWN | no (always lands) | yes |
| 3 HARD_KNOCKDOWN | no | no |

Engine rules this relies on: `IgnoreKnockdownHits` on the air tech move blocks it for KNOCKDOWN and up; Sakuga blocks every move from hit states during HARD_KNOCKDOWN; moves wait for hitstun to end (`IgnoreHitstun = false`).
Recovery states are LOCKED (nothing cancels them). Tune with the AnimationData `Duration` and the hurtbox `AtFrame = 10`.

| Date | Change |
|---|---|
| 2026-10-11 | Created |
