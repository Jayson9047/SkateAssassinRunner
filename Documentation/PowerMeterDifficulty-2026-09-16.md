# Phase 2 Ticker Difficulty — 2026-09-16

Implemented and verified in Unity 6000.0.67f1. This is separate from Phase 1 runner-speed progression.

## Where to configure

Select Project asset **Assets/Prefabs/PowerMeterConfig.asset** (PowerMeterConfig ScriptableObject).
Expand **Ticker Difficulty Progression → Ticker Speed Windows**.
The gameplay scene's **UICamera/Canvas/PowerMeterUI**, **PowerMeter** component, already references this asset through its Config field; that reference was not changed.

## Fields

- Name: optional label.
- Start Level: inclusive first level; Starting Ticker Speed applies exactly here (zero increments).
- End Level: inclusive last level; evaluation never extrapolates past it.
- Starting Ticker Speed: baseline cycles per second at Start Level.
- Ticker Speed Increase Per Level: cycles per second added for each player level after Start Level, not acceleration during gameplay.
- Maximum Ticker Speed: hard cap within the window; hold the cap for the remaining levels after reaching it.

## Actual saved defaults

| Name | Levels | Starting | Increase per level | Maximum |
|---|---|---:|---:|---:|
| Tutorial | 1–5 | 0.20 | 0 | 0.20 |
| Early Game | 6–20 | 0.20 | 0.005 | 0.25 |
| Developing | 21–100 | 0.25 | 0.0025 | 0.35 |
| Mid Game | 101–300 | 0.35 | 0.001 | 0.45 |
| Late Game | 301–500 | 0.45 | 0.0005 | 0.50 |
| End Game | 501–1000 | 0.50 | 0.0002 | 0.55 |

Formula: min(StartingTickerSpeed + TickerSpeedIncreasePerLevel × (clampedLevel − StartLevel), MaximumTickerSpeed).

Example: level 21 = 0.25, level 41 ≈ 0.30, level 61 ≈ 0.35, levels 61–100 hold 0.35.
Level 128 = 0.377. Level 1500 holds the final window's capped EndLevel result, 0.55.

## Runtime and preservation

PowerMeter.StartMeter resolves authoritative SkateRunnerGameManager.SkateRunnerGameManagerAccessor.LevelNum each time it starts/retries. The resolved value lives only in that PowerMeter instance's nonserialized _resolvedTickerSpeed, readable through ResolvedTickerSpeed.

Update uses Time.deltaTime × _resolvedTickerSpeed. The rest of Update, including sine/ping-pong math, phase wrap and ticker positioning, is unchanged. There is no per-frame resolution, list sorting, logging, or new allocation.

The global serialized config.speed field is unchanged and still equals 0.2 on the actual asset. It remains the fallback. Runtime code never assigns config.speed or mutates progression/zone configuration. New unrelated config assets default to an empty list for backward compatibility; the requested six defaults were populated explicitly only on the actual game asset.

Preserved exactly:
- smoothMotion = true.
- randomizeStartPosition = true, including the existing phase/random position initialization.
- Scaled Time.deltaTime and interaction with slow motion.
- Red 0–1; Yellow 0.3–0.75; Green 0.425–0.625; Cyan 0.5–0.55.
- Evaluation priority: Cyan > Green > Yellow > Red.
- Actual scene padding = 3, layout refs, feedback/events.
- Phase 1 progression and all success/failure/tap/animation/audio/reward/save implementations.

[PowerMeterDifficulty] success log occurs once per StartMeter in Editor/development builds; it is excluded from non-development players. Invalid-configuration warnings occur only at resolution, not per frame.

## Edge cases

- Below first range: its starting speed, respecting its maximum.
- Above final range: its evaluated/capped endpoint, never perpetual extrapolation.
- Gaps: nearest completed valid window's final capped result; warning.
- Overlaps: latest containing Start Level wins. Ties: earliest End, ascending starting speed, rate, cap, then ordinal Name. Exact duplicates have identical behavior. Inspector ordering cannot change the result.
- Unsorted entries: private sorted copy only; serialized list unchanged.
- Null entries, Start < 1, End < Start, negative/nonfinite speeds or rates: skip with a combined warning. Negative player level evaluates level 1 with a warning.
- Maximum below Starting: clamp to Maximum and warn; never raise the cap.
- Empty/all-invalid list or missing GameManager: use config.speed exactly, with one warning.
- Huge finite rates: double intermediate and clamp before conversion prevent float overflow.
- Missing PowerMeter config retains the existing missing-config error/early return.

