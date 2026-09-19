# Checkpoint Cash Slot — gameplay verification

Prefab: `Assets/Prefabs/MicroScenarios/Scenario_CheckpointCashSlot.prefab`.

The supplied checkpoint gate blocks the road with a tall shutter and a low opening. Cash leads through the opening: read the barrier, slide beneath it, collect the reward. Its industrial frame, bollards and warning markings make it a plausible street checkpoint. This replaces the rejected pipe concepts; those older files were not modified.

## Actual gameplay results

Tested in SkateRunner through the real ObstacleSpawner and MMMultipleObjectPooler, retaining OriginalOrder. Only the test scenario was enabled temporarily. Used the real player's jump and swipe handlers, after spawn invincibility expired.

| Action | Observed outcome |
|---|---|
| Normal jump | Death |
| Double jump | Death |
| Earlier double jump | Death; recorded maximum player Y 7.875 |
| Do nothing | Death |
| Dash | Death after collision fix |
| Slide | Survives; four cash collected with the tested normal timing |
| Earlier slide | Survives; three cash collected |
| Faster slide | Survives; three cash collected |
| Faster dash / double jump | Death |

Normal LevelManager speed was approximately 20; faster cases used 40. The existing slide speed boost remained active. These are sampled input timings, not exhaustive timing or device coverage.

## Visual review and iteration

Inspected actual gameplay-camera captures. Reduced the source gate's width, increased its height to block the observed jump envelope, and positioned the shutter with approximately 1.2 units of visual slide clearance. The frame is approximately 12 units tall. The tall silhouette communicates a barrier rather than a jumpable hurdle.

The initial viewing angle let a post hide the slide route. Rotated the gate and its hazards together by 25 degrees around the encounter pivot, then repeated the gameplay tests. The final slide capture shows the player beneath the shutter between connected frame supports. Cash spacing was adjusted after the first collection test. The frame top clips the camera at close range; the lower opening stays readable. I consider this a coherent, playable encounter for review: a single recognizable object, an obvious rewarded route and a verified alternative to blind double jumping. Personal timing comfort and device performance still need user playtesting.

The source CheckpointGate prefab is unchanged. Its corrected model rotation is retained on the nested visual; the scenario root has zero rotation and unit scale for spawning. Its frame and shutter were already separate meshes, so Blender separation was unnecessary. This is a static gate encounter, not an animated shutter.

Reused Scenario1's moving/pooling/recycling/reset configuration and existing slide-aware hazard configuration; cash copies Scenario2's existing pickup settings. No vehicle is used and no additional model is required.

## Dash-through collision fix

Reproduced dash passing completely through the shutter alive. SwipeRightAttackDetector interpolates Transform.position during the dash, allowing the body to cross a thin trigger between physics samples.

Changed `Assets/ThirdParty/InGame/InfiniteRunnerEngine/Common/Scripts/Enemies/KillsPlayerOnTouch.cs` to supplement native callbacks with a relative swept box intersection in LateUpdate. The new `Assets/Scripts/SweptBoxIntersection.cs` checks overlap across the movement interval, accounting for both player and obstacle translation. Dispatch uses the existing virtual TriggerEnter, preserving slide/down-attack exceptions and invincibility. Layer exclusions and ignored collider pairs are respected; sweep history resets on hazard enable/disable for pooling.

This covers 3D BoxCollider trigger hazards using KillsPlayerOnTouch and its existing subclasses against the player's BoxCollider. It does not claim continuous angular collision, MeshCollider, sphere or 2D coverage. No player ability implementation or tuning was changed. This is a project patch to Infinite Runner Engine 2.1 and should be preserved when upgrading that package.

78 deterministic swept-box geometry checks passed, including crossing in both directions, stationary overlap, clear overhead/depth misses and rotated boxes. Before the request to stop further obstacle testing, an existing Scenario3 hot-dog cart fixture also completed: normal/faster dash killed the player, while double jump cleared it. No additional obstacle tests were run after that request.

## Evidence and restoration

Selected real gameplay captures and action/result logs are in `Documentation/CheckpointPlaytest`. `final-slide-action.png` shows the intended route; `final-dash-outcome.png` shows the corrected death; `v1-dash-action.png` records the original bug. Reproducible gate test harness source and geometry check source are preserved as `.cs.txt`, outside Assets. Temporary executable helpers and the cart fixture were removed.

Original pool and sequence-configurator serialized settings matched their saved pre-test JSON exactly after restoration. Six original scenario entries are enabled; original disabled vehicle entries remain disabled. The new scenario is excluded from normal rotation. Original saved scene was reopened without saving test changes.

SHA256 hashes verified unchanged for SkateRunner.unity, ObstacleSpawner.prefab and the supplied CheckpointGate.prefab. Backgrounds, UI, audio, progression and player ability scripts were untouched. The final gameplay tests compiled successfully; the console retained three pre-existing null-object inspector errors, not gameplay or compilation failures. No device performance profiling was performed.

Deliverable changes: the new scenario prefab and metadata, the shared hazard script patch, SweptBoxIntersection and metadata, this report and selected test evidence.
