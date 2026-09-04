# Ruthless Tap screen slash

## Shipped setup

`Assets/Scenes/SkateRunner.unity` contains one permanent `RuthlessTapSlashVolume` root with one global Volume (Default layer, priority 100, weight 1) and one `RuthlessTapSlashFeedback` component. Its dedicated profile is `Assets/Prefabs/VFX/RuthlessTapSlash/RuthlessTapSlashProfile.asset`; the profile contains only Fronkon's Slash override and is authored with intensity/progress zero, unscaled procedural time, and Scene-view rendering disabled.

One Slash Renderer Feature was added to each renderer used by gameplay quality settings:

- `Assets/Settings/Elroi_URP_Renderer.asset`: current desktop/Editor quality and the default pipeline.
- `Assets/Settings/Mobile_Renderer.asset`: Android's Level 6 quality.

Only one of these pipelines is active at a time, with exactly one Slash feature. Existing blur features were preserved. Render Graph was already enabled.

The scene's Main Camera already had post-processing enabled and includes the Default volume layer. The UICamera is an Overlay camera with post-processing disabled; the Main Camera's authored camera-stack list is empty. These existing camera settings were not changed. Slash renders through the gameplay camera, not an additional UI post-process pass.

The tap zone originates in `Assets/Prefabs/Characters/UICamera.prefab`. Its feedback reference is saved as a **scene instance override** on `UICamera/Canvas/MainActionButton`; the prefab itself was not otherwise changed.

## Runtime behavior

- The only gameplay trigger is immediately after `RuthlessTapCount++` inside the accepted `GameplayInputsLocked && RuthlessTapModeEntered` branch in `TapOnlyMainActionZone.OnPointerUp`.
- Pointer down, rejected taps, dragging/swipes and Phase 1 do not call the effect. Existing combo, mission, Cash, recoil and FOV code remains unchanged.
- Every trigger overwrites angle, elapsed time, progress and peak intensity on the same cached SlashVolume. There is no queue, pool, coroutine, per-tap object/material/profile creation, or second slash state.
- One private Volume profile is created once during initialization, leaving the authored asset untouched. Its owned resources are released on destruction. A duplicate-controller guard prevents another feedback controller from taking ownership.
- `LateUpdate` uses `Time.unscaledDeltaTime`, preserves full strength on the triggering render frame, and drives progress to completion. Fronkon's `useScaledTime` is also false.
- `StopImmediate` sets intensity and progress to zero. It runs on phase exit, tap-zone disable, feedback disable/destruction and application pause.
- Missing setup disables only feedback and warns once at initialization. Missing/destroyed serialized references do not prevent accepted taps, rewards or existing feedback.
- No recurring Editor callback is owned by the runtime feature. Idle Update exits immediately; Fronkon skips its render-graph blit when intensity is zero.

## Designer controls and defaults

Select `RuthlessTapSlashVolume` in the gameplay scene and edit **Ruthless Tap Slash Feedback**.

| Control | Default |
|---|---|
| Enabled | Yes |
| Duration | 0.18 real seconds; allowed 0.10–0.35 |
| Peak intensity | 0.85 |
| Fade starts | 40% of lifetime; SmoothStep to zero |
| Start progress | 0.03; the vendor shader's first fully revealed frame |
| Angle range / minimum separation | 0–360° / 35° |
| Visual impact scale | 1 |
| Split / distortion | 0.015 / 0.012 |
| Slash fade / core width / glow falloff | 0.85 / 0.007 / 60 |
| Glow RGBA | (1, 0.82, 0.68, 0.8), additive |
| Light smoke RGBA | (0.9, 0.9, 0.95, 0.12), additive |
| Dark smoke RGBA | (0.02, 0.02, 0.02, 0.1), darken |
| Background RGBA | (0, 0, 0, 1) |
| Smoke fade / expansion / widths | 0.55 / 0.12 / 0.10 and 0.16 |
| Brightness / contrast / gamma / hue / saturation | 0 / 1 / 1 / 0 / 1 |

Shape, colors, smoke and grading are grouped under **Advanced Visual Settings**. Impact scale multiplies core/smoke widths and expansion, and divides glow falloff because lower falloff makes the glow wider. Fronkon's parameter setters clamp results to their supported ranges. Widths stay positive to avoid degenerate shader smoothsteps.

