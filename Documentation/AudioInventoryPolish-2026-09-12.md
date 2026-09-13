# Audio and Inventory polish

> Gameplay death/retry, Outro transitions and result-screen timing below were superseded by [Audio / Arcade polish](AudioArcadePolish-2026-09-13.md). The older music-boundary/wait APIs described here have been removed. Other inventory/UI work remains unchanged.

Project: Skate Assassin Runner / Skate Runner. Unity 6000.0.67f1.

The ELROI knowledge review found no matching registered audio/UI workflow and kept the implementation scoped to the existing project controllers. This is a project handoff report, not a promoted canonical ELROI knowledge entry.

## Configuration

Open `Assets/Resources/SkateRunnerAudio.prefab`, component `SkateRunnerAudioManager`.

- **Homepage Music 1 / 2:** Clip, Volume, Loop Enabled, and conditional Loop Count. Count is additional repeats: off or zero = once; 1 = twice; 2 = three times. Playback never uses infinite source looping for Homepage entries.
- **Homepage Music Gap Seconds:** default 7. After an entry's finite plays, wait the gap and advance to the next non-null entry. A sole valid entry rotates to itself. Music off cancels playback and freezes a pending gap; enabling resumes the gap or restarts the interrupted entry. There is one playlist coroutine owner.
- **Gameplay Music Tracks:** arbitrary list of Intro / Body / Outro / Volume packages. Intro plays once and Body is scheduled at the same DSP boundary, then loops. The selected package is retained through revive. New levels avoid immediate repetition when multiple packages are valid.
- **Ruthless Tap Pool Size:** default 16 preallocated, priority-32 sources. Each accepted tap in `TapOnlyMainActionZone.OnPointerUp()` makes one PlayOneShot request beside the actual combo increment and slash. Source wrap overlaps instead of restarting the shared pool's first source. Cue retrigger delay is intentionally not used for these taps. Sources ignore listener pause and have no Doppler pitch shift; configured pitch is never multiplied by timescale.
- **Gameplay Music Fade Out Duration:** fallback fade when no Outro exists.
- **Final Landing Wait Timeout:** default 15 seconds, guarding a missing grounded callback. An independent duration-based watchdog also protects an aborted audio routine.
- `Phase2PowerSlamFrameEvents` exposes **Level End Presentation Fallback Seconds**, default 6, for missing Outro / Music-off cases.

Music output is configured track volume × SoundManager.MusicVolume × the active fade/duck factor. Reward reveal restores the selected track volume when ducking ends. Existing SFX/music preferences and ES3 persistence are unchanged.

At final grounded landing, the current Body iteration finishes, then the selected Outro plays once. The result screen awaits `WaitForLevelEndingPresentation()`, backed by public ending state and `GameplayMusicEndingCompleted`. No-Outro and Music-off paths use the normal fallback delay. Success bookkeeping remains in its existing location. The grounded music handoff no longer requires the optional power-meter visual to exist.

## Click audio and Down Attack

The old implementation scanned only Button.onClick and added removable runtime listeners. MMTouchButton and UIClickToggle bypassed that path; other UI initialization could also clear the listeners.

`SkateRunnerUIClickAudio` now supplies the shared bridge. Authored Buttons have its persistent callback first in onClick, so it runs before a control closes/disables itself and survives RemoveAllListeners. MMTouchButton uses its accepted ButtonStateChange press event, independent of UnityEvent listeners. Mixed Button/MMTouch controls use only the touch route. UIClickToggle emits audio only when its activation targets actually change. EventSystem fallback supports runtime Buttons registered by existing callers. Scene registration includes inactive controls; there is no per-frame discovery.

The actual serialized `SkateRunnerGUIManager.DownslamButton` at `UICamera/Canvas/SlamHUD/SlamButton` has `UIClickAudioOptOut`. No hierarchy-name filtering is used during playback. Down Attack audio moved from DownAttackRoutine startup to the real impact callback, guarded by the existing per-attack impact flag. Damage, shockwave and FEEL behavior were not changed.

## Inventory Focus

Copied source: `StartScreenCanvas/Background/FullscreenPopupRoot/RewardsPage/Popup_RewardWeek/Group_Reward/Reward_Days/Day3_LIst/Focus`.

All 20 existing Inventory cards now contain an independent `EquippedFocus` copy referencing the exact source sliced sprite (GUID `02cde502a26be42d896a7719bd9cabfb`) and its authored color/material. The MessageBox subtree was removed from the copies only. They stretch around each card with a small margin, ignore layout and raycasts, and draw below card labels/badges. Rewards itself was not changed.

`InventoryEquippedCardVisual.SetEquipped()` controls both the existing yellow sprite and the copied Focus. Existing category controllers already refresh this from saved equipped state on enable and after equip. Preview selection does not drive Focus. No TMP text, font or material was changed.

## Ruthless diagnosis and live-input fix

The live gameplay scene uses `UICamera/Canvas/MainActionButton` with `TapOnlyMainActionZone`. It increments `LevelManager.RuthlessTapCount` and plays the slash without ever calling the old `RuthlessTapModeController.RegisterTap()`. The sound had been attached to that unused entry point. This was the cause of the continuing silence, not a slowed AudioSource pitch.

The sole source caller of RegisterTap was `RuthlessTapOverlay`; auditing Assets/Packages and serialized scene/prefab/asset references found no uses of that overlay. RegisterTap and the unreferenced overlay script/meta were removed. The mode controller retains timing/end-callback responsibilities and reads the real LevelManager count instead of maintaining a second, unused counter.

