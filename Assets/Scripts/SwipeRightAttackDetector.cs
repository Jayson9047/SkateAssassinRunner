using System.Collections;
using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

public class SwipeRightAttackDetector : MonoBehaviour
{
    [Header("Swipe Settings")]
    public float minSwipeDistance = 100f; // pixels
    public float maxSwipeTime = 0.5f;

    [Header("Keyboard Debug")]
    public KeyCode swipeRightKey = KeyCode.D;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Animator layer name that holds the katana overlay.")]
    [SerializeField] private string katanaLayerName = "KatanaLayer";

    [SerializeField] private Transform startingPosition;
    private SwipeDownDetector swipeDownDetector;

    [Header("Dash (X Axis)")]
    public float dashDistanceX = 1.0f;
    public float dashDuration = 0.08f;
    public float returnDuration = 0.12f;
    public bool useLocalX = false;

    public bool IsAttacking => isInAttackDash || isReturning;

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private Jumper jumper;
    private int katanaLayerIndex = -1;

    // State split
    private bool isInAttackDash;   // dash phase (hard lock)
    private bool isReturning;      // returning phase (interruptible)
    private Coroutine dashRoutine;

    [Header("Attack Timing Gate")]
    [SerializeField] private string attackStateName = "DashAttack"; // animator state name in Base Layer
    [SerializeField] private int AttackStartFrame = 15;             // frame to start movement
    [SerializeField] private float dashStartExtraDelay = 0f;         // optional delay after frame gate
    private float attackMoveStartNormalizedTime = 1f;                // computed

    // ---- Gravity suspend support (3D rigidbody) ----
    [Header("Air Dash Gravity Control")]
    [SerializeField] private bool suspendGravityDuringAirDash = true;

    private Rigidbody rb3D;
    private bool gravitySuspended;
    private bool savedUseGravity;
    private float savedYVelocity;

    private void Awake()
    {
        swipeDownDetector = GetComponent<SwipeDownDetector>();
        if (swipeDownDetector == null)
        {
            Debug.LogWarning("SwipeRightAttackDetector: SwipeDownDetector not found.");
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);
            if (katanaLayerIndex < 0)
            {
                Debug.LogWarning($"SwipeRightAttackDetector: Animator layer '{katanaLayerName}' not found.");
            }
        }
        else
        {
            Debug.LogWarning("SwipeRightAttackDetector: Animator reference is missing.");
        }

        jumper = GetComponent<Jumper>();
        if (jumper == null)
        {
            Debug.LogError("SwipeRightAttackDetector: Jumper component not found on player.");
        }

