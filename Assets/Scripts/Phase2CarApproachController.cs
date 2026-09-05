using System;
using System.Collections;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>
/// Phase 2 car approach (collision-driven, no estimation):
/// - Start drifting ONLY when colliding with StartDriftMarker.
/// - Stop ONLY when colliding with CarSlotMarker.
/// - Drift is time-based Z animation (nice slide), never Direction.z.
/// - No snapping to slot position (optional tiny settle available).
/// </summary>
[RequireComponent(typeof(Collider))]
public class Phase2CarApproachController : MonoBehaviour
{
    /// <summary>Raised once when this pooled car actually reaches the drift marker.</summary>
    public static event Action OnPhase2ApproachStarted;

    [Header("References")]
    [SerializeField] private Transform TargetSlot;              // used only for drift target Z (and optional settle)
    [SerializeField] private EnemyType3 ShooterEnemyType3;

    [Header("Drift Feel")]
    [SerializeField] private float driftDurationSeconds = 1.6f; // how long to drift into lane
    [SerializeField] private AnimationCurve driftEase;          // optional (SmoothStep used if null)

    [Header("Optional Polish (off by default)")]
    [Tooltip("If > 0, after stopping on slot collision, we gently settle Z (and optionally X) to TargetSlot over this time. 0 = no settle.")]
    [SerializeField] private float settleSeconds = 0f;

    [Tooltip("If true, settle X toward TargetSlot.x as well (useful if your slot marker is a little thick).")]
    [SerializeField] private bool settleXToSlot = false;

    [Header("Debug")]
    [SerializeField] private bool DebugLogs = false;

    private MovingObject _movingObject;
    private Transform _root;         // the transform we drift/stop (MovingObject root)

    private bool _driftStarted;
    private bool _locked;

    private float _driftStartTime;
    private float _driftFromZ;
    private float _driftToZ;

    private Coroutine _settleCo;

    private void Awake()
    {
        // Works whether this script is on root or a child collider object
        _movingObject = GetComponent<MovingObject>() ?? GetComponentInParent<MovingObject>();
        _root = _movingObject != null ? _movingObject.transform : transform;
    }

    private void OnEnable()
    {
        _driftStarted = false;
        _locked = false;

        // Pool safety
        if (_movingObject == null) _movingObject = GetComponentInParent<MovingObject>();
        _root = _movingObject != null ? _movingObject.transform : transform;

        if (_settleCo != null)
        {
            StopCoroutine(_settleCo);
            _settleCo = null;
        }
    }

    public void SetTargetSlot(Transform slot) => TargetSlot = slot;

    private void Update()
    {
        if (_locked) return;
        if (TargetSlot == null) return;

        // Keep MovingObject purely forward on X (never inject Z into Direction)
        if (_movingObject != null)
        {
            var dir = _movingObject.Direction;
            dir.y = 0f;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
            _movingObject.Direction = dir.normalized;
        }

        // Drift Z over time (visual lane change)
        if (_driftStarted)
        {
            float t = Mathf.Clamp01((Time.time - _driftStartTime) / Mathf.Max(0.0001f, driftDurationSeconds));

            float easedT = (driftEase != null && driftEase.length > 0)
                ? Mathf.Clamp01(driftEase.Evaluate(t))
                : (t * t * (3f - 2f * t)); // SmoothStep

            Vector3 p = _root.position;
            p.z = Mathf.Lerp(_driftFromZ, _driftToZ, easedT);
            _root.position = p;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_locked) return;

        // Start drift EXACTLY when we hit the StartDriftMarker collider
        if (!_driftStarted && other.CompareTag("StartDriftMarker"))
        {
            BeginDrift();
            return;
        }

        // Stop EXACTLY when we hit the CarSlotMarker collider
        if (other.CompareTag("CarSlotMarker"))
        {
            StopAndNotify();
            return;
        }
    }

    private void BeginDrift()
    {
        _driftStarted = true;
        _driftStartTime = Time.time;

        _driftFromZ = _root.position.z;
        _driftToZ = TargetSlot.position.z;

        OnPhase2ApproachStarted?.Invoke();

        ShooterEnemyType3?.StartAiming();

        if (DebugLogs)
            Debug.Log($"[Phase2CarApproachController] Drift started. fromZ={_driftFromZ}, toZ={_driftToZ}, dur={driftDurationSeconds}");
    }

    private void StopAndNotify()
    {
        if (_locked) return;

        // Stop forward motion immediately (no snapping position)
        if (_movingObject != null)
        {
            _movingObject.Speed = 0f;
            _movingObject.Direction = Vector3.zero;
        }

        _locked = true;

        // Optional: small visual settle toward TargetSlot (pure polish)
        if (settleSeconds > 0f && TargetSlot != null)
        {
            _settleCo = StartCoroutine(SettleToSlotCo());
        }

        var lm = LevelManager.Instance as SkateAssassinRunnerLevelManager;
        if (lm != null)
        {
            lm.RegisterPhase2Car(_root);
            lm.OnEnemyType3LockedInPosition();
        }
        else if (DebugLogs)
        {
            Debug.LogWarning("[Phase2CarApproachController] LevelManager is not SkateAssassinRunnerLevelManager.");
        }

        if (DebugLogs)
            Debug.Log($"[Phase2CarApproachController] STOPPED on slot collision. pos={_root.position}");
    }

    private IEnumerator SettleToSlotCo()
    {
        Vector3 start = _root.position;
        Vector3 target = TargetSlot.position;

        // We only ever care about Z visually; X settle is optional
        float startX = start.x;
        float targetX = settleXToSlot ? target.x : start.x;

        float startZ = start.z;
        float targetZ = target.z;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, settleSeconds);

            Vector3 p = _root.position;
            p.x = Mathf.Lerp(startX, targetX, t);
            p.z = Mathf.Lerp(startZ, targetZ, t);
            _root.position = p;

            yield return null;
        }
    }
}
