# Arcade rank timing and Blood kill popups

Implemented September 13, 2026 in Skate Runner, Unity 6000.0.67f1, Damage Numbers Pro 4.51, Unity UI 2.0.0. This is a presentation delta to `AudioArcadePolish-2026-09-13.md`; no audio, rank thresholds, combat damage, or saved scene edits.

## Result and tuning

### Shared Phase 2 start delays (September 15 follow-up)

Two independent settings now delay the start of the three rank remarks after successful Ruthless Tap completion:

- Text: `Assets/Prefabs/Characters/UICamera.prefab` → `Canvas/ArcadeAnnouncements` → `ArcadeAnnouncerPresentation` → **Phase 2 Rank Timing / Rank Text Delay Seconds**.
- Voice: `Assets/Resources/SkateRunnerAudio.prefab` → `SkateRunnerAudioManager` → **Optional Arcade Announcers (SFX) / Rank Audio Delay Seconds**.

Each field applies to all three ranks, not to individual remarks. Both default to 0 seconds (immediate), use unscaled real-time waits, and count independently from the same success event. They do not change the text's fade/hold lifetime, Powerslam, Blood popups, music Outro, or result-screen timing. The captured final tap count selects the remark even after gameplay resets its counter. Pending remarks are replaced by a newer result and cancelled on disable, death or retry; pending audio is also cancelled on a single-scene load or when SFX is switched off. This is an ordinary extension of the existing separate GUI/audio listeners, not a new popup or audio system.

### Preset presentation

Delay verification: 23 controlled Play Mode assertions passed on September 15 with timeScale zero. Checked all rank counts 6/11/16 with text delay 0.25s and audio delay 0.65s; reversed order with text 0.65s/audio 0.2s; immediate zero-delay behavior; LifeLost/GameStart/disable cancellation; replacement by a newer result; and no remark below the rank threshold. A temporary in-memory clip exercised the existing `PlayOneShot` voice path; original cue clips were restored. The temporary Editor test script/meta was removed and Play Mode exited. No saved prefab or scene changes were required to expose the new zero-default fields.

| Presentation | Project-owned prefab | Timing |
| --- | --- | --- |
| KILLER ASSASSIN / BRUTAL!!! / RUTHLESS!!! | `Assets/Prefabs/UI/DN_ArcadeRank.prefab` | 0.3s fade-in, about 2s fully visible, 0.6s fade-out; no entrance scale pop or ongoing scale/lerp |
| POWERSLAM!!! | `Assets/Prefabs/UI/DN_ArcadeAnnouncement.prefab` | Existing 0.12s pop entrance, about 2s hold, existing 0.22s exit |
| Execution / Critical / Killed / Dead | `Assets/Prefabs/UI/DN_EnemyBloodPopup.prefab` | Vendor Blood Text pop/motion, 2s total pre-exit lifetime, 0.2s fade-out |

All use DNP unscaled timing. In the DNP Inspector, **Lifetime includes Fade In**, but not Fade Out. Current rank Lifetime is 2.3; Powerslam Lifetime is 2.12. DNP GUI's initial mesh/alpha guard costs a few frames: actual measured full-opacity rank holds were 1.966, 1.972, and 1.968 seconds in the 60fps controlled test. Increase Lifetime to extend the stay; Fade In / Fade Out control the transitions.

Powerslam's scale-over-time key positions were compensated for the longer normalized lifetime, preserving its original real-time entrance punch instead of stretching it. Absolute-time curve comparison against the previous curve differed by no more than 0.00000012. Existing Comic font, material, per-rank color/scale, anchor and optional voice paths remain intact.

Open `Assets/Prefabs/Characters/UICamera.prefab`, then **Canvas / ArcadeAnnouncements → ArcadeAnnouncerPresentation** for rank intensity, Blood popup scale and min/max kill spacing (default 2–4, inclusive). The rank prefab is separate from Powerslam's, so their presentation can be tuned independently. No per-frame polling was added to the adapter; the September 15 start delays use cancellable coroutines only when a remark is pending.

## Blood style and reward integration

The new independent prefab is copied from `Assets/DamageNumbersPro/Demo/Prefabs/3D/Blood Text.prefab`. It uses the existing font asset **`Assets/DamageNumbersPro/Materials/Bloody/Blood-Thick.asset`** and its **Blood Thick Material** subasset. The source demo originally used Blood-Thin; only the project-owned copy is assigned Blood-Thick. No vendor assets or existing cash prefab were edited.