        rb3D = GetComponent<Rigidbody>(); // if your player uses Rigidbody for gravity
    }

    private void Update()
    {
        // Your original normalized time calc (kept)
        attackMoveStartNormalizedTime = AttackStartFrame / 36f; // assuming 36 frames (your current assumption)

        // Keyboard test
        if (Input.GetKeyDown(swipeRightKey))
        {
            OnSwipeRight();
        }

        // Touch
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            startPos = touch.position;
            startTime = Time.time;
            tracking = true;
        }

        if (!tracking) return;

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            float duration = Time.time - startTime;
            Vector2 delta = touch.position - startPos;

            if (duration <= maxSwipeTime &&
                Mathf.Abs(delta.x) > Mathf.Abs(delta.y) &&
                delta.x > minSwipeDistance)
            {
                OnSwipeRight();
            }

            tracking = false;
        }
    }

    private void OnSwipeRight()
    {
        // Block during dash AND during return AND if down attack is active.
        if (isInAttackDash || isReturning ||
            (swipeDownDetector != null && swipeDownDetector.IsDownAttacking))
            return;

        // If we were returning, cancel it so we can attack again immediately.
        // (Note: your original code never reaches this because of the early return above,
        // but I'm leaving your intention intact by keeping the block as-is.)
        if (isReturning && dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            isReturning = false;

            // Keep katana layer OFF since we�re chaining into another attack.
            if (animator != null && katanaLayerIndex >= 0)
            {
                animator.SetLayerWeight(katanaLayerIndex, 0f);
            }
        }

        // Katana layer OFF at attack start
        if (animator != null && katanaLayerIndex >= 0)
        {
            animator.SetLayerWeight(katanaLayerIndex, 0f);
        }

        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
        }

        dashRoutine = StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isInAttackDash = true;

        // If we started in-air, suspend gravity so phone FPS doesn't drop us during the strike displacement
        bool startedInAir = jumper != null && !jumper.IsGrounded;
        if (suspendGravityDuringAirDash && startedInAir)
        {
            SuspendGravity();
        }

        try
        {
            // Wait until the Attack state reaches the move-start frame
            while (true)
            {
                if (animator == null)
                    break;

                var state = animator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName(attackStateName) && state.normalizedTime >= attackMoveStartNormalizedTime)
                    break;

                yield return null;
            }

            if (dashStartExtraDelay > 0f)
                yield return new WaitForSeconds(dashStartExtraDelay);

            Vector3 start = transform.position;
            Vector3 dashDir = useLocalX ? transform.right : Vector3.right;
            Vector3 dashTarget = start + dashDir * dashDistanceX;

            // Dash out
            yield return MoveOverTime(start, dashTarget, dashDuration);

            // Return to starting X (this is the "unrealistic but hits" snap you like)
            isReturning = true;
            yield return MoveXOverTime(returnDuration);
            isReturning = false;

            // IMPORTANT: Restore gravity AFTER we�re back into lane (back into position)
            if (gravitySuspended)
            {
                RestoreGravity();
            }

            // Your original grounding wait (kept)
            yield return WaitUntilGrounded();

            isInAttackDash = false;
            dashRoutine = null;

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            // Safety: never leave gravity disabled if coroutine gets interrupted
            if (gravitySuspended)
                RestoreGravity();

            isInAttackDash = false;
            isReturning = false;
            dashRoutine = null;
        }
    }

    // ---- Gravity suspend helpers ----
    private void SuspendGravity()
    {
        if (rb3D == null) return;

        gravitySuspended = true;
        savedUseGravity = rb3D.useGravity;
        savedYVelocity = rb3D.linearVelocity.y;

        rb3D.useGravity = false;

        // Also pin vertical velocity so we don't keep falling from existing momentum
        Vector3 v = rb3D.linearVelocity;
        v.y = 0f;
        rb3D.linearVelocity = v;
    }

    private void RestoreGravity()
    {
        if (rb3D == null) { gravitySuspended = false; return; }

        rb3D.useGravity = savedUseGravity;

        // Optional: restore prior Y velocity if you want continuity.
        // For your case (you explicitly don't want falling during the attack),
        // restoring velocity can reintroduce drop, so we keep Y at whatever it is now.
        // If you DO want to restore, uncomment:
        // var v = rb3D.velocity;
        // v.y = savedYVelocity;
        // rb3D.velocity = v;

        gravitySuspended = false;
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            // If gravity is suspended, keep Y pinned (prevents drift from other systems)
            float pinnedY = transform.position.y;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            Vector3 p = Vector3.Lerp(from, to, a);

            if (gravitySuspended)
                p.y = pinnedY;

            transform.position = p;
            yield return null;
        }

        if (gravitySuspended)
        {
            Vector3 final = to;
            final.y = transform.position.y;
            transform.position = final;
        }
        else
        {
            transform.position = to;
        }
    }

    private IEnumerator MoveXOverTime(float duration)
    {
        float fromX = transform.position.x;
        float targetX = startingPosition.position.x;
        float t = 0f;

        while (t < duration)
        {
            float currentY = transform.position.y;
            float currentZ = transform.position.z;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            Vector3 p = transform.position;
            p.x = Mathf.Lerp(fromX, targetX, a);

            // Keep Y pinned during return if gravity suspended
            if (gravitySuspended)
            {
                p.y = currentY;
                p.z = currentZ;
            }

            transform.position = p;
            yield return null;
        }

        Vector3 final = transform.position;
        final.x = targetX;
        transform.position = final;
    }

    private IEnumerator WaitUntilGrounded()
    {
        const float minStableTime = 0.03f;
        float groundedTime = 0f;

        while (true)
        {
            if (jumper != null && jumper.IsGrounded)
            {
                groundedTime += Time.deltaTime;
                if (groundedTime >= minStableTime)
                    yield break;
            }
            else
            {
                groundedTime = 0f;
            }

            yield return null;
        }
    }

    private void OnDisable()
    {
        // Safety: never leave animator frozen or gravity disabled
        if (animator != null)
            animator.speed = 1f;

        if (gravitySuspended)
            RestoreGravity();
    }
}
