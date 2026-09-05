using DG.Tweening;
using MoreMountains.InfiniteRunnerEngine;
using System.Collections;
using UnityEngine;

public class Phase2PowerSlamFrameEvents : MonoBehaviour
{
    [Header("Tween")]
    [SerializeField] private float launchDuration = 0.45f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Execution Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string strikeTakeoffTrigger = "StrikeTakeoff"; // you create this in Animator
    [SerializeField] private string upperBodyLayerName = "KatanaLayer";

    [Header("Execution Rotation")]
    [SerializeField] private Vector3 executionEuler = new Vector3(-55f, 95f, -25f);
    [SerializeField] private float rotationTweenDuration = 0.01f;


    [Header("StrikeTakeoff Clip (needed for frame sync)")]
    [SerializeField] private AnimationClip strikeTakeoffClip;
    [SerializeField] private int arriveAtMeetPointByFrame = 6;   // configurable (frame N)

    [Header("Positions")]
    [SerializeField] private Transform playerMeetPoint;
    [SerializeField] private Transform playerStrikeEndPoint;     // dash end point

    [Header("Phase 2 Presentation")]
    [SerializeField] private Phase2CameraDirector cameraDirector;
    [SerializeField] private Phase2SpeedlinesController phase2Speedlines;
    [SerializeField] private Phase2FinalStrikeFeedback finalStrikeFeedback;

    [Header("Move-to-Line")]
    [SerializeField] private Ease meetMoveEase = Ease.OutQuad;

    [Header("Dash (XY)")]
    [SerializeField] private float dashSpeed = 10f;              // units/sec
    [SerializeField] private Ease dashEase = Ease.OutCubic;
    [SerializeField] private bool keepZ = true;                  // usually yes in your runner

    [SerializeField] private float lockedKatanaWeight = 0f;

    [Header("Frame 18 Failsafe (Generous)")]
    [SerializeField] private string downAttackStateName = "DownAttack";   // base layer state name
    [SerializeField] private AnimationClip downAttackClip;                 // clip that has frame 18 event
    [SerializeField] private int triggerAtFrame = 18;                      // default 18
    [SerializeField] private int graceFramesEarly = 2;                     // allow 16+
    [SerializeField] private float armedFailsafeSeconds = 0.20f;           // if state name mismatch, still fire

    [Header("Frame 18 Failsafe (Never Early)")]
    [SerializeField] private int frame18 = 18;
    [SerializeField] private float extraGraceSeconds = 0.03f;   // small cushion for frame timing / hiccups

    [SerializeField] private bool debugAlignCamera = false;

    [Header("Slow Motion")]
    [SerializeField] private float slowMoScale = 0.25f;
    [SerializeField] private float slowMoDurationRealtime = 0.20f;
    [SerializeField] private bool slowMoAffectsPhysics = true;

    [Header("Slow Motion Reward / Sync")]
    [SerializeField] private float slowMoDurationYellow = 5f;
    [SerializeField] private float slowMoDurationGreen = 6.5f;
    [SerializeField] private float slowMoDurationCyan = 8f;
    [SerializeField] private float ruthlessTapStartDelayRealtime = 0f; // default 0
    [SerializeField] private float ruthlessLeadInRealtime = 3f;

    private PowerMeter.ZoneResult cachedZoneResult = PowerMeter.ZoneResult.Yellow;
    private float _slowMoStartUnscaledTime;
    private float _slowMoDurationRealtimeActive;

    private GameObject _enemyInstance;

    private bool _ruthlessTapStarted;
    private Coroutine _ruthlessTapCo;

    private float _defaultFixedDeltaTime;
    private bool _slowMoActive;


    private float _armedAtTime;
    private bool _frame18FailsafeArmed;

    private bool _frame18FailsafeFired;
    private float _triggerNormEarly = 0.6f; // computed


    private bool _lockKatanaLayerWeight;
    private int _upperBodyLayerIndex = -1;

    private bool _armedForThisSlam;
    private bool _launchedThisSlam;
    private bool _finalStrikeFeedbackPlayedThisSlam;
    private bool _warnedMissingSpeedlines;
    private bool _warnedMissingFinalStrikeFeedback;
    private Tween _tween;
    private Tween _meetTween;
    private Tween _dashTween;

    private Transform _root;
    private Rigidbody _rootRb;

    private Vector3 _cachedBodyLocalPos;
    private Quaternion _cachedBodyLocalRot;
    private void OnEnable()
    {
        // Pool-safe reset
        _launchedThisSlam = false;
        _finalStrikeFeedbackPlayedThisSlam = false;
        _frame18FailsafeFired = false;
        _defaultFixedDeltaTime = Time.fixedDeltaTime;

        _tween?.Kill();
        _tween = null;

        _dashTween?.Kill();
        _meetTween?.Kill();

        _dashTween = null;
        _meetTween = null;

        _root = transform.root;
        _rootRb = _root.GetComponent<Rigidbody>();

        _cachedBodyLocalPos = transform.localPosition;
        _cachedBodyLocalRot = transform.localRotation;
        ResolvePresentationReferences();
    }