The cue now fires synchronously at the live tap-count increment. A pointer press is consumed once on release to prevent duplicate processing. Existing slash, combo, cash, and mission actions remain in place. The overlapping pool remains preallocated and explicitly ignores AudioListener.pause while preserving the existing SFX toggle and global/cue volumes.

Before/after verification used the actual authored gameplay tap component, with world movement frozen, the real SkateRunnerGameFeel slow-motion path set to 0.03, and cash/mission hooks disabled only in the temporary test. Before: 5 combo increments, 0 audio requests, output peak 0. After: 5 increments, 5 requests, output peak 0.8977661. Listener-paused hitstop also produced audio at pitch 1; rejected swipes and SFX-off taps produced no new requests. The earlier direct RegisterTap test did not exercise the live input path and was insufficient to validate this feature. The temporary live-input harness was removed afterward.

## Migration preservation

Captured the current live prefab before restructuring, staged old and new fields together, migrated through Unity's serialized API, compared every pre-existing field, and only then removed legacy fields. No existing field changed during the staged comparison.

| Assignment | Preserved GUID / destination |
| --- | --- |
| Homepage Music 1 | `d24a5dddd82c171498cabc56d9be5175` → Homepage Music 1 Clip |
| Homepage Music 2 | `16b8d6fdf5606944c8ec36fa21be00f0` → Homepage Music 2 Clip |
| Gameplay entry 0 | `ae7e74295b743cb4ea17c6a29d1549f1` → Gameplay Tracks[0].Body |
| Audio prefab | GUID remains `81b3b72109c25eb42982e48617f12810` |

New track volumes are 1. The former global Outro was unassigned; the migrated package's Intro/Outro remain unassigned. All unrelated SFX clip/volume/pitch/random-pitch/retrigger values are preserved. No audio files were removed or importer settings changed. Pre-existing user changes/deletions were left alone.

## Files changed

Modified scripts:

- `Assets/Scripts/Audio/SkateRunnerAudioManager.cs`
- `Assets/Scripts/TapOnlyMainActionZone.cs`
- `Assets/Scripts/RuthlessTapModeController.cs`
- `Assets/Scripts/Phase2PowerSlamFrameEvents.cs`
- `Assets/Scripts/SkateRunnerGUIManager.cs`
- `Assets/Scripts/SwipeDownDetector.cs`
- `Assets/Scripts/UI/UIClickToggle.cs`
- `Assets/Scripts/UI/Inventory/InventoryEquippedCardVisual.cs`

Removed unused runtime code: `Assets/Scripts/RuthlessTapOverlay.cs` and its meta file, after verifying there were no serialized references. Also removed the unused RegisterTap method and duplicate private counter from RuthlessTapModeController.

New scripts and their Unity-generated meta files:

- `Assets/Scripts/Audio/HomepageMusicEntry.cs`
- `Assets/Scripts/Audio/GameplayMusicTrack.cs`
- `Assets/Scripts/Audio/SkateRunnerUIClickAudio.cs`
- `Assets/Scripts/Audio/UIClickAudioOptOut.cs`
- `Assets/Editor/HomepageMusicEntryDrawer.cs`
- `Assets/Editor/SkateRunnerAudioPolishSetup.cs` — explicit, idempotent authoring helper; does not auto-run or enter player builds. The one-time migration method was removed after successful migration.

Serialized assets changed through Unity MCP:

- `Assets/Resources/SkateRunnerAudio.prefab`
- `Assets/Scenes/SkateRunnerStartScreen.unity` — 108 click bridges and 20 copied Focus frames.
- `Assets/Scenes/SkateRunner.unity` — 12 click bridges and explicit serialized Downslam opt-out.

## Verification and remaining manual checks

Unity compilation and Console error checks were run throughout. Controlled tests use in-memory short Intro/Body/Outro clips and the actual configured Ruthless clip on the Start Screen, avoiding hazards. No production test scene or progression bypass was added. Temporary verification source/components are removed at handoff.

Final result: **38 controlled assertions passed**, including the independent waiter watchdog. The temporary `Assets/Editor/AudioPolishVerification.cs` and its meta file were removed, compilation completed without errors, and Unity was left out of Play Mode. Full assertions are in [AudioInventoryPolish-TestResults.txt](AudioInventoryPolish-TestResults.txt).

Passed tests cover finite Homepage repeats, null clips, single-entry rotation, gap toggles, independent volume/duck restoration; Intro/Body DSP boundary, final Body loop termination, Outro completion releasing presentation, missing sections, failed Outro, Music-off fallback, missing landing timeout, selection retention and next-level anti-repeat; and rapid Ruthless playback/output/mute/slow-motion behavior.

Real Free Cash, Settings gear, and Spin-the-Wheel controls each emitted one central click. Every Shop tab emitted one on change and zero on immediate repeat; the real Inventory Abilities tab did likewise. Synthetic EventSystem tests covered normal, disabled, mixed Button/MMTouch, opt-out, RemoveAllListeners and a runtime Button closing itself.

Inventory screenshots were inspected for Swords, Abilities and Rollerblades. Equipped defaults showed the copied frame plus yellow background, with other cards unframed. The exact Rewards sprite remains shared, but copied objects are independently controlled.

Still manual: listen to the full Phase 2 sound mix on the target device; tune balance and the future authored Intro/Body/Outro seams; verify grounded slam sound perceptually against VFX during normal and buffered attacks; inspect the final real-level result-screen choreography with your chosen musical package. A production device/release build was not run.