Set smoke color alpha to zero to hide smoke. This reduces visual clutter, **not** the fixed noise-calculation cost of the unchanged vendor shader. Android GPU performance still needs measurement on the target device.

Angle selection tries up to eight random candidates using circular `Mathf.DeltaAngle` separation, then chooses a legal farthest endpoint or antipode. A separate allocation-free PRNG leaves Cash/recoil's Unity random stream untouched. For narrow designer-selected ranges, Inspector validation limits separation to half the range so every previous angle has a legal successor; a zero-width range is widened to 0.1°. No unbounded loop or normal fixed-angle sequence is used.

## Editor testing

In Play Mode, the component's custom Inspector offers:

- **TEST SINGLE SLASH**
- **TEST RAPID 10 TAP BURST** — 0.08 seconds between triggers
- **TEST RAPID 50 TAP BURST** — 0.04 seconds between triggers
- **STOP IMMEDIATELY**

These call only the visual API, not Ruthless gameplay, so there is no need to reach Phase 2. Burst tooling is Editor-only, runs one finite callback, never queues catch-up triggers, and unsubscribes on completion, Inspector disposal or Play Mode changes. No runtime cheat controls were added.

## Validation performed

Tests ran in an isolated temporary Play Mode scene, not by playing through a level. That scene and its metadata were deleted afterward; the original Play Mode start-scene setting was restored. Screenshots, test method bodies and JSON results are retained under ignored `Library/RuthlessTapSlashQA/`.

- Clean compilation; one feature in each applicable renderer; one authored Volume/override/controller; all scene references valid; no missing scripts.
- Immediate peak/progress reset, fading, explicit stop, disable/re-enable cleanup and unchanged authored profile verified.
- 10,000 random retriggers: smallest observed separation **35.01654°**, with no repeat. Deterministic fallback explicitly exercised.
- 1,000 warmed-up `TriggerSlash` calls: **0 managed bytes allocated** on the calling thread. This measures the new feedback hot path, not unrelated pre-existing combo/FOV behavior or total rendering cost.
- Actual Inspector burst controls: **10 triggers in 0.732 s**, **50 in 2.016 s**. Peak strength and angle separation held on every observed trigger. Test scene roots stayed **18 → 18** and the runtime profile identity stayed unchanged.
- Real lifetime at timeScale **0.1: 0.18091 s**; at timeScale **0: 0.18102 s**.
- Rendered 512×512 comparisons: desktop quality changed 36,919 pixels at peak; Android quality changed 37,518. Both returned to **0 residual changed pixels** after completion. Peak/fading screenshots were visually inspected. Android quality was tested in the Editor, not on a physical Android GPU.
- Fifty calls with each missing cached reference safely did nothing. Three missing-profile initialization attempts emitted exactly one expected warning. Recovery and forced-angle fallback passed.
- Scene diff is limited to the new Volume root/components, the scene root list, and the tap-zone serialized reference. No camera, unrelated Volume or gameplay state configuration changed.
- All **98 imported Fronkon files** matched their pre-task SHA-256 hashes. Shader, pass, SlashController, SlashVolume and demos were not modified. Ultimate Preview and unrelated Layer Lab code were also untouched.

The isolated QA scene initially lacked an AudioListener; one was added for testing and removed with that temporary scene. No production audio objects were added. The missing-profile warning above was intentional test coverage, not a recurring gameplay error.

## Manual acceptance remaining

Perform the final actual Ruthless Tap feel test: verify Phase 1, swipes/rejected taps and menus remain silent; accepted taps replace immediately; phase exit clears immediately; existing combo/Cash/mission/recoil/FOV/equip gameplay remains correct. Those input branches were structurally inspected, not driven through a full level. Tune colors/impact/smoke against actual gameplay and profile the full-screen shader on target Android hardware.

## File changes

Created runtime controller, Editor Inspector, this guide, and the dedicated profile (plus Unity metadata). Modified `TapOnlyMainActionZone.cs`, `SkateRunner.unity`, and the two renderer assets listed above. The tap source's pre-existing Windows-1252 comment was normalized to UTF-8; that encoding change has no gameplay effect. Pre-existing Easy Save defaults, vHierarchy data, ProjectSettings changes and imported vendor files were preserved.
