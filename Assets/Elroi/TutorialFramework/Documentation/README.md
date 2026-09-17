# ELROI Tutorial Framework v1

ELROI Tutorial Framework is an original, scene-local Unity tutorial runtime. It supports a simple manual API and an optional automatic sequencing layer without requiring either workflow to adopt the other.

The authoring model is deliberately strict:

- **Tutorial Definition = WHAT** is shown and how it completes.
- **Tutorial Sequencer = ORDER / AUTOMATION** for an optional onboarding flow.
- **Sequence Entry = WHEN** a referenced Tutorial Definition starts.
- **TutorialManager = RUNTIME / API** for one scene.

## Requirements and package boundary

- Unity 6 was used for v1 verification.
- Unity UI and TextMeshPro are used for presentation.
- The runtime has no game-specific assembly dependency.
- Add `Assets/Elroi/TutorialFramework/` to a future project as one package boundary.
- The manager is scene-local. It never calls `DontDestroyOnLoad`.

## Quick start

1. Drag `Prefabs/ELROI_TutorialManager.prefab` into a scene.
2. Assign a component implementing `ITutorialGameplayAdapter` if tutorials hand gestures into real gameplay actions.
3. Optionally assign components implementing `ITutorialVariableProvider` and `ITutorialPersistenceProvider`.
4. Add a Tutorial Definition to **Tutorials** and give it a unique Tutorial Name.
5. Leave **Tutorial Sequencers** empty for a manual-only manager.
6. Call the manager from gameplay code.

```csharp
tutorialManager.PlayTutorial("Boss Weak Point");

if (tutorialManager.TryPlayTutorial("Inventory Introduction"))
{
    // The tutorial is now active.
}
```

The manager also exposes:

```csharp
tutorialManager.StopCurrentTutorial();
tutorialManager.CompleteCurrentTutorial();
bool playing = tutorialManager.IsTutorialPlaying;
TutorialDefinition definition = tutorialManager.GetTutorial("Boss Weak Point");
```

## Runtime target override

Use a runtime context when the highlighted object is spawned dynamically:

```csharp
tutorialManager.PlayTutorial("Enemy Introduction", spawnedEnemy);

// Equivalent explicit context:
tutorialManager.PlayTutorial(
    "Enemy Introduction",
    new TutorialContext(spawnedEnemy));
```

Set the definition's Target Source to **Runtime Context Target**. This avoids storing a scene reference that cannot exist at authoring time.

## Tutorial Definitions

Tutorial Definitions live in the manager's independent **Tutorials** list. They contain:

- Enabled, Tutorial Name, stable internal ID, and manual run policy.
- Target Type: None, 2D, or 3D.
- Target Source: Direct Object, Runtime Target ID, or Runtime Context Target.
- Introduction text, bottom instruction, optional gesture animation, spotlight shape and padding, and Freeze World.
- Completion Type: OK Button, Gesture, Event, or Manual/API.
- Required Gesture and generic Gameplay Action ID where applicable.

Automatic trigger settings do not belong to a Tutorial Definition. Keeping them on Sequence Entries allows one definition to be used manually and reused in several automatic contexts.

### Tutorial Name and stable ID

Tutorial Name is the simple programmer-facing lookup key. Names must be unique within one manager. Duplicate names and IDs are shown as validation warnings.

The stable ID is generated once and survives list reordering and renaming. Persistence and sequencer references use the stable ID, so renaming a Tutorial Name does not inherently lose completion state.

## Tutorial Sequencers

The independent **Tutorial Sequencers** list is optional. A sequencer contains:

- Enabled, Sequence Name, stable ID, and run policy.
- A start trigger.
- Lock Gameplay Between Tutorials.
- Ordered Sequence Entries.

Manual control is also available:

```csharp
tutorialManager.PlaySequencer("First-Time Controls");
bool started = tutorialManager.TryPlaySequencer("First-Time Controls");
tutorialManager.StopSequencer("First-Time Controls");
bool advanced = tutorialManager.TriggerPendingEntry();
```

The last enabled entry is detected automatically. Disabled or deleted trailing entries do not require a separate “final tutorial” flag.

### Sequence start triggers and entry triggers

Supported trigger types are:

- Immediate
- Time
- Distance
- Visible On Screen
- Variable Condition
- Event
- Manual/API

Only a pending spatial entry runs its distance or visibility watcher. Future entries are dormant. Timers exist only while needed, event and variable triggers are notification-driven, and gesture capture exists only while a matching tutorial is active.

### Time

Time triggers can originate at manager/scene start, sequencer start, or previous tutorial completion. They can use scaled or unscaled time. Scaled time is the default for gameplay timing, so a frozen tutorial does not advance normal game-time delays.

### Distance and Y range

Distance triggers compare `abs(target.x - reference.x)` to Maximum Absolute X Distance. They do not use vector magnitude. The optional relative Y interval checks `target.y - reference.y` against the configured minimum and maximum. Both checks must pass.

### Actual visibility and visibility delay

3D visibility projects Renderer/Collider bounds through the configured camera and tests intersection with that camera's viewport while rejecting points behind it. 2D visibility resolves RectTransform screen bounds. Active objects outside the view do not count.

Visibility Delay requires uninterrupted visibility. Leaving the screen resets accumulated delay to zero.

## Variables

`ITutorialVariableProvider` supports current-value reads plus a `VariableChanged` event. Integer, Float, Boolean, and String values are supported.

`TutorialVariableStore` is the built-in no-code provider. Its setters emit only when a value actually changes:

```csharp
variableStore.SetInteger("Coins", 100);
variableStore.SetFloat("Health", 75.5f);
variableStore.SetBoolean("HasSword", true);
variableStore.SetString("Class", "Warrior");
```

On manager initialization, current values are evaluated once before the framework waits for future events. Start-variable dependencies are indexed by variable ID so unrelated sequencers are not reevaluated.

Numeric comparisons support `==`, `!=`, `>`, `>=`, `<`, and `<=`. Boolean and String values support `==` and `!=`. Incompatible comparisons fail safely and produce a bounded warning.

## Tutorial events

Raise an event directly:

```csharp
tutorialManager.RaiseEvent("BossEnteredArena");
```

`TutorialEventEmitter.Fire()` is a no-code bridge for Buttons, UnityEvents, animation events, and collider scripts.

## Targets

### Runtime target IDs

Add `TutorialTargetMarker` to a live object and assign a stable Target ID. Target resolution removes stale markers, selects the lowest active Unity instance ID deterministically when duplicates exist, and warns once about ambiguous active IDs.

### 2D

A 2D target must resolve a RectTransform. Its world corners are converted to screen coordinates with the owning Canvas camera when required. A missing RectTransform is a validation error; it is not silently reinterpreted as 3D.

### 3D

Renderer and Collider bounds are combined recursively and projected into a screen rectangle. Moving objects are tracked every presentation frame. If no bounds exist, the object's Transform point plus the configured fallback dimensions is used and can be reported during validation/debugging.

## Presentation

`Prefabs/ELROI_TutorialCanvas.prefab` is reused for every tutorial. It contains one input capture surface, one spotlight, one procedural dialogue bubble and tail, one introduction text, one bottom instruction, one gesture image, and one OK button.

### Spotlight

The spotlight is one fullscreen custom Unity UI Graphic with an original shader. It supports Rectangle, Rounded Rectangle, and Circle holes plus overlay opacity, padding, corner radius, feather, transition values, and an optional pulse. Presentation and gesture animation use unscaled time.

### Dialogue bubble

The original manga-style bubble uses procedural Unity UI shapes, an outline, a triangle tail, and TextMeshPro. It evaluates space above, below, left, and right of the highlighted rect, chooses a useful side, clamps within safe margins, wraps text, and enables bounded TMP autosizing.

### Gesture animation

`TutorialGestureAnimation` is a ScriptableObject containing shared sprite frames, FPS, Loop, and playback scale. Definitions reference the shared asset; they do not duplicate frames. A null asset hides the gesture image.

### OK button

OK Button completion works while `Time.timeScale == 0` because Unity UI events and the presentation use unscaled timing. It is hidden for Gesture, Event, and Manual/API completion modes.

## Gestures and input handoff

Built-in gestures are Tap, Double Tap, Swipe Left, Swipe Right, Swipe Up, and Swipe Down. The capture surface uses generic Unity pointer events, so touch and Editor mouse testing follow the same path.

