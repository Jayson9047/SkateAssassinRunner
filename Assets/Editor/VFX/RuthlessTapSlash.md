# Phase 2 presentation: designer guide

## Shipped scene setup

`Assets/Scenes/SkateRunner.unity` contains the complete authored setup:

- `RuthlessTapSlashVolume` owns the one global Volume and one `RuthlessTapSlashFeedback`. Ruthless taps and the Final Strike share this cached Fronkon `SlashVolume` and the existing one renderer feature.
- `Phase2SpeedlinesController/Phase2SpeedlinesBackground` is a permanent instance of `Assets/Art/Speedlines/frame_001.prefab`. The background starts inactive, has no collider, casts/receives no shadows, and its Animator uses `UnscaledTime`.
- `FEELManager/FinalStrikeFlashFEEL` contains an independent unscaled copy of the powered Down Attack kill-flash feedback from `FEELManager/FlashFEEL`.
- `FEELManager/FinalStrikeCameraShakeFEEL` contains an independent unscaled copy of the `Cinemachine Impulse` from the player prefab's `PowerSlamFEEL`. Its FOV feedback is intentionally not copied.
- `FEELManager/FinalStrikeHapticFEEL` contains one unscaled `MMF_NVContinuous`.
- `FEELManager/Phase2FinalStrikeFeedback` holds the scene references for the shared Slash, Flash player, camera-shake player, and haptic player.
- `Phase2Camera` has its `CinemachineBrain` reference assigned. `IsCollisionCameraSettled` is true only when Cinemachine reports no active blend and `VCam_Collision` is live.

The runtime player prefab resolves these scene presentation controllers once through their active scene instances. No scene reference is stored inside the player prefab, and no per-tap object search occurs.

## Speedlines

Timing is automatic. When the Collision-camera switch begins, Phase 2 arms the controller after Ruthless Tap becomes active. The background remains off while the Brain is blending. It starts only when `VCam_Collision` is live and the Brain is no longer blending. Ruthless end cancels any pending show, hides the background immediately, and only then starts the return-camera flow.

To position it, open `Assets/Scenes/SkateRunner.unity`, expand `Phase2SpeedlinesController`, select `Phase2SpeedlinesBackground`, and edit its **Transform > Position / Rotation / Scale**. This is the only intentionally manual setup. Keep the flat sprite facing the Phase 2 camera and keep the authored horizontal streak orientation.

To change animation speed, open `Assets/Art/Speedlines/frame_001.controller` in the Animator window, select the `speedlines` state, and edit **Speed**. The source `Assets/Art/Speedlines/speedlines.anim` contains 71 frames, samples at 36 fps, and has **Loop Time** enabled. The scene Animator is forced to **Update Mode: Unscaled Time** when shown.

In Play Mode, select `Phase2SpeedlinesController` and use **TEST SHOW** / **TEST HIDE**. These are Editor Inspector controls only and do not start Phase 2.

## Ruthless Tap Slash timing

Open `Assets/Scenes/SkateRunner.unity`, select `RuthlessTapSlashVolume`, then edit **Ruthless Tap Slash Feedback**:

- **General > Slash Duration**: short tap lifetime, default `0.18` real seconds.
- **General > Peak Intensity**: default `0.85`.
- **General > Fade Start Normalized**: fraction of lifetime held at peak before SmoothStep fade, default `0.40`.
- **General > Start Progress**: initial Fronkon reveal progress, default `0.03`.
- **Rotation**: random angle range and minimum consecutive separation.
- **Size / Impact > Visual Impact Scale**: tap core/smoke/glow scale.
- **Advanced Visual Settings** in the custom Inspector: slash shape, glow alpha, smoke alpha/mix, smoke size, and grading.

Every accepted Ruthless tap replaces the current short Slash. It still uses an allocation-free private PRNG, preserves the minimum angle difference, and runs on unscaled time. A Final Strike has priority, so a late tap cannot replace it.

## Final Strike Slash timing and size

On the same `RuthlessTapSlashVolume` component, use the **Final Strike** section:

- **Final Strike Duration**: default `2.0` seconds; range `0.5–4.0`.
- **Final Peak Intensity**: default `1.0`.
- **Final Visual Impact Scale**: default `1.8` times the tap preset.
- **Final Fade Start Normalized**: default `0.10`; fade continues across most of the lifetime.
- **Final Start Progress**: default `0.03`.
- **Final Core Width Multiplier**: default `1.75`.
- **Final Glow Multiplier**: default `1.65`.
- **Final Smoke Multiplier**: default `1.35`.
- **Final Slash Angle Offset**: default `0°`; use this only for a small artistic calibration.

The trigger projects `PlayerMeetPoint` and `PlayerStrikeEndPoint` through the gameplay camera, computes `atan2(endScreen.y - startScreen.y, endScreen.x - startScreen.x)` in screen pixels, adds **Final Slash Angle Offset**, and writes degrees to Fronkon. Pixel-space direction keeps the angle correct across phone aspect ratios. Fronkon's shader has a fixed fullscreen center and no translation parameter, so the implementation aligns the trajectory angle and uses the wider core/glow rather than modifying vendor code.

