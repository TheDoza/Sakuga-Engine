# Clashes

**Status:** ✅ Working (tested 2026-10-17) · **Fighters:** all (engine-level) · **Rollback-safe:** Yes (engine hit detection)

## What it does
When two attacks' hitboxes touch on the same frame and neither one also touches the other fighter's body (hurtbox), they **clash**: no damage, both fighters freeze for the clash hitstop (20 frames) and the clash effect plays, then both can cancel into most actions. Up close, where each hitbox also reaches the other fighter's body, the same attacks **trade** instead (both get hit).

With the Dummy's tightened hurtboxes (2026-10-17), two Test Mids (5B) clash from about 16,500 to 28,000 apart and trade closer than that. Before, the Dummy's wide hurtbox made them trade almost everywhere (clash only at 26,000–28,000).

## How it works
- `PhysicsWorld.HitboxesCheck`: hitbox ↔ hitbox with equal `Priority` = clash; hitbox ↔ hurtbox = hit. A hit found first wins (the hurtbox is the first box in each hitbox settings file).
- `SakugaActor.HitboxClash`: clash hitstop, clash effect, both fighters get the hit-cancel window.

## Tuning: "I want to change…"
| I want to… | File | Field | Current |
|---|---|---|---|
| Clash freeze length | the attack's hitbox (`HitboxElement`) | `ClashHitStopDuration` | 20 |
| Clash effect | same | `ClashEffectIndex` (Shared_VFXList) | 2 (Dummy), 3 (Kaede) |
| Make an attack beat another instead of clashing | both hitboxes | `Priority` (different values never clash) | 0 |
| More / fewer clashes | the fighters' hurtboxes (body) vs hitbox reach | `Center` / `Size` | Dummy body (0, 5000) / (5000, 10000) |

## Tests
FeatureTest `clash:` checks: two 5Bs meeting at 22,000 apart clash (both freeze, no damage); at 12,000 apart they trade.

## Change log
| Date | Change |
|---|---|
| 2026-10-17 | Documented; Dummy hurtboxes tightened so clashes happen whenever attacks meet in front of both fighters |
