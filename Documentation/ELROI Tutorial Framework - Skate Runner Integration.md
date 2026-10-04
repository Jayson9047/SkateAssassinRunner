# ELROI Tutorial Framework — Skate Runner Integration

## Configured sequences

`Assets/Scenes/SkateRunner.unity` contains 15 reusable lessons and five enabled sequences on `ELROI Tutorial Manager`. Edit copy, targets, gestures and triggers there.

| Sequence | Trigger | Replay policy | Lessons |
|---|---|---|---|
| Level 1 Basic Controls | CurrentLevel = 1 | Once per scene load (preserved) | Jump, Ground Dash, Double Jump, Air Dash, Slide, Ground Slam setup, Down Attack, Destroy Barrels, controls recap |
| Level 2 Powerslam | PowerSlamTutorialReady | Once ever | Five-kill SlamHUD explanation |
| Level 2 Missions | MissionHudTutorialReady | Once ever | Both mission rows, best completion rewards and chance to 4X earnings |
| Phase 2 Introduction | Phase2TutorialReady | Once ever | Meter, ten-second timer, Down Attack button |
| Ruthless Tap Introduction | RuthlessTimerTutorialReady | Once ever | Yellow 3s / Green 4s / Cyan 5s; cash per slash |

Level 1 locks ordinary input between lessons. HUD lessons only block during presentation, and preserve Phase 2's own input lock when dismissed. Stable IDs and the existing `ELROI.Tutorials.v1.` ES3 namespace are preserved.

## Level 1 scenario authoring

The Level 1 obstacle override is Scenario6 → TutorialCashBreather → TutorialSlideSlam → TutorialCashBreather → TutorialBarrelRecap → TutorialCashBreather → Scenario3 → Scenario1 → Scenario2 → Scenario5. Dedicated prefabs live under `Assets/Prefabs/MicroScenarios/` with the `Scenario_` prefix.

The new cash chunk has nine grounded bundles, normal pickup/confetti effects, and a 30-unit pool width to reserve travel time. The slide/enemy chunk separates the gate and enemy by 55.34 units. The later barrel chunk follows five earlier obstacle/enemy encounters and a recovery stretch; its recap marker is 45 units beyond the barrel, followed by another safe cash stretch. The three dedicated chunks are registered in the master pool but excluded from other levels' shuffled selection through `authoredOnlyScenarios` on SkateRunnerObstacleSequenceConfigurator. Authored sequences can still explicitly use them.

| Lesson | Trigger target | Maximum X | Target Y minus player Y |
|---|---|---:|---|
| Jump | Tutorial.JumpTarget | 8 | unrestricted |
| Ground Dash | Tutorial.GroundDashEnemy | 10 | unrestricted |
| Double Jump | Tutorial.DoubleJumpObstacle | 6 | -0.5 to 0.5 |
| Air Dash | Tutorial.AirDashAlignment | 12 | -0.4 to 0.4 |
| Slide | Tutorial.SlideTarget | 8 | 0.05 to 0.3 |
| Ground Slam setup | Tutorial.DownAttackTarget | 16.5 | unrestricted |
| Down Attack | Tutorial.DownAttackTarget | 10 | -12 to -5 |
| Destroy Barrels | Tutorial.BarrelTarget | 10 | -0.3 to 0.4 |
| Controls recap | Tutorial.SafeRecap | 1 | -0.3 to 0.4 |

The air-dash alignment marker sits 0.92 units below the first middle-row cash position to align the player's body with the row. Its spotlight targets the entire CashStack through Tutorial.AirDashTarget. All six teaching bundles have forgiving pickup boxes (local size 0.5 on each axis) to remain reliable as the cash rotates. Collection still runs through Coin's normal single-claim swept pickup: each bundle awards its own cash and spawns VFX_CashBurst. No currency is silently granted.

Slide waits for landing and cues close enough that its 0.8-second slide lasts through the gate. The slam setup-to-attack X spacing times the apex, with a tolerant height band for frame timing variation. The recap is a centered modal with all six Phase 1 controls, an OK button and “Best Of Luck Assassin!”; it waits for the grounded player in the clear recovery lane.

