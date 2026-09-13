# Gameplay audio and arcade callouts

Presentation follow-up: [Arcade Blood Revision](ArcadeBloodRevision-2026-09-13.md) supersedes the short shared announcement lifetime below. Ranks now have a separate fade/hold preset, Powerslam has a longer hold, and occasional world-space Blood phrases replace only the corresponding kill's cash text. Audio and gameplay eligibility described here are unchanged.

Implemented in the live Skate Runner project, Unity **6000.0.67f1**, via Unity MCP. This supersedes the earlier audio-owned Outro/result timing in `AudioInventoryPolish-2026-09-12.md`.

ELROI documentation approved and promoted on 2026-09-13: the reusable DNP GUI callout workflow is in `C:/Obsidian_Vaults/ELROI_Brain/Elroi_Tools/Damage Numbers Pro GUI Callouts.md`, with the accepted project integration in `Elroi_Implementations/Skate Runner/Arcade Callouts.md` in that vault. Installed DNP **4.51** was confirmed from both its assembly-definition and Comic prefab AssetOrigin metadata; Unity UI is **2.0.0**. This promotion covers the offered DNP GUI/pooling/motion setup, not unrelated audio workflows.

Vendor-source archive: `C:/Obsidian_Vaults/ELROI_Brain/Vendor_Docs/Damage Numbers Pro/SOURCE.md`, registered as `vendor.damage-numbers-pro`. The imported `Documentation.url` and its metadata are copied unchanged; its linked official guide and 15 illustrations are saved locally, with an offline-readable derivative and a capture manifest. The website snapshot is rolling documentation, not a guaranteed version-pinned 4.51 manual.

## Configuration

Open **Assets/Resources/SkateRunnerAudio.prefab**, component **SkateRunnerAudioManager**:

- **Music Transitions → Gameplay Death Fade Duration**: 0.25 seconds, unscaled.
- **Music Transitions → Gameplay Outro Crossfade Duration**: 0.4 seconds, unscaled; fades only outgoing Intro/Body.
- **Destruction → Barrel Destruction**: assigned the project's existing `Assets/Resources/Barrel.wav`. Optional; clearing it is safe.
- **Optional Arcade Announcers (SFX)**: Powerslam, Killer Assassin, Brutal and Ruthless cue slots. All four remain **null**, volume 1, pitch 1, random pitch 0. Assign your voice clips here. No voice/audio files were generated.

Open **Assets/Prefabs/Characters/UICamera.prefab → Canvas/ArcadeAnnouncements**, component **ArcadeAnnouncerPresentation**:

- Per-callout scale/color; defaults Powerslam 1.1, Killer Assassin 1, Brutal 1.12, Ruthless 1.25.
- RectTransform controls placement: upper center, 68% of canvas height. It does not intercept pointer input.
- Announcement Prefab points to **Assets/Prefabs/UI/DN_ArcadeAnnouncement.prefab**. Tune its Damage Numbers Pro inspector for font/material, entrance, upward motion, lifetime and fade. The existing cash popup is untouched.

**S_01_Male/Body → Phase2PowerSlamFrameEvents → Level End Presentation Delay Seconds** is the gameplay-owned result delay (6 seconds), in `Assets/Prefabs/Characters/S_01_Male.prefab`. `FormerlySerializedAs` preserves the previously authored fallback-delay value under its clearer new name.

## Behavior and causes

### Death and actual retry

Previously `LifeLost` only played the death cue; music continued, and the already-started guard ignored the subsequent retry `GameStart`.

The same authoritative `LifeLost` now cancels the current transition and pending DSP starts, fades all gameplay music quickly, stops/clears Intro, Body and Outro, and marks the run dead. Death never requests Outro. The real retry path is `LifeLostAction → ResetLevel/PrepareStart → LevelStart → GameStart`; that restarts the **same selected track package** from Intro, or directly from Body when Intro is absent. Temporary pause/UI events do not restart the track. A new scene resets selection as before.

Music-off cancels/stops all sources. Re-enabling while dead stays silent. A retry while muted resets the package state without playback; enabling Music after that starts the beginning normally. Re-enabling after a completed ending does not resurrect the old Body.

### Drone and barrel destruction

The actual drone prefab is **DroneRoot**, with `SkateRunnerDestructibleObject` on the root but `EnemyTypeDrone` on its **EnemyDrone child**. The old classifier searched only parents from the destructible, missed that child, and selected the unassigned unknown-enemy fallback. It also checked Type 1 before Drone. The live `EnemyTypeDrone` class derives from `EnemyBase`, so the root/child mismatch—not C# inheritance alone—was the confirmed failure.