In Play Mode, select `RuthlessTapSlashVolume` and use **TEST FINAL SLASH**. Its Editor test references are already assigned to the authored `PlayerMeetPoint` and `PlayerStrikeEndPoint` prefab transforms.

## Power colors

Open `Assets/Prefabs/VFX/RuthlessTapSlash/WeaponPowerScreenSlashPalette.asset`. Expand **Colors** and edit the entry whose **Power** field matches the desired weapon power. Initial RGB values are:

| Power | Color |
|---|---|
| None / Default | `#4E9DFF` |
| Ice | `#D8F4FF` |
| Electricity | `#3F5BE8` |
| Fire | `#FF9F2F` |
| Poison | `#63E06B` |
| Magic | `#A95CFF` |

The active runtime `WeaponPowerEquipper.GetEquippedWeaponPowerId()` is authoritative. Missing palette or equipper falls back to None/azure. The palette is cached before input; taps do not use LINQ, PlayerPrefs, or per-tap allocations. Smoke RGB is derived from the same primary color. On `RuthlessTapSlashVolume > Ruthless Tap Slash Feedback > Advanced Visual Settings`, edit **Light Smoke White Mix**, **Dark Smoke Black Mix**, and the alpha of **Smoke Color 1 / 2**.

The custom Inspector also provides **TEST TAP — DEFAULT / FIRE / ICE / ELECTRICITY / POISON / MAGIC**, plus the existing 10- and 50-trigger burst tests. These test only the visual system.

## Final FEEL timing

For flash, expand `FEELManager`, select `FinalStrikeFlashFEEL`, expand its **Flash** feedback, and edit:

- **Flash Duration**: `0.10` seconds.
- **Flash Alpha**: `0.50`.
- **Flash Color**: white.

This is a copied `MMF_Flash` configuration from `FEELManager/FlashFEEL`: it broadcasts to the same existing `FlashImage` target through Flash ID `0` and is forced to unscaled time. It does not reference or mutate the Down Attack player; both players own independent feedback copies.

For camera shake, select `FEELManager/FinalStrikeCameraShakeFEEL` and expand **Cinemachine Impulse**:

- **Impulse Duration**: `0.20` seconds.
- **Amplitude Gain**: `0.50`.

The Raw Signal, frequency, velocity, envelope, and channel are copied from `S_01_Male/PowerSlamFEEL`. Final Strike owns an independent feedback copy and runs it unscaled. Adjust **Amplitude Gain** and **Impulse Duration** there to tune strength and duration. The powered slam's FOV feedback was not copied, so Final Strike does not alter camera transition or lens logic.

For haptics, select `FEELManager/FinalStrikeHapticFEEL`, expand **Haptic Continuous**, and edit:

- **Min Duration / Max Duration**: both `0.60` seconds.
- **Min Amplitude / Max Amplitude**: both `0.80`.
- **Min Frequency / Max Frequency**: both `0.35`.

The player and feedback are unscaled. Playback occurs only when `GameSettingsSave.IsVibrationEnabled()` and `SystemInfo.supportsVibration` are both true. Disabling vibration never suppresses the Slash or flash and never changes the saved setting.

## Runtime hook and cleanup

`Phase2PowerSlamFrameEvents.StartDashToStrikeEnd()` triggers the Final Slash, powered Down-Attack-style flash, camera impulse, haptic, and `PlayRuthlessFinalCut()` together before the existing `DOMove`. Its guard resets on enable, execution reset, and each new execution arm. Movement, ease, speed, animation, slow motion, slicing, damage, camera return, and level completion are unchanged.

`TapOnlyMainActionZone` now calls `StopTapSlashImmediate()` on Ruthless exit. That method stops only `RuthlessTap`; it cannot cancel `FinalStrike` regardless of Update ordering. Full `StopImmediate()` remains the hard cleanup used for component/scene disable, pause, destruction, and explicit Inspector stop.

## Isolated validation

Validation used a temporary minimal Play Mode scene and did not run the level. The temporary scene was deleted afterward.

- All six exact palette colors reached the cached Slash override.
- 1,000 warmed `TriggerSlash()` calls allocated 0 managed bytes on the calling thread.
- Final Strike began at intensity 1, faded gradually, ended cleanly at 2 seconds, rejected tap replacement, and survived tap-only cleanup.
- A 33.02° projected diagonal produced a 33.02° Fronkon Slash angle.
- Speedlines stayed hidden at request time, appeared only at the settled Collision camera, used `UnscaledTime`, hid immediately before return, and cancelled an ended-mode pending request.
- Production scene audit found one Flash feedback, one continuous haptic feedback, valid palette/camera/test references, and zero missing-script components.

The remaining acceptance step is the artistic in-level feel pass on target hardware: position/scale the speedline background, then tune Slash/Flash/haptic values if desired and profile the fullscreen effect on the target Android device.
