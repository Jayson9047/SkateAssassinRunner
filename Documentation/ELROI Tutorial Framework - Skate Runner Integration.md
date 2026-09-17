# ELROI Tutorial Framework v1 — Skate Runner Integration

## Installed scene integration

`Assets/Scenes/SkateRunner.unity` contains one scene-local `ELROI Tutorial Manager` with:

- `TutorialManager`
- `SkateRunnerTutorialGameplayAdapter`
- `SkateRunnerTutorialVariableProvider`
- `SkateRunnerTutorialPersistence`

The manager contains two independent lists: five reusable Tutorial Definitions and two optional Tutorial Sequencers.

Both sequencers are intentionally **disabled** until real gameplay content receives the target markers below. This prevents the manager from locking controls while waiting forever on an invented or missing target.

## Production action mapping

`SkateRunnerTutorialGameplayAdapter` maps opaque framework IDs to live gameplay paths:

| Gameplay Action ID | Production path |
|---|---|
| `Jump` | `TapOnlyMainActionZone.TriggerTutorialMainAction()` → current `InputManager` main action |
| `DoubleJump` | Replays two production main-action taps 0.10 real seconds apart so both Jump and Double Jump occur |
| `DashAttack` | `SwipeRightAttackDetector.TriggerTutorialDashAttack()` |
| `AirDashAttack` | Same dash system; grounded/airborne production state determines behavior |
| `DownAttack` | `SwipeDownDetector.TriggerTutorialDownAttack()` |

The forced handoff methods bypass only the normal input-lock check. They do not recreate motion or animation. The tutorial manager retains the global input lock through the completing release frame, invokes the production action once, and unlocks on the following frame or after the final sequence entry.

`DoubleJump` is a composite action rather than one invocation: the Skate adapter sends two real `MainActionButtonDown/Up` pairs through `TapOnlyMainActionZone`. The optional async adapter contract keeps tutorial input locked until both production taps have completed.

## CurrentLevel provider

`SkateRunnerTutorialVariableProvider` exposes `CurrentLevel` from authoritative `SkateRunnerGameManager.LevelNum`.

`SkateRunnerGameManager.OnLevelChanged` fires only when the stored value actually changes. The framework also evaluates the current value once during initialization, so a preloaded level does not require a new event.

## Persistence

`SkateRunnerTutorialPersistence` stores stable Tutorial and Sequencer IDs through the existing save API under the `ELROI.Tutorials.v1.` prefix. No save-system reference exists inside the reusable framework.

## Prepared tutorial content

### Level 1 Basic Controls

Start condition: `CurrentLevel == 1`  
Run policy: Once Ever  
Lock Gameplay Between Tutorials: true

1. Jump Tutorial — Time from manager start.
2. Double Jump Tutorial — absolute X distance plus relative Y range.
3. Ground Dash Tutorial — actual camera visibility plus 0.4-second continuous delay.

After Jump and Double Jump, gameplay world time resumes while normal input remains locked. Ground Dash is the final enabled entry and automatically releases input.

### Level 2 Advanced Controls

Start condition: `CurrentLevel == 2`  
Run policy: Once Ever  
Lock Gameplay Between Tutorials: true

1. Air Dash Tutorial — absolute X distance plus relative Y alignment.
2. Down Attack Tutorial — absolute X distance plus target-below Y range.

## Required target assignments before enabling sequences

Add `TutorialTargetMarker` to the authoritative spawned object and use these exact IDs:

| Target ID | Intended content |
|---|---|
| `Tutorial.Player` | Added automatically to the spawned player by the Skate gameplay adapter |
| `Tutorial.JumpTarget` | Real Level 1 jump hazard or teaching target |
| `Tutorial.DoubleJumpObstacle` | Real large obstacle requiring Double Jump |
| `Tutorial.GroundDashEnemy` | Real grounded dash enemy/target |
| `Tutorial.AirDashTarget` | Real airborne dash target |
| `Tutorial.DownAttackTarget` | Real target beneath the airborne player |

After target markers exist, inspect timing and alignment in live gameplay, enable the relevant sequencer, and repeat the full level flow. Do not enable a sequence before its targets are guaranteed to spawn.

## Files outside the reusable package

- `Assets/Scripts/Tutorial/SkateRunnerTutorialGameplayAdapter.cs`
- `Assets/Scripts/Tutorial/SkateRunnerTutorialVariableProvider.cs`
- `Assets/Scripts/Tutorial/SkateRunnerTutorialPersistence.cs`
- `Assets/Editor/SkateRunnerTutorialInstaller.cs`
- `Assets/Scripts/SkateRunnerGameManager.cs` — adds change-only `OnLevelChanged`
- `Assets/Scripts/TapOnlyMainActionZone.cs` — adds real main-action handoff
- `Assets/Scripts/SwipeRightAttackDetector.cs` — adds real dash handoff
- `Assets/Scripts/SwipeDownDetector.cs` — adds real down-action handoff
- `Assets/Scenes/SkateRunner.unity` — one disabled-until-targeted scene-local configuration

## Verified status

- Unity compilation: passed.
- Standalone demo runtime: passed through time, variable, event, distance/Y-range, and actual visibility entries.
- Gesture handoff: four sequential actions executed exactly once in the demo.
- Between-entry lock and final unlock: passed.
- Exact timescale restoration: passed.
- Manual-only manager with zero sequencers: passed and remained idle/callable.
- Automated tests: 28 EditMode and 2 PlayMode passed.
- Skate scene smoke test with sequences disabled: no compile/runtime errors; CurrentLevel resolved and the manager remained idle.
- Live dash/down action resolution becomes available after the production player spawns; the smoke run remained before that spawn point, so those two live gameplay actions still require target-content acceptance testing after the sequences are enabled.
