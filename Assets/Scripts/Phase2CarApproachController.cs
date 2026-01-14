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

        // 1) Trigger steering only after we are "ahead" enough (old behavior)
        if (!_slotApproachStarted)
        {
            if (StartingPosition == null)
            {
                // Fallback: if StartingPosition isn't assigned, use TargetSlot as reference.
                // But you SHOULD assign StartingPosition for consistent behavior.
                float fallbackAhead = pos.x - target.x;
                if (fallbackAhead < diagonalTriggerX) return;
            }
            else
            {
                float aheadX = pos.x - StartingPosition.position.x;
                if (aheadX < diagonalTriggerX) return;
            }

            _slotApproachStarted = true;
            ShooterEnemyType3?.StartAiming();

            if (DebugLogs) Debug.Log("[Phase2CarApproachController] Steering started (slot approach).");
        }

        // 2) We only care about X and Z for lane/slot alignment
        float dx = target.x - pos.x;
        float dz = target.z - pos.z;

        bool closeX = Mathf.Abs(dx) <= positionTolerance;
        bool closeZ = Mathf.Abs(dz) <= positionTolerance;

        bool crossedX = false;
        bool crossedZ = false;

        if (_hasPrev)
        {
            crossedX = (_prevDeltaX > 0f && dx < 0f) || (_prevDeltaX < 0f && dx > 0f) || dx == 0f;
            crossedZ = (_prevDeltaZ > 0f && dz < 0f) || (_prevDeltaZ < 0f && dz > 0f) || dz == 0f;
        }

        // 3) Steering (Z ONLY). Never override X forward motion.
        // This prevents the car from flipping direction or changing forward speed behavior.
        if (!closeZ) // if already aligned, stop drifting
        {
            float steerSign = Mathf.Sign(dz);
            steerSign *= -1f;

            Vector3 dir = _movingObject.Direction;
            dir.z = steerSign * Mathf.Abs(zDriftMagnitude);
            _movingObject.Direction = dir;
        }
        else
        {
            Vector3 dir = _movingObject.Direction;
            dir.z = 0f;
            _movingObject.Direction = dir;
        }

        // 4) Lock condition: close enough on both axes OR we crossed target on both axes.
        if ((closeX && closeZ) || (crossedX && crossedZ))
        {
            // Snap to exact slot position (keep Y as-is)
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

        _prevDeltaX = dx;
        _prevDeltaZ = dz;
        _hasPrev = true;
    }

    public void SetTargetSlot(Transform slot) => TargetSlot = slot;
}