    public void ResetExecutionAttempt()
    {
        _armedForThisSlam = false;
        _launchedThisSlam = false;
        _finalStrikeFeedbackPlayedThisSlam = false;
        phase2Speedlines?.HideImmediate();

        _tween?.Kill(); _tween = null;
        _meetTween?.Kill(); _meetTween = null;
        _dashTween?.Kill(); _dashTween = null;

        _lockKatanaLayerWeight = false;

        // Optional: restore layer to normal gameplay weight (if you want)
        if (animator != null && _upperBodyLayerIndex >= 0)
            animator.SetLayerWeight(_upperBodyLayerIndex, 1f);
        _ruthlessTapStarted = false;
        if (_ruthlessTapCo != null) { StopCoroutine(_ruthlessTapCo); _ruthlessTapCo = null; }
    }
    public void SetEnemyInstance(GameObject enemyInstance)
    {
        _enemyInstance = enemyInstance;
    }


    /// <summary>
    /// Call this ONLY when the PHASE2 down-attack button starts the special slam.
    /// This arms the frame-18 animation event to do the launch.
    /// </summary>
    public void ArmPhase2LaunchForNextSlam()
    {
        _armedForThisSlam = true;
        _launchedThisSlam = false;
        _finalStrikeFeedbackPlayedThisSlam = false;
        ResolvePresentationReferences();
        phase2Speedlines?.HideImmediate();

        _armedAtTime = Time.time;
        _frame18FailsafeArmed = true;

        _ruthlessTapStarted = false;
        if (_ruthlessTapCo != null) { StopCoroutine(_ruthlessTapCo); _ruthlessTapCo = null; }

        if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] ARMED");
    }



    public void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        _upperBodyLayerIndex = animator != null ? animator.GetLayerIndex(upperBodyLayerName) : -1;

        if (downAttackClip != null && downAttackClip.frameRate > 0f)
        {
            float totalFrames = downAttackClip.length * downAttackClip.frameRate;
            float targetFrame = Mathf.Clamp(triggerAtFrame - graceFramesEarly, 0, totalFrames);
            _triggerNormEarly = Mathf.Clamp01(targetFrame / Mathf.Max(1f, totalFrames));
        }

    }
    private void LateUpdate()
    {
        if (_lockKatanaLayerWeight)
            ForceKatanaLayerWeight();
    }



    /// <summary>
    /// Animation Event on frame 18 of the slam clip.
    /// </summary>
    public void OnPhase2PowerSlamFrame18()
    {
        _frame18FailsafeArmed = false;

        if (!_armedForThisSlam)
        {
            if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] Frame18 fired but NOT armed (ignored)");
            return;
        }

        if (_launchedThisSlam)
        {
            if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] Frame18 fired but already launched (ignored)");
            return;
        }

        // Consume arm so other downattacks don't trigger
        _armedForThisSlam = false;
        _launchedThisSlam = true;

        if (playerMeetPoint == null)
        {
            Debug.LogError("[Phase2PowerSlamFrameEvents] playerMeetPoint is NULL.");
            return;
        }

        // 1) switch pose/state
        if (animator != null && !string.IsNullOrEmpty(strikeTakeoffTrigger))
        {
            animator.ResetTrigger(strikeTakeoffTrigger);
            animator.SetTrigger(strikeTakeoffTrigger);
        }
        // Track slowmo window in realtime/unscaled time so we can compute what's left later

        float ruthlessDuration = GetRewardedSlowMoDuration(); // now means ruthless duration
        float totalSlowMo = ruthlessLeadInRealtime + ruthlessDuration;

        _slowMoDurationRealtimeActive = totalSlowMo;
        _slowMoStartUnscaledTime = Time.unscaledTime;

        SkateRunnerAudioManager.PlayPhase2SlowMotionStart();
        