The airborne enemy lesson temporarily lowers CameraFollowTarget's Y offset by 8 to keep the grounded enemy visible. Completion, cancellation/input release and adapter disable restore the original offset. Normal camera settings remain unchanged.

## Production input handoff

The Skate adapter delegates Jump to TapOnlyMainActionZone, DoubleJump to two production taps 0.10 real seconds apart, DashAttack/AirDashAttack to SwipeRightAttackDetector, and DownAttack to SwipeDownDetector. Grounded DownAttack means Slide; airborne after double jump means Ground Slam. These methods bypass the tutorial input lock while retaining the production movement and attack rules.

## Readiness signals and UI

SkateRunnerTutorialVariableProvider publishes change-only readiness signals requiring active gameplay and an idle tutorial manager:

- Powerslam: Level 2, five charges, Phase 1, and 0.2 seconds of normal timescale after kill hit-stop.
- Missions: Level 2, both missions assigned, visible mission HUD, at least 0.5 seconds of Phase 1, and normal timescale settled. It cannot overlap another lesson, including Powerslam.
- Phase 2: HUD transition complete and meter running. Both ticker and countdown freeze during the explanations.
- Ruthless Timer: safe-color award and visible timer, before the camera/tap window starts. Reading the explanation does not spend the unscaled tap window.

SlamHUD's empty parent rect does not cover its controls. Its Platform/TutorialSlamHUDFocus child supplies a configured spotlight rectangle over the button and all five shards. Missions/TutorialMissionHUDFocus frames both visible mission rows, since the Missions parent also has a small unrelated rect. Other lessons target PowerMeterUI, PhaseTimerText, SlamButton and RuthlessTimer directly.

The existing UIPulse animates RuthlessTapTutorialPrompt at speed 26 with unscaled time. The prompt is bright red “TAP! TAP! TAP!” with no cash subtitle. It appears above the timer during live Ruthless mode and does not intercept taps. The player prefab rewards are 3/4/5 seconds.

TutorialPresentation converts Canvas units to screen pixels for placement and reserves room for the OK button/instructions, avoiding overlap with controls and highlighted targets. TargetType.None presents a centered modal, with no spotlight hole or dialogue tail.

## Phase 2 failure resolution

Timeout and Red share a guarded failure path: cancel the ticker without evaluating another result, disable the button, then fire the enemy shot. Late or duplicate clicks cannot cancel failure. Countdown expiration resolves on the zero frame.

PlayerPhase2Controller guarantees fallback death when no execution bullet arrives: 2.5 seconds for the shot, followed by the existing death-animation delay. Legacy serialized zero values also receive the fallback. Projectile collision is no longer required to reach life loss and retry.

## Verification

Unity 6000.0.67f1 live Editor checks:

- Existing framework regressions: 30/30 Edit Mode and 3/3 Play Mode passed; scene validation returned no issues.
- Complete Level 1 through eight gesture prompts and the recap: all six airborne bundles collected, six concurrent cash-confetti effects observed, slide cleared, ground-slam enemy defeated and barrel destroyed. Three lives retained through three seconds after recap OK; input restored.
- Measured playthrough gaps: Air Dash → Slide 8.21 simulated seconds, Slide → Ground Slam setup 4.37 seconds, Ground Slam → barrel 7.87 seconds. Small variations come from normal spawn gaps and frame timing.
- Level 2 mission lesson highlighted both populated rows; five charges did not interrupt it. After dismissal, four charges showed no Powerslam lesson; five showed it. Level 2 shuffled pool contained only Scenario1–5, with no dedicated recovery/teaching chunks.
- No Powerslam lesson at four charges; introduction at five on Level 2.
- Phase 2 ticker/countdown frozen during ordered HUD prompts; gameplay-input lock retained after dismissal.
- Cyan success: timer introduction, camera transition, five-second tap window, accepted taps and cash awards.
- Missing-projectile timeout, Red result, late click and failed retry all reach life loss instead of a stuck state.

Temporary QA traces and screenshots are under Temp/CodexTutorial and Temp/CodexTutorialRevision (ignored; no runtime dependency). Playtest save files are backed up and restored. These are Editor checks using production handlers, not device touch/performance tests.
