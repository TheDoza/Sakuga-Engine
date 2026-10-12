# Crouching

**Status:** ✅ Working (tested headless 2026-10-11) · **Fighters:** Dummy · **Rollback-safe:** Yes (data only)

Holding any down direction crouches; releasing stands up. Crouch hits and crouch blocks return to Crouching.

| State | What it is | Hurtbox |
|---|---|---|
| 34 Crouch_Enter | 4 frames → Crouching | Crouch |
| 35 Crouching | loops while down is held | Crouch |
| 36 Crouch_Exit | 4 frames → Neutral | Basic |

| Move | Input | Notes |
|---|---|---|
| 9 Crouch | hold 1, 2 or 3 | ignored while crouching or crouch-blocking |
| 10 Uncrouch | release down | only from Crouch_Enter / Crouching |

Changed: 21 Block_Crouch_Exit, 23 GuardBreak_Crouch, 32 Hit_Crouch → 35 Crouching (Uncrouch fires next frame if down isn't held).

| Date | Change |
|---|---|
| 2026-10-11 | Created |