`ResolveAudioKind()` now checks Drone first across the destructible's parents and own descendants, then the explicit classification and other enemy types. It deliberately does not search an arbitrary pool-container `transform.root`, which could contain unrelated enemies.

The audio manager now listens to the existing **OnDestroyed** event for all destructibles. The barrel prefab is explicitly classified **Barrel** and still has `countsAsEnemyKill=false`. A real destruction plays exactly one barrel cue and returns before enemy audio. The existing `_isDead` guard prevents duplicate callbacks; reactivation resets it normally. Surviving damage/touch produces no destruction audio. Enemy reward, kill-cause, health, collision, pooling and damage code was not changed. No stumble/punishment mechanic was added.

### Immediate Outro; gameplay owns the result screen

`RuthlessTapModeController.End()` verifies a still-active, non-dead sequence and captures **LevelManager.RuthlessTapCount** before its existing gameplay callback exits the mode. It publishes **CompletedSuccessfully(finalCount)** once, then executes that original callback. Death, GameOver, execution reset, disable and an unrelated mode exit do not publish success.

The audio subscriber immediately plays the selected Outro at its configured normal track volume on a third reusable music source. Outgoing Intro/Body fades underneath it for 0.4 seconds, then is stopped and cleared. Pending Body starts and looping are cancelled. No clip-boundary or landing wait exists. Missing Outro simply fades the old music; muted audio remains silent.

Removed the grounded call to `EndGameplayMusicAtFinalLanding`, the audio wait in `ShowLevelEndAfterDelayCo`, `WaitForLevelEndingPresentation`, `GameplayMusicEndingCompleted`, public `MusicEndingState`/`EndingState`, the boundary-scheduling Outro routine and its completion/watchdog plumbing. The two old serialized audio timing fields are retained **hidden and unused** only to preserve existing prefab data.

`ShowLevelEndAfterDelayCo()` now uses only `WaitForSecondsRealtime(levelEndPresentationDelaySeconds)` between the existing gameplay success notification and result UI call. No audio source, clip duration, announcer or audio completion event controls it. Existing final traversal, camera/landing presentation and success bookkeeping were not changed. A long Outro may continue after the result screen appears.

### Powerslam and final ranks

Powerslam originates in **SwipeDownDetector.TryTriggerGroundImpactFromCollider**, alongside the existing FEEL/VFX impact, using the exact same full-meter `slamReady` condition **before charge consumption**, guarded by `impactTriggeredThisDownAttack`. Normal down attacks and initial swipe/button presses do not produce a callout.

Successful final ranks come exclusively from the captured accepted count:

| Final count | Only announcement |
| --- | --- |
| 0–5 | None |
| 6–10 | KILLER ASSASSIN |
| 11–15 | BRUTAL!!! |
| 16+ | RUTHLESS!!! |

The optional voices use the existing SFX settings/cue structure and a dedicated reusable priority-16 AudioSource. It ignores listener pause, uses no Doppler/timescale pitch adjustment, and can overlap combat and Outro. A null voice never blocks the visual. No new preference or persistence system was introduced.

## Damage Numbers Pro presentation

Copied **Assets/DamageNumbersPro/Demo/Prefabs/UI/Comic.prefab** into the independent **DN_ArcadeAnnouncement** prefab. Both authored Comic TMP font assets and the strong outline material **Assets/DamageNumbersPro/Materials/Saw/Comic.asset** are preserved. No existing production TMP font/material was replaced.

The integration uses the vendor's GUI spawn API (`SpawnGUI`), per-instance scale/color and a prewarmed pool of eight. These public APIs were checked against the [official Damage Numbers Pro documentation](https://ekincantas.com/damage-numbers-pro/). The preset uses unscaled time, 0.78-second lifetime, 0.12-second entrance and 0.22-second fade-out, an overshooting scale pop and slight upward motion. DNP GUI movement expands the preset offsets into canvas units: the verified settings use 0.35 upward lerp, -0.18 entrance offset and 0.16 exit offset, not pixel-sized values. Tune from these starting values.

Visual checks in the actual gameplay Game View confirmed readable centered placement above the player, with controls unobscured. Captures:

- [Powerslam](Verification/AudioArcadePolish/powerslam-1.png)
- [Killer Assassin](Verification/AudioArcadePolish/killer-assassin.png)
- [Ruthless](Verification/AudioArcadePolish/ruthless.png)

