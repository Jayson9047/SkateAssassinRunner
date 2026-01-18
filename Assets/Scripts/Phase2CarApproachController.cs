using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>
/// Phase 2 car entrance behavior (deterministic + old-style trigger steering):
/// - Car moves normally using MovingObject (we do NOT interfere at first).
/// - Once we pass a trigger point (aheadX >= diagonalTriggerX), we start steering toward TargetSlot.
/// - Steering ONLY adjusts Z (sideways drift). We never override X forward direction.
/// - When within tolerance (or crosses target), snaps exactly to slot (X/Z only) and stops.
/// - Triggers ShooterEnemyType3.StartAiming() once when steering begins.
/// </summary>
public class Phase2CarApproachController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform TargetSlot;

    [Tooltip("Reference point used to decide when to START steering (matches your old behavior).")]
    [SerializeField] private Transform StartingPosition;

    [Header("Steering Trigger")]
    [Tooltip("We start steering only when (transform.position.x - StartingPosition.position.x) >= diagonalTriggerX.")]
    [SerializeField] private float diagonalTriggerX = 0.5f;

    [Header("Tuning")]
    [Tooltip("How close we must be (per-axis) to snap to TargetSlot.")]
    [SerializeField] private float positionTolerance = 0.05f;

    [Tooltip("Sideways drift amount while steering (Z only).")]
    [SerializeField] private float zDriftMagnitude = 0.6f;

    [Header("Enemy Type 3 (Shooter)")]
    [SerializeField] private EnemyType3 ShooterEnemyType3;

    [Header("Steering Start Marker (World)")]
    [SerializeField] private Transform StartDriftMarker;

    [Header("Debug")]
    [SerializeField] private bool DebugLogs = false;

    private MovingObject _movingObject;
    private bool _locked;

    private bool _slotApproachStarted;

    private float _prevDeltaX;
    private float _prevDeltaZ;
    private bool _hasPrev;

    private void Awake()
    {
        _movingObject = GetComponent<MovingObject>();
        if (_movingObject == null)
        {
            // Some prefabs put MovingObject on root/pickup; if so, adjust this to GetComponentInParent.
            _movingObject = GetComponentInParent<MovingObject>();
        }
    }

    private void OnEnable()
    {
        _locked = false;
        _hasPrev = false;
        _slotApproachStarted = false;

        if (_movingObject == null)
        {
            if (DebugLogs) Debug.LogWarning("[Phase2CarApproachController] MovingObject not found.");
            return;
        }

        // Ensure direction isn't zero so it starts moving
        if (_movingObject.Direction == Vector3.zero)
            _movingObject.Direction = Vector3.right;
    }

    private void Update()
    {
        if (_locked) return;
        if (TargetSlot == null || _movingObject == null) return;

        Vector3 pos = transform.position;
        Vector3 target = TargetSlot.position;

        // -----------------------------
        // 1) Start steering deterministically
        // -----------------------------
        if (!_slotApproachStarted)
        {
            // RECOMMENDED: use a world marker to start the Z drift.
            // Add: [SerializeField] private Transform StartDriftMarker;
            // Place it in the scene where you want the car to begin drifting toward TargetSlot.
            if (StartDriftMarker == null)
            {
                // Fallback to old behavior if you haven't assigned the marker yet
                // (but this is the part that can be inconsistent).
                if (StartingPosition == null) return;

                float aheadX = pos.x - StartingPosition.position.x;
                if (aheadX < diagonalTriggerX) return;
            }
            else
            {
                // Deterministic trigger: start steering once car crosses marker's X
                // (assumes car moves in +X direction)
                if (pos.x < StartDriftMarker.position.x) return;
            }

            _slotApproachStarted = true;
            ShooterEnemyType3?.StartAiming();

            if (DebugLogs) Debug.Log("[Phase2CarApproachController] Steering started.");
        }

        // -----------------------------
        // 2) Z steering ONLY (never override X forward motion)
        // -----------------------------
        float dz = target.z - pos.z;
        bool closeZ = Mathf.Abs(dz) <= positionTolerance;

        Vector3 dir = _movingObject.Direction;

        if (!closeZ)
        {
            // We want to move toward target.z
            // If dz > 0 => target is "positive z" relative to us => we need +z drift.
            // If dz < 0 => we need -z drift.
            dir.z = -Mathf.Sign(dz) * Mathf.Abs(zDriftMagnitude);
        }
        else
        {
            dir.z = 0f;
        }

        _movingObject.Direction = dir;

        // -----------------------------
        // 3) Deterministic lock condition
        // -----------------------------
        // We lock when:
        // - We have reached/passed the target X (since X is driven by MovingObject speed)
        // - AND we are aligned on Z within tolerance
        //
        // This removes "sometimes" caused by per-frame overshoot and crossed-delta logic.
        bool reachedOrPassedX = pos.x >= target.x; // assumes car moves +X

        if (reachedOrPassedX && closeZ)
        {
            // Snap exactly to target slot (keep current Y)
            pos.x = target.x;
            pos.z = target.z;
            transform.position = pos;

            // Stop movement
            _movingObject.Speed = 0f;
            _movingObject.Direction = Vector3.zero;

            _locked = true;

            // Notify LevelManager exactly like before
            var lm = MoreMountains.InfiniteRunnerEngine.LevelManager.Instance
                as MoreMountains.InfiniteRunnerEngine.SkateAssassinRunnerLevelManager;

            if (lm != null)
            {
                lm.RegisterPhase2Car(transform);
                lm.OnEnemyType3LockedInPosition();
            }
            else
            {
                if (DebugLogs) Debug.LogWarning("[Phase2CarApproachController] LevelManager is not SkateAssassinRunnerLevelManager.");
            }

            if (DebugLogs)
                Debug.Log($"[Phase2CarApproachController] LOCKED at slot. pos={transform.position}");

            return;
        }
    }

    public void SetTargetSlot(Transform slot) => TargetSlot = slot;
}
