using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>
/// Phase 2 car entrance behavior:
/// - Car moves along X using MovingObject (already configured on prefab).
/// - When car is (FrontOffsetX / 2) units ahead of StartingPosition on X, begin diagonal drift by setting Direction.z.
/// - When car reaches StartingPosition.z (within tolerance), stop drift (Direction.z=0) and stop car (Speed=0).
/// </summary>
public class Phase2CarApproachController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform StartingPosition;

    [Header("Tuning")]
    [Tooltip("How far ahead on X the car should Start diagonal drift")]
    [SerializeField] private float diagonalTriggerX = 0.5f;

    [Tooltip("Z direction to set while drifting diagonally. Example: 0.5.")]
    [SerializeField] private float DiagonalZDirection = 0.5f;

    [Tooltip("How close in Z we must be to StartingPosition.z to consider aligned.")]
    [SerializeField] private float ZAlignTolerance = 0.05f;

    [Header("Enemy Type 3 (Shooter)")]
    [SerializeField] private EnemyType3 ShooterEnemyType3;

    private float _previousSignedZDelta;
    private bool _hasPreviousZDelta;

    [Header("Debug")]
    [SerializeField] private bool DebugLogs = false;

    private MovingObject _movingObject;
    private bool _diagonalStarted;
    private bool _locked;

    private void Awake()
    {
        _movingObject = GetComponent<MovingObject>();
        if (ShooterEnemyType3 == null)
        {
            ShooterEnemyType3 = GetComponentInChildren<EnemyType3>(true);
        }
    }

    private void OnEnable()
    {
        // Reset state for pooled reuse
        _diagonalStarted = false;
        _locked = false;
        _hasPreviousZDelta = false;
        _previousSignedZDelta = 0f;
    }

    private void Update()
    {
        if (_locked) return;

        if (StartingPosition == null)
        {
            if (DebugLogs) Debug.LogWarning("[Phase2CarApproachController] StartingPosition not assigned.");
            return;
        }

        if (_movingObject == null)
        {
            if (DebugLogs) Debug.LogWarning("[Phase2CarApproachController] MovingObject not found on car prefab.");
            return;
        }

        // How far ahead of StartingPosition are we on X?
        float aheadX = transform.position.x - StartingPosition.position.x;



        if (!_diagonalStarted && aheadX >= diagonalTriggerX)
        {
            Vector3 dir = _movingObject.Direction;
            dir.z = DiagonalZDirection;
            _movingObject.Direction = dir;
            _diagonalStarted = true;
            ShooterEnemyType3?.StartAiming();
            if (DebugLogs) Debug.Log($"[Phase2CarApproachController] Diagonal started at aheadX={aheadX:F2}. dir={_movingObject.Direction}");
        }

        // 2) Lock when we match StartingPosition.z
        if (_diagonalStarted)
        {
            float targetZ = StartingPosition.position.z;
            float signedDelta = transform.position.z - targetZ;

            // first frame after diagonal starts: initialize previous delta
            if (!_hasPreviousZDelta)
            {
                _previousSignedZDelta = signedDelta;
                _hasPreviousZDelta = true;
                return;
            }

            // Condition A: we got close enough (still useful)
            // Condition B: we crossed the target between frames (sign flip)
            bool closeEnough = Mathf.Abs(signedDelta) <= ZAlignTolerance;
            bool crossedTarget = (_previousSignedZDelta > 0f && signedDelta < 0f) || (_previousSignedZDelta < 0f && signedDelta > 0f) || signedDelta == 0f;

            if (closeEnough || crossedTarget)
            {
                // Clamp exactly to target Z so we never drift off-screen
                Vector3 p = transform.position;
                p.z = targetZ;
                transform.position = p;

                // Stop Z drift
                Vector3 dir = _movingObject.Direction;
                dir.z = 0f;
                _movingObject.Direction = dir;

                // Stop movement
                _movingObject.Speed = 0f;

                _locked = true;

                // --------------------
                // NEW: tell LevelManager Phase 2 "lock moment" happened
                // --------------------
                var lm = MoreMountains.InfiniteRunnerEngine.LevelManager.Instance as MoreMountains.InfiniteRunnerEngine.SkateAssassinRunnerLevelManager;
                if (lm != null)
                {
                    lm.OnEnemyType3LockedInPosition();
                }
                else
                {
                    // If you ever swap level manager class, you'll see this once in console and know why.
                    if (DebugLogs) Debug.LogWarning("[Phase2CarApproachController] LevelManager is not SkateAssassinRunnerLevelManager.");
                }

                if (DebugLogs) Debug.Log($"[Phase2CarApproachController] Locked (cross/close). signedDelta={signedDelta:F3}, pos={transform.position}");
            }
            else
            {
                _previousSignedZDelta = signedDelta;
            }

        }
    }

    // Optional helper if you want to set this at runtime later
    public void SetStartingPosition(Transform start) => StartingPosition = start;
}