Double Tap stores a first-tap timestamp and accepts the second only within Double Tap Maximum Interval. An expired attempt is replaced by a new first tap.

For a Gesture tutorial with a Gameplay Action ID:

1. The overlay consumes the completing pointer release.
2. Presentation closes and the prior world timescale is restored.
3. `ITutorialGameplayAdapter.ExecuteGameplayAction(actionId)` invokes the real game action once, or optional `ITutorialAsyncGameplayAdapter` completes a multi-frame composite action.
4. Normal gameplay remains locked until the action routine finishes, then through end-of-frame and one following frame.
5. The framework unlocks input, or keeps it locked when a sequencer still owns the between-step lock.

This ordering prevents the underlying input scripts from processing the same release after the overlay disappears.

## Gameplay adapter

Implement `ITutorialGameplayAdapter` outside the reusable package:

```csharp
public interface ITutorialGameplayAdapter
{
    void SetGameplayInputBlocked(bool blocked);
    bool CanExecuteGameplayAction(string actionId, out string reason);
    void ExecuteGameplayAction(string actionId);
}
```

The core treats action IDs as opaque strings. Another game may map `Reload`, `Interact`, or `Crouch` without modifying the framework.

For a composite action such as replaying two production tap pulses, also implement `ITutorialAsyncGameplayAdapter`. The manager yields its `ExecuteGameplayActionRoutine` result and retains the gameplay-input lock for the entire routine.

## Freeze and cleanup

The default `TimeScaleTutorialFreeze` stores the exact previous `Time.timeScale`, sets zero, and restores the stored value. Stop, disable, destroy, cancellation, and normal completion all run cleanup. The abstraction can be replaced in a later version without changing Tutorial Definitions.

## Persistence and run policies

`ITutorialPersistenceProvider` reads and marks Tutorial and Sequencer stable IDs. The included `PlayerPrefsTutorialPersistence` is suitable for the standalone demo and simple games; production games can provide their own save adapter.

Sequencer policies are Every Time, Once Per Scene Load, Once Per Session, and Once Ever. Manual tutorials separately choose Always Allow or Respect Completion, so replayable help content remains possible.

## Lifecycle and performance

The manager has no `Update` method. With Tutorials configured and no Sequencers, it remains enabled in an idle, callable state with no polling or trigger watchers. Empty managers can disable themselves. Automatic work starts only the watcher required for a start trigger or currently pending entry, then stops it.

When Automatic Work Completes defaults to **Stay Idle**. **Disable Component** is available when no manually callable content remains. Cleanup disables only the component, never the entire GameObject.

## Demo

Open `Demo/ELROI_Tutorial_Demo.unity`. It contains only Unity primitives, Unity UI/TextMeshPro, and this package. It demonstrates:

- manual play by Tutorial Name;
- runtime target override;
- 2D and 3D target projection;
- all built-in gestures;
- time, variable, event, X/Y distance, and camera visibility triggers;
- world freeze and unscaled presentation;
- locked gameplay between entries and automatic final unlock.

## Extending

- Add a new gameplay integration by implementing the interfaces, outside the package.
- Add a new trigger by extending `TutorialTriggerType`, its conditional editor drawer, and the manager's watcher arming path.
- Add a new completion mode beside OK/Gesture/Event/Manual without moving automatic trigger data into Tutorial Definitions.
- Replace presentation styling through `TutorialTheme` while retaining one shared Canvas.

## Known v1 limitations

- Cross-scene sequencing is intentionally out of scope.
- Clicking the highlighted target as a completion type is reserved for a later version.
- The default persistence component cannot enumerate and delete an arbitrary PlayerPrefs prefix.
- Only one sequencer actively presents entries at a time.
- A World Space Canvas must provide a working Canvas camera for correct 2D projection.

## Verification

The v1 test suite covers lookup and duplicate validation, stable IDs, comparisons, change-only variable events, every gesture direction, real double-tap timing, sequencing and final-entry detection, X/Y distance, visibility projection and delay reset, run policies, deterministic target resolution, exact timescale restoration, idle-manager behavior, and exact-once input handoff.