`ArcadeAnnouncerPresentation.TryShowEnemyKill()` owns an independently randomized 2/3/4-kill interval and one of the four exact requested phrases. It counts real enemy deaths, not barrels, damage hits or individual Ruthless taps. The interval is initialized with the presentation owner and rerolled after each Blood popup; it is not rerolled by UI enable/disable. Cadence and label selection use a separate `System.Random`, not gameplay's Unity random stream. The DNP library retains its own existing animation randomization.

`SkateRunnerGUIManager.HandleDestroyedForCash()` calls this once from the existing guarded `OnDestroyed` event, before checking for a cash reward component. Position is exactly the old death cash position: destroyed object position + `cashPopupWorldOffset` (currently Y +1.2). Thus reward-disabled/zero-cash enemies still count. Only a successfully spawned Blood phrase suppresses that kill's normal cash text. Cash amount calculation, `AddCash`, UI balance refresh, and per-tap Ruthless cash remain unchanged. Missing optional Blood prefab/owner falls back to normal cash presentation.

The mesh popup uses the same world-space camera-facing/render-through-walls/consistent-size settings as cash. It has a prewarmed 12-object pool and no combination/destruction/collision/push interaction. Rank and Powerslam each have independent eight-object pools. Rapid kills may produce multiple legitimate Blood labels in different death positions; only the corresponding kill's cash is suppressed, not all nearby cash or the rewards themselves.

## Verification

Controlled Play Mode used the live gameplay scene, its GUI/announcer, real Enemy1_Female and barrel prefab copies, and real `ApplyDamage` callbacks. World time was held at zero so hazards could not interrupt. Mission/progression listeners and hit-stop feedback were isolated only in the temporary test harness. Session cash was restored before exiting; no result saving/Continue was invoked.

Passing checks:

- 120 pooled enemy deaths produced 39 Blood popups. Every interval was 2, 3 or 4; all interval sizes and all four words appeared.
- Every test kill awarded the configured seven cash. Every Blood kill had zero cash popup; every other rewarded kill had one. Every Blood spawn matched the authoritative cash death position.
- Duplicate fatal damage did not award again or advance cadence; pooled reactivation did. Barrels did not advance it.
- Disabled cash rewards still allowed eligible Blood phrases without awarding cash. Missing Blood prefab retained normal cash reward and popup.
- All three rank boundaries (6, 11, 16) showed intermediate entrance alpha without a scale pop, held at full opacity for the measured times above, faded gradually, then returned to their pool at timeScale zero.
- Powerslam retained its entrance flags/curve and remained opaque at 1.95 seconds, then expired normally.
- Two owner disable/enable cycles retained exactly one rank event subscription. Blood pool remained 12 after all kills.
- Final changed-prefab/live-scene missing-script scan: zero. Saved scene hash remained `98E43C476D8F89CC5F2B90E05686AA4A94E91687CC258062722D9A563178B023`; audio prefab hash remained `37836A129FE2E6D3960910615FE7E534805BF5CDEE42CA7B2903C42A753B7D50` throughout this pass.

Visual checks in the real Game View:

- [Blood Thick: four phrase style samples](Verification/ArcadeBloodRevision/blood-phrases-visible.png). These were placed across the camera for style comparison, not a claim that all four appear on one kill.
- [Killer Assassin still visible at about 1.8 seconds](Verification/ArcadeBloodRevision/killer-assassin-hold.png).

The startup fader was hidden **in Play Mode only** for these frozen-world screenshots; its serialized setup remains unchanged. The temporary verification script/meta and unused captures were removed. No test/debug code enters the released game. No full hazardous-level run, target-device profiling, or standalone build was performed.

## File scope

- Runtime adapter: `Assets/Scripts/UI/Presentation/ArcadeAnnouncerPresentation.cs`.
- Single cash-versus-Blood decision: `Assets/Scripts/SkateRunnerGUIManager.cs`.
- Explicit Editor-only authoring: `Assets/Editor/ArcadeKillPopupSetup.cs` (never auto-runs; preserves already-created rank/Blood tuning).
- Existing prefab updates: `Assets/Prefabs/Characters/UICamera.prefab`, `Assets/Prefabs/UI/DN_ArcadeAnnouncement.prefab`.
- New prefabs: `Assets/Prefabs/UI/DN_ArcadeRank.prefab`, `Assets/Prefabs/UI/DN_EnemyBloodPopup.prefab`, with Unity-generated metadata.

The already-registered DNP vendor archive is reused. This newest revision is under user review; prior accepted canonical ELROI knowledge has not been overwritten as if this delta were already accepted.