## Verification actually performed

Controlled Play Mode tests used the live gameplay scene, actual player/GUI/controllers and instantiated copies of the real enemy/barrel prefabs. World movement was frozen so hazards could not prevent Phase 2 tests. Runtime-only in-memory tone clips exercised Intro/Body/Outro/voice timings; no audio files were written. The real assigned drone/barrel/Type 1 clips were used for destruction tests.

**57 assertions passed**, including:

- Real `KillCharacter → LifeLost`, fast fade/stop, real `LifeLostAction` retry, same package, Intro → Body loop, missing Intro fallback, pause/resume and mute/dead/retry combinations.
- Immediate Outro while Intro is active and while a deliberately long Body is looping; old-source fade/cleanup; death during Outro.
- Real destructible `ApplyDamage` with DashAttack kill context: surviving barrel silent, duplicate fatal damage one cue, drone only its assigned cue, Type 1 unchanged, drone pooling/reactivation, SFX-off barrel silence. **The assigned drone source produced nonzero sampled audio output**, not merely a matching classification.
- Final count boundaries 5, 6, 10, 11, 15, 16; exactly one appropriate DNP instance; dedicated optional voice output; no success/rank on death; normal impact silent; powered `OnTriggerEnter` impact callout exactly once; null voice still shows the visual.
- Real `ShowLevelEndAfterDelayCo` and result UI: hidden just before 6 seconds, visible by the next check around **6.12–6.13 seconds**, identically with a 30-second Outro, no Outro and Music disabled. The long Outro was still playing when the result screen appeared.
- DNP callouts expire back into their pool.

Additional checks: two disable/enable cycles leave exactly two success subscribers (audio + presentation), unrelated mode exit emits zero success events, and the DNP pool remains eight objects after repeated announcements.

[Detailed passing output](Verification/AudioArcadePolish/play-mode-results.txt). These are controlled callback/integration tests, **not** a full manual swipe-through of the hazardous level. No Continue button or `SaveAfterLevelEnd` was invoked. Runtime test values were restored and Play Mode exited. The temporary verification script and its meta were removed; none enters a build.

Final Unity compile: **zero console errors**. Missing-script scan of all changed/new prefabs and the live scene: **zero**. Scene **SkateRunner.unity remains clean**, with no direct scene edits. The live central audio prefab was compared again after all tests: **all 45 pre-existing serialized properties, including every clip/volume/pitch/playlist assignment, exactly match the pre-edit live snapshot**. Seven new top-level properties were added. All four announcer clips remain null.

Manual follow-up: assign your announcer recordings, judge their final mix/loudness, and check the callout sizing/timing and final cinematic together on target mobile hardware/aspect ratios. No standalone player build or full hazardous-level playthrough was performed.

## Exact file manifest for this pass

Modified runtime scripts:

- `Assets/Scripts/Audio/SkateRunnerAudioManager.cs`
- `Assets/Scripts/SkateRunnerDestructibleObject.cs`
- `Assets/Scripts/RuthlessTapModeController.cs`
- `Assets/Scripts/Phase2PowerSlamFrameEvents.cs`
- `Assets/Scripts/SkateRunnerGUIManager.cs`
- `Assets/Scripts/SwipeDownDetector.cs`

New scripts (and Unity-generated `.meta` files):

- `Assets/Scripts/UI/Presentation/ArcadeAnnouncerPresentation.cs` — runtime DNP presentation.
- `Assets/Editor/ArcadeAudioPresentationSetup.cs` — explicit, idempotent authoring helper; no automatic execution, excluded from player assemblies.

Modified prefabs:

- `Assets/Resources/SkateRunnerAudio.prefab` — new cues and transition settings; old properties unchanged.
- `Assets/Prefabs/Props/Enemies/barrel_01_destructible.prefab` — explicit Barrel audio classification only.
- `Assets/Prefabs/Characters/UICamera.prefab` — new non-interactive Canvas/ArcadeAnnouncements child and references.

New prefab (and `.meta`):

- `Assets/Prefabs/UI/DN_ArcadeAnnouncement.prefab`

No scene was directly modified; existing prefab instances inherit the new UI. The player prefab was not rewritten; its serialized delay migrates through `FormerlySerializedAs`.

Project-only handoff artifacts: this report, the supersession notice in `Documentation/AudioInventoryPolish-2026-09-12.md`, and the four verification files linked above. No canonical ELROI knowledge has been promoted without approval.