## Verification

**4,059 resolver assertions passed**, including every requested example:
1=.20, 5=.20, 6=.20, 10=.22, 16=.25, 20=.25, 21=.25, 41=.30, 61=.35, 100=.35, 101=.35, 151=.40, 201=.45, 300=.45, 301=.45, 401=.50, 500=.50, 501=.50, 751≈.55, 1000=.55, 1500=.55.

Also checked each level 1–1500, reordered data, endpoint holding before a cap is reached, gaps, duplicate/overlap ordering, invalid/null/empty settings, non-divisible and extreme increments, and 1,000 randomized cap cases.

**1,036 controlled Play Mode assertions passed**:
- StartMeter uses current LevelNum, caches the result, and does not re-resolve in Update.
- Restart re-resolves after an in-memory level change, then original level restored.
- Missing-manager and empty-list fallback use config.speed.
- 1,001 zone samples match the existing priority/ranges.
- Sine positions, top/bottom reversals, faster middle than ends, padded ticker placement.
- Deterministic seeded random-start behavior matches the existing code.
- Start, stop, and Red/Yellow/Green/Cyan result events; duplicate stop ignored.
- Shared asset serialization unchanged after all runtime tests.

Additional live-scene integration checks:
- Actual EnterPhase2BossHUD starts the meter at level 128 speed 0.377.
- GUI retry at temporarily set level 751 resolves 0.55; restore 128, retry resolves 0.377.
- One normal frame advances phase ≈0.00754. At timeScale 0.1 the measured ratio is 0.09999971; scaled timing is preserved.
- Existing Yellow/Green/Cyan result handlers receive the correct result, arm the existing launch, and preserve 3/4/5-second rewards.
- Red result enters execution-pending; execution/death handler removes the player, produces LifeLost, and LifeLostAction respawns one player. HUD retry resumes at 0.377. Presentation waits were bypassed for this controlled test.
- Existing RuthlessTapModeController Begin/Cancel still works.
- No full cinematic/ad-revive UI sequence or saved level completion was performed; success dispatch/arming, death/respawn and mode entry/cancel are the scope of these checks.
- No new compile/runtime errors in final console; Editor returned to edit mode, scene clean.
- Temporary startup-pause test hook and its .meta removed.

Actual config comparison: every pre-existing field unchanged; only tickerSpeedWindows added. Full configured asset serialization still matches after Play Mode. Existing PowerMeter serialized fields/events unchanged.

Unchanged file hashes:
- Phase 1 manager: 9A63191C5E52D89AEDD1012FE09A1A0545E9025546BBD7B98E941E73048077E3.
- Phase 1 window resolver: 6C9B8AFC8F14B093DA6E5DBEA8154E941730827D9501D55F680780B4AE238C22.
- LevelManager prefab: D870642F4F95D18E906E5CF91F50BD567D31F240009AA22F07215A871BC15FBA.
- Gameplay scene: B9F230C8EFFE6A514E59DC29792BBB0159A58ACED1032FC3B4B1E6CD529FB3B0.

## Files changed

1. Assets/Scripts/AccuracyUI/PowerMeter.cs — instance cache, StartMeter resolution, Update speed source, development log.
2. Assets/Scripts/AccuracyUI/PowerMeterConfig.cs — serialized list and read-only resolution entrypoint.
3. Assets/Scripts/AccuracyUI/PowerMeterSpeedWindow.cs — new independent window data and pure resolver.
4. Assets/Scripts/AccuracyUI/PowerMeterSpeedWindow.cs.meta — Unity-generated metadata.
5. Assets/Prefabs/PowerMeterConfig.asset — six actual defaults, no other values changed.
6. Documentation/PowerMeterDifficulty-2026-09-16.md — this handoff.

No unexpected implementation blocker was found. An existing standalone Phase 2 test scene was not found in the current project; tests instead paused gameplay at startup and exercised the meter and existing handlers directly, without traversing hazards or saving a completed level.