SkateRunnerGameFeel.TriggerSlowMoStatic(slowMoScale, totalSlowMo, slowMoAffectsPhysics);


        // optional: kill upper body aiming layer during execution
        if (_upperBodyLayerIndex >= 0 && animator != null)
        {
            _lockKatanaLayerWeight = true;
            ForceKatanaLayerWeight();
        }

        // Aggressive execution pose rotation (very fast, not snapped)
        transform.DOLocalRotate(
            executionEuler,
            rotationTweenDuration,
            RotateMode.Fast
        ).SetEase(Ease.OutQuad);

        _cachedBodyLocalPos = transform.localPosition;
        _cachedBodyLocalRot = transform.localRotation;
        StartMoveToMeetPointSynced();
    }


    private void Update()
    {
        // If event already happened or we're not armed, do nothing
        if (!_frame18FailsafeArmed || !_armedForThisSlam || _launchedThisSlam)
            return;

        if (downAttackClip == null || downAttackClip.frameRate <= 0f)
            return; // can't compute frame 18 time safely; assign the clip

        // Compute the earliest allowed moment to behave like frame 18:
        float frame18Time = (frame18 / downAttackClip.frameRate) + extraGraceSeconds;

        // IMPORTANT: This can NEVER fire early because it's time-based from the moment you armed the slam.
        if (Time.time - _armedAtTime >= frame18Time)
        {
_frame18FailsafeArmed = false;

            if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] Frame18 FAILSAFE fired (time-based, never early)");

            // Reuse your proven pathway
            OnPhase2PowerSlamFrame18();
        }
    }


    private void StartMoveToMeetPointSynced()
    {
        if (playerMeetPoint == null)
        {
            Debug.LogError("[Phase2] playerMeetPoint is NULL.");
            return;
        }
        if (strikeTakeoffClip == null)
        {
            Debug.LogError("[Phase2] strikeTakeoffClip not assigned (need it to compute frame duration).");
            return;
        }

        _meetTween?.Kill();

        // How long until we must arrive at meet point:
        // duration = (arriveFrame / clipFrameRate) seconds
        float fps = strikeTakeoffClip.frameRate;
        float duration = Mathf.Max(0.001f, arriveAtMeetPointByFrame / fps);

        // Move BODY (visual) to meet line
        _meetTween = transform
            .DOMove(playerMeetPoint.position, duration)
            .SetEase(meetMoveEase);
    }

    private void ResyncRootToBodyAndRestoreHierarchy()
    {
        if (_root == null) return;

        // We want: body world position stays where it is right now,
        // but we move the ROOT under it so local offset returns to normal.

        Vector3 bodyWorld = transform.position;

        // Convert cached local offset into world space using root rotation
        Vector3 worldOffset = _root.TransformVector(_cachedBodyLocalPos);

        Vector3 newRootPos = bodyWorld - worldOffset;

        // Move root (Rigidbody-safe)
        if (_rootRb != null && !_rootRb.isKinematic)
        {
            _rootRb.position = newRootPos;
            _rootRb.linearVelocity = Vector3.zero; // optional
        }
        else
        {
            _root.position = newRootPos;
        }

        // Restore body to its normal local placement under root
        transform.localPosition = _cachedBodyLocalPos;
        transform.localRotation = _cachedBodyLocalRot;
    }

    public void OnPhase2StrikeDashStart()
    {
        if (_ruthlessTapStarted) return;
        _ruthlessTapStarted = true;

        if (_ruthlessTapCo != null) StopCoroutine(_ruthlessTapCo);
        _ruthlessTapCo = StartCoroutine(CoBeginRuthlessTapSyncedToSlowMo());
    }

    private IEnumerator CoBeginRuthlessTapSyncedToSlowMo()
    {
        // Optional additional delay (default 0). Uses realtime so it doesn't get affected by slowmo scale.
        if (ruthlessTapStartDelayRealtime > 0f)
            yield return new WaitForSecondsRealtime(ruthlessTapStartDelayRealtime);

        // Switch camera, then arm speedlines. The controller waits for the real
        // Cinemachine live/no-blend state rather than a guessed delay.
        ResolvePresentationReferences();
        cameraDirector?.SwitchToCollision();

        // Set target for ruthless tap cash popups
        if (_enemyInstance != null)
        {
            LevelManager.Instance?.SetRuthlessTapTarget(_enemyInstance.transform);
        }

        LevelManager.Instance?.EnterRuthlessTapMode();

        float ruthlessDuration = GetRewardedSlowMoDuration(); // exact tap window
        float remaining = ruthlessDuration;                   // full window at start
        float totalSlowMo = ruthlessLeadInRealtime + ruthlessDuration;

        SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.Phase2BeginRuthlessTapCountdown(ruthlessDuration, ruthlessDuration);

        RuthlessTapModeController.Instance.Begin(remaining, taps =>
        {
            phase2Speedlines?.HideImmediate();
            LevelManager.Instance?.ExitRuthlessTapMode();
            Debug.Log("Final combo taps: " + taps);

            StartDashToStrikeEnd(fromCurrentPosition: true, onArrive: () =>
            {
                var enemyGo = _enemyInstance;
                if (enemyGo == null)
                {
                    Debug.LogError("[Phase2PowerSlamFrameEvents] Enemy instance is NULL. SetEnemyInstance() was never called.");
                    return;
                }

                var dmg = enemyGo.GetComponentInParent<IndieKit.IDamageable>();
                if (dmg != null)
                {
                    Vector3 hitPoint = enemyGo.transform.position;
                    dmg.ApplyDamage(999f, hitPoint, true);
                }
                else
                {
                    Debug.LogError("[Phase2PowerSlamFrameEvents] No IDamageable found in parent chain of enemy instance.");
                }

                if (LevelManager.Instance != null)
                    LevelManager.Instance.FreezeSpeedAndCancelBoost();
            });

            cameraDirector?.SwitchToFollow();
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.FadeOutPowerMeterWhenPlayerGrounded();

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.FreezeSpeedAndCancelBoost();
            }

            StartCoroutine(ShowLevelEndAfterDelayCo());
        });

        // Begin() has made the mode authoritative before the controller starts
        // watching the camera, so an immediate mode end cannot leave a stale request.
        phase2Speedlines?.ShowWhenCollisionCameraSettled();
        if (phase2Speedlines == null && !_warnedMissingSpeedlines)
        {
            _warnedMissingSpeedlines = true;
            Debug.LogWarning("[Phase 2 Presentation] Speedlines controller is unavailable; Phase 2 gameplay continues.", this);
        }
    }


    private IEnumerator ShowLevelEndAfterDelayCo()
    {
        SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor?.NotifyLevelWon();
        //delay to allow for pose to finish
        yield return new WaitForSecondsRealtime(6f);
        SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.ShowLevelEndScreen(true);
    }


    private void StartDashToStrikeEnd(bool fromCurrentPosition = false, System.Action onArrive = null)
    {
if (playerStrikeEndPoint == null)
        {
            Debug.LogError("[Phase2] playerStrikeEndPoint is NULL.");
            return;
        }

        SkateRunnerAudioManager.PlayRuthlessFinalCut();

        if (!_finalStrikeFeedbackPlayedThisSlam)
        {
            _finalStrikeFeedbackPlayedThisSlam = true;
            ResolvePresentationReferences();
            if (finalStrikeFeedback != null)
            {
                finalStrikeFeedback.Play(playerMeetPoint != null ? playerMeetPoint.position : transform.position,
                                         playerStrikeEndPoint.position);
            }
            else if (!_warnedMissingFinalStrikeFeedback)
            {
                _warnedMissingFinalStrikeFeedback = true;
                Debug.LogWarning("[Phase 2 Presentation] Final Strike feedback is unavailable; the execution dash continues.", this);
            }
        }

        
_dashTween?.Kill();

        Vector3 start = transform.position; // always current
        Vector3 end = playerStrikeEndPoint.position;

        if (keepZ) end.z = start.z;

        float dist = Vector3.Distance(start, end);
        float duration = Mathf.Max(0.001f, dist / Mathf.Max(0.01f, dashSpeed));

        if (_meetTween != null && _meetTween.IsActive() && _meetTween.IsPlaying())
            _meetTween.Kill(false);

        _dashTween = transform
            .DOMove(end, duration)
            .SetEase(dashEase)
            .OnComplete(() =>
            {
                ResyncRootToBodyAndRestoreHierarchy();
                onArrive?.Invoke();
            });
    }

    public void SetPhase2PowerMeterResult(PowerMeter.ZoneResult result)
    {
        cachedZoneResult = result;
    }

    private float GetRewardedSlowMoDuration()
    {
        switch (cachedZoneResult)
        {
            case PowerMeter.ZoneResult.Cyan:
                return slowMoDurationCyan;
            case PowerMeter.ZoneResult.Green:
                return slowMoDurationGreen;
            case PowerMeter.ZoneResult.Yellow:
            default:
                return slowMoDurationYellow;
        }
    }
    public float PeekRewardedSlowMoDuration()
    {
        return GetRewardedSlowMoDuration();
    }

    private void ForceKatanaLayerWeight()
    {
        if (animator == null || _upperBodyLayerIndex < 0) return;

        float current = animator.GetLayerWeight(_upperBodyLayerIndex);
        if (!Mathf.Approximately(current, lockedKatanaWeight))
            animator.SetLayerWeight(_upperBodyLayerIndex, lockedKatanaWeight);
    }

    private void ResolvePresentationReferences()
    {
        if (cameraDirector == null) cameraDirector = Phase2CameraDirector.ActiveInstance;
        if (phase2Speedlines == null) phase2Speedlines = Phase2SpeedlinesController.ActiveInstance;
        if (finalStrikeFeedback == null) finalStrikeFeedback = Phase2FinalStrikeFeedback.ActiveInstance;
    }

    private void OnDisable()
    {
        phase2Speedlines?.HideImmediate();
    }


}
