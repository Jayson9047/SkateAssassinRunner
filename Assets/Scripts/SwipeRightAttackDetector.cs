using MoreMountains.InfiniteRunnerEngine;
using System.Collections;
using UnityEngine;

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
    [SerializeField] private Transform dashDistanceLimit;

    private SwipeDownDetector swipeDownDetector;

    [Header("Dash (X Axis)")]
    public float dashDistanceX = 1.0f;          // fallback if dashDistanceLimit is missing
    public float dashDuration = 0.08f;
    public float returnDuration = 0.12f;
    public bool useLocalX = false;

    [Header("Attack Timing Gate")]
    [SerializeField] private string attackStateName = "DashAttack";
    [SerializeField] private int AttackStartFrame = 15;     // gate frame
    [SerializeField] private float dashStartExtraDelay = 0f;

    // Assumption you already made: 36 frames total in the clip.
    private float attackMoveStartNormalizedTime => AttackStartFrame / 36f;

    [Header("Air Dash Gravity Control")]
    [SerializeField] private bool suspendGravityDuringAirDash = true;

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private Jumper jumper;
    private int katanaLayerIndex = -1;

    // Single “busy” lock for the whole attack pipeline.
    private bool attackInProgress;
    private Coroutine dashRoutine;

    // Gravity suspend
    private Rigidbody rb3D;
    private bool gravitySuspended;
    private bool savedUseGravity;
    private float savedYVelocity;

    public bool IsDashingOrReturning => attackInProgress; // keep compatibility with your other logic

    private bool isDashMovementInProgress;
    public bool IsDashMovementInProgress => isDashMovementInProgress;

    private void Awake()
    {
        swipeDownDetector = GetComponent<SwipeDownDetector>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);

        jumper = GetComponent<Jumper>();
        rb3D = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(swipeRightKey))
            OnSwipeRight();

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
        if (attackInProgress)
            return;

        if (swipeDownDetector != null && swipeDownDetector.IsDownAttacking)
            return;

        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 0f);

        if (animator != null)
            animator.ResetTrigger(attackTriggerName);
        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        dashRoutine = StartCoroutine(DashRoutine());
    }

    // ✅ helper: abort safely without leaving gravity off or locks stuck
    private void AbortDashRoutine()
    {
        isDashMovementInProgress = false;
        attackInProgress = false;
        dashRoutine = null;

        if (gravitySuspended)
            RestoreGravity();
    }

    private IEnumerator DashRoutine()
    {
        attackInProgress = true;

        try
        {
            // YOUR REQUIREMENT: if airborne, disable gravity for the ENTIRE attack (arming + dash + return + anim tail)
            bool startedInAir = jumper != null && !jumper.IsGrounded;
            if (suspendGravityDuringAirDash && startedInAir)
                SuspendGravity();

            // ---- ARMING PHASE (gravity may already be off) ----
            float gateWaitStartTime = Time.time;
            const float maxGateWaitSeconds = 1.0f; // safety: never lock forever

            // Wait for fresh DashAttack start, but allow abort
            IEnumerator freshStart = WaitForFreshAttackStateStart();
            while (freshStart.MoveNext())
            {
                // If down attack steals control while arming, abort NOW (and restore gravity)
                if (swipeDownDetector != null && swipeDownDetector.IsDownAttacking)
                {
                    AbortDashRoutine();
                    yield break;
                }

                if (Time.time - gateWaitStartTime > maxGateWaitSeconds)
                {
                    AbortDashRoutine();
                    yield break;
                }

                yield return freshStart.Current;
            }

            // Wait for frame gate, but allow abort
            IEnumerator gateFrame = WaitForAttackGateFrame();
            while (gateFrame.MoveNext())
            {
                if (swipeDownDetector != null && swipeDownDetector.IsDownAttacking)
                {
                    AbortDashRoutine();
                    yield break;
                }

                if (Time.time - gateWaitStartTime > maxGateWaitSeconds)
                {
                    AbortDashRoutine();
                    yield break;
                }

                yield return gateFrame.Current;
            }

            if (dashStartExtraDelay > 0f)
                yield return new WaitForSeconds(dashStartExtraDelay);

            Vector3 start = transform.position;
            Vector3 dashDir = useLocalX ? transform.right : Vector3.right;

            float rawDist = dashDistanceX;
            if (dashDistanceLimit != null)
                rawDist = Mathf.Abs(dashDistanceLimit.position.x - start.x);

            Vector3 dashTarget = start + dashDir * rawDist;

            if (dashDistanceLimit != null)
            {
                float limitX = dashDistanceLimit.position.x;
                if (dashDir.x >= 0f)
                {
                    dashTarget.x = Mathf.Min(dashTarget.x, limitX);
                    if (start.x > limitX) dashTarget.x = start.x;
                }
                else
                {
                    dashTarget.x = Mathf.Max(dashTarget.x, limitX);
                    if (start.x < limitX) dashTarget.x = start.x;
                }
            }

            // movement phase begins
            isDashMovementInProgress = true;

            yield return MoveOverTime(start, dashTarget, dashDuration);
            yield return MoveXOverTime(returnDuration);

            // movement phase ends (we're back at starting X now)
            isDashMovementInProgress = false;

            if (gravitySuspended)
                RestoreGravity();

            // keep gravity OFF until the whole attack animation finishes (per your requirement)
            yield return WaitForAttackAnimationToFinish();

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            // Safety: always restore gravity and unlock, even if something stops the coroutine
            if (gravitySuspended)
                RestoreGravity();

            isDashMovementInProgress = false;
            attackInProgress = false;
            dashRoutine = null;
        }
    }

    private IEnumerator WaitForFreshAttackStateStart()
    {
        if (animator == null)
            yield break;

        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(attackStateName))
                break;
            yield return null;
        }

        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(attackStateName))
            {
                while (true)
                {
                    state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.IsName(attackStateName))
                        break;
                    yield return null;
                }
            }

            float frac = state.normalizedTime % 1f;
            if (frac <= 0.08f)
                yield break;

            yield return null;
        }
    }

    private IEnumerator WaitForAttackGateFrame()
    {
        if (animator == null)
            yield break;

        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName(attackStateName))
            {
                float frac = state.normalizedTime % 1f;
                if (frac >= attackMoveStartNormalizedTime)
                    yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForAttackAnimationToFinish()
    {
        if (animator == null)
            yield break;

        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);

            if (!state.IsName(attackStateName))
                yield break;

            float frac = state.normalizedTime % 1f;
            if (frac >= 0.98f)
                yield break;

            yield return null;
        }
    }

    // Gravity suspend helpers
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
        if (startingPosition == null)
            yield break;

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

    private void OnDisable()
    {
        if (animator != null)
            animator.speed = 1f;

        if (gravitySuspended)
            RestoreGravity();

        attackInProgress = false;
        isDashMovementInProgress = false;
        dashRoutine = null;
    }
}
