using MoreMountains.InfiniteRunnerEngine;
using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

    [Header("Positions")]
    [SerializeField] private Transform startingPosition;

    // NEW: hard cap for dash max X
    [SerializeField] private Transform dashDistanceLimit;

    private SwipeDownDetector swipeDownDetector;

    [Header("Dash (X Axis)")]
    public float dashDistanceX = 1.0f;
    public float dashDuration = 0.08f;
    public float returnDuration = 0.12f;
    public bool useLocalX = false;

    [Header("Spam Protection")]
    [SerializeField] private float groundedDashCooldown = 0.25f; // tune
    private float nextDashAllowedTime = 0f;

    // keep this as your "attack is busy" flag (includes the wait-until-grounded tail)
    public bool IsAttacking => isInAttackDash || isReturning;

    // NEW: only the movement phases (this is what DownSwipe should block against)
    public bool IsDashingOrReturning => isInAttackDash || isReturning;

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
    [SerializeField] private string attackStateName = "DashAttack";
    [SerializeField] private int AttackStartFrame = 15;
    [SerializeField] private float dashStartExtraDelay = 0f;
    private float attackMoveStartNormalizedTime = 1f;

    [Header("Air Dash Gravity Control")]
    [SerializeField] private bool suspendGravityDuringAirDash = true;

    private Rigidbody rb3D;
    private bool gravitySuspended;
    private bool savedUseGravity;
    private float savedYVelocity;

    private void Awake()
    {
        swipeDownDetector = GetComponent<SwipeDownDetector>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);
        }

        jumper = GetComponent<Jumper>();
        rb3D = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        attackMoveStartNormalizedTime = AttackStartFrame / 36f;

        if (Input.GetKeyDown(swipeRightKey))
        {
            OnSwipeRight();
        }

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
        // global cooldown guard (prevents spam on ground)
        if (Time.time < nextDashAllowedTime)
            return;

        if (isInAttackDash || isReturning ||
            (swipeDownDetector != null && swipeDownDetector.IsDownAttacking))
            return;

        // If grounded, apply cooldown immediately
        bool groundedNow = jumper != null && jumper.IsGrounded;
        if (groundedNow)
            nextDashAllowedTime = Time.time + groundedDashCooldown;

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

        bool startedInAir = jumper != null && !jumper.IsGrounded;
        if (suspendGravityDuringAirDash && startedInAir)
        {
            SuspendGravity();
        }

        try
        {
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

            float rawDist = dashDistanceX;

            //TODO: add logic to dash only up to dashDistanceLimit if no enemy is inside that range.
            if (dashDistanceLimit != null)
            {
                rawDist = Mathf.Abs(dashDistanceLimit.position.x - transform.position.x);
            }
            Vector3 dashTarget = start + dashDir * rawDist;

            if (dashDistanceLimit != null)
            {
                float limitX = dashDistanceLimit.position.x;
                if (dashDir.x >= 0f)
                {
                    dashTarget.x = Mathf.Min(dashTarget.x, limitX);
                    if (start.x > limitX)
                        dashTarget.x = start.x;
                }
                else
                {
                    dashTarget.x = Mathf.Max(dashTarget.x, limitX);
                    if (start.x < limitX)
                        dashTarget.x = start.x;
                }
            }

            yield return MoveOverTime(start, dashTarget, dashDuration);

            isReturning = true;
            yield return MoveXOverTime(returnDuration);
            isReturning = false;
            isInAttackDash = false;

            if (gravitySuspended)
            {
                RestoreGravity();
            }

            // NOTE: we keep waiting until grounded for “attack completion”,
            // but DownSwipe won’t be blocked by this anymore.
            yield return WaitUntilGrounded();

            dashRoutine = null;

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            if (gravitySuspended)
                RestoreGravity();

            isInAttackDash = false;
            isReturning = false;
            dashRoutine = null;
        }
    }

    private void SuspendGravity()
    {
        if (rb3D == null) return;

        gravitySuspended = true;
        savedUseGravity = rb3D.useGravity;
        savedYVelocity = rb3D.linearVelocity.y;

        rb3D.useGravity = false;

        Vector3 v = rb3D.linearVelocity;
        v.y = 0f;
        rb3D.linearVelocity = v;
    }

    private void RestoreGravity()
    {
        if (rb3D == null) { gravitySuspended = false; return; }
        rb3D.useGravity = savedUseGravity;
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
            float pinnedY = transform.position.y;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            Vector3 p = Vector3.Lerp(from, to, a);
            if (gravitySuspended) p.y = pinnedY;

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
        if (animator != null)
            animator.speed = 1f;

        if (gravitySuspended)
            RestoreGravity();
    }
}
