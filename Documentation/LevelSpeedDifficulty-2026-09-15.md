# Level Speed Difficulty — capped progression

Current revision, superseding the earlier starting-value/interpolation design. Verified in Unity 6000.0.67f1 on 2026-09-15.

## Where to configure

Scene: Assets/Scenes/SkateRunner.unity.
GameObject: Managers/LevelManager.
Component: SkateAssassinRunnerLevelManager.
Section: Level Speed Difficulty → Level Speed Windows.
Shared prefab: Assets/Prefabs/Characters/LevelManager.prefab.

The scene inherits all six entries. Edit the prefab for shared defaults; scene edits become overrides.

Inspector cleanup: the normal Skate Runner Inspector now hides the inherited edit-time InitialSpeed and MaximumSpeed inputs. Configure speed through Level Speed Windows instead. Speed Acceleration remains editable. In Play Mode, a read-only Runtime Speed (Calculated) section shows the resolved values. The underlying fields and serialized fallback data are preserved for runtime compatibility; no gameplay or save behavior changed. This presentation is implemented only in Assets/Editor/SkateAssassinRunnerLevelManagerEditor.cs (plus its Unity-generated .meta), excluded from player builds.

## Fields and saved defaults

Name is optional. Start Level and End Level are inclusive. Initial Speed Limit and Maximum Speed Limit are per-range ceilings. Their Increase Per Level fields control campaign increases, not within-run acceleration. There are no Starting Initial Speed / Starting Maximum Speed fields.

| Range | Initial limit | Initial increase/level | Maximum limit | Maximum increase/level |
|---|---:|---:|---:|---:|
| Tutorial 1–5 | 10 | 0 | 15 | 0 |
| Early Game 6–20 | 15 | 0.5 | 20 | 0.5 |
| Early-Mid Game 21–80 | 15 | 0 | 25 | 0.25 |
| Mid Game 81–300 | 15 | 0 | 30 | 0.25 |
| Late Game 301–500 | 15 | 0 | 35 | 0.25 |
| End Game 501–1000 | 15 | 0 | 40 | 0.25 |

## Calculation

The first range uses its limits immediately (10 / 15). Later ranges use the previous distinct range's limits as the baseline:
`min(currentLimit, previousLimit + rate * (clampedLevel - StartLevel + 1))`.

The first level of a new range already includes one increment. Previous limits are targets, not actual reached endpoints: if a designer shortens a range or lowers its rate enough to miss its target, the next range still uses that target as baseline.

| Level | Initial | Maximum |
|---|---:|---:|
| 1–5 | 10 | 15 |
| 6 | 10.5 | 15.5 |
| 7 | 11 | 16 |
| 14 | 14.5 | 19.5 |
| 15–20 | 15 | 20 |
| 21 | 15 | 20.25 |
| 39 | 15 | 24.75 |
| 40–80 | 15 | 25 |
| 81 | 15 | 25.25 |
| 100–300 | 15 | 30 |
| 301 | 15 | 30.25 |
| 320–500 | 15 | 35 |
| 501 | 15 | 35.25 |
| 520–1000 | 15 | 40 |
| 1500 | 15 | 40 |

Non-divisible or oversized increments clamp exactly at the limit. Double intermediate arithmetic prevents float overflow before clamping. For inconsistent Initial/Maximum settings, Initial is reduced to resolved Maximum; Maximum is never raised past its limit. Lowered range limits clamp immediately. No hard-coded Initial cap of 15.

## Safety and integration

- Authoritative GameManager.LevelNum is evaluated before the existing startup Speed = InitialSpeed assignment.
- Global SpeedAcceleration remains 1, with its implementation unchanged.
- Beyond final range, hold its evaluated EndLevel values (defaults 15 / 40), without extrapolating.
- Below first range, use its limits. Invalid player levels evaluate level 1 with a warning.
- Gaps hold the nearest completed range's endpoint and warn.
- Overlaps use latest containing Start; equal starts choose earliest End, then ascending Initial limit, Maximum limit, Initial rate, Maximum rate, Name. Previous baseline uses latest strictly earlier Start with those same tie rules.
- Invalid ranges, null entries, negative/nonfinite limits/rates are skipped and warned.
- Empty/all-invalid configuration or missing GameManager retains existing Inspector speeds with a warning.
- Private sorted view; no serialized reordering, frame polling or per-frame logs.

This cap applies to the per-level InitialSpeed and MaximumSpeed configuration. The separate existing runtime Speed implementation remains unchanged: it permits a one-frame acceleration overshoot and temporary speed modifiers. Existing freeze-resume still restores acceleration to literal 1. The user's live Phase1DurationSeconds = 0 override is preserved.

## Verification for this revision

4,068 live-Editor assertions passed: 23 explicit boundary/plateau values; resolution and cap invariants for every level 1–1500; reversed order; non-divisible 0.7 increments; float.MaxValue rates; lowered caps; inconsistent Initial/Maximum; zero rates; editable Initial limits above 15; final endpoint holding with a low rate; gap, overlap, duplicate, below-first, negative, invalid/null/empty handling; 1,000 randomized configurations; actual scene's six entries and acceleration.

Final console: zero errors. Editor left out of Play Mode, scene clean.
All unrelated live scene and prefab serialized values/references matched pre-revision snapshots.
Scene SHA-256 unchanged: 789668CDCA39054E4D11151FA24EFCB10355546814D72827DA42C8A495215C70.
Vendor LevelManager source SHA-256 unchanged: 45A2C53286938779410DC0A9169B432769623D1B47C64E598E8B8E58101672CE.
These are Editor evaluations. Prior controlled gameplay tests are not claimed as rerun for this revision. No save/currency writes or Play Mode test run were needed.

## Files changed in this revision

- Assets/Scripts/LevelSpeedWindow.cs — limit fields, capped resolver and defaults.
- Assets/Scripts/SkateAssassinRunnerLevelManager.cs — Inspector tooltip; startup wiring unchanged.
- Assets/Prefabs/Characters/LevelManager.prefab — six revised serialized entries.
- Documentation/LevelSpeedDifficulty-2026-09-15.md — current handoff.

LevelSpeedWindow.cs.meta is unchanged. No scene/vendor source, runtime acceleration, phase, mission, audio, death/revive, temporary-modifier, freeze or save code changed. Previously archived Infinite Runner Engine source documentation and its index registration are unchanged.
