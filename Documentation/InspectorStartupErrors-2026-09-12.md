# Inspector startup exceptions: session repair

Project: Skate Assassin Runner / Skate Runner. Unity 6000.0.67f1.

## Symptom and audit

Entering Play Mode repeatedly produced `UnityEditor.GameObjectInspector.OnDisable`
NullReferenceException and `SerializedObjectNotCreatableException: Object at index 0
is null` from GameObjectInspector and TransformInspector OnEnable.

The current Inspector was unlocked. Its active targets were valid after selecting
the audio prefab and forcing a rebuild, but the same errors still recurred. Four
additional Editor objects remained alive with only destroyed/null targets:

- GameObjectInspector: instance -3075128
- GenericInspector: instance -3075126
- TransformInspector: instance -3075124
- Ultimate Preview GameObject editor wrapper: instance -3075122

All four were non-persistent, HideAndDontSave objects, absent from the active
editors of every open Inspector window. They were not scene objects or assets.
The Home scene missing-script scan was clean. Today's audio scripts and new
property drawer/explicit authoring helper contain no Editor.CreateEditor calls;
the temporary audio verification helpers had already been removed.

## Controlled comparison and repair

1. Rebuilt the active Inspector and entered Home Play Mode with previews enabled:
   the same three exceptions recurred.
2. Used the bundled manual's documented Tools / Ultimate Preview / Toggle Ultimate
   Preview control. Both animation and GameObject preview flags became false.
   Entering Play Mode still produced the same three exceptions. This does not
   exclude an earlier extension contribution, since the old objects still existed.
3. Restored the original preview settings. Validated the four exact instance IDs
   again: Editor type, non-persistent, no active Inspector ownership, all targets
   null. Disposed only those four orphan Editor objects with asset destruction
   disabled, then rebuilt the normal Inspector. The damaged GameObjectInspector
   emitted its existing OnDisable exception once during disposal.
4. Cleared the historical Console entries before fresh verification. No exception
   filtering, suppression, recurring cleanup hook, plugin patch or gameplay-code
   rollback was added.

The repair removed only temporary in-memory Inspector state. Unity recreates valid
Inspector editors as needed. No scene objects, assets, progress or audio changes
were deleted. Ultimate Preview remains fully enabled and its settings asset has
no Git diff after the off/on comparison.

## Verification

After cleanup, three Home Play Mode entry/exit cycles and one explicit script
compilation/domain reload completed with zero Console errors. One cycle selected
the real runtime SkateRunnerAudio manager before exiting, exercising destruction
of the selected runtime object. The final orphan Editor count was zero. Animation,
GameObject and scene previews were all enabled. Unity was left out of Play Mode,
not compiling, with the Start Screen scene not dirty. No Editor restart was needed
or performed. A restart would be consistent with clearing this kind of transient
state, but was not independently tested here.

## Attribution limits

The null-target editor state is confirmed; its initial creator is not. Ultimate
Preview's presence is not sufficient evidence that an asset upgrade is required.
Today's edit/test/reload activity could have triggered the stale state, but no
specific audio change has been shown to create it. Do not describe the earlier
asset suspicion as a proven defect or this session cleanup as a vendor-code fix.
