using IndieKit;
using MoreMountains.InfiniteRunnerEngine;
using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private float sweepRadius = 0.35f; // use ~player body width or katana reach
    private Vector3 _prevDashPos;


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

    // -------------------------------
    // Pass-to-destroy damage logic
    // -------------------------------
    [Header("Attack Hit (Pass-to-Destroy)")]
    [Tooltip("Only colliders on these layers are hittable targets (hurtboxes / barrels). IMPORTANT: exclude detection-trigger layers.")]
    [SerializeField] private LayerMask damageableMask = ~0;

    [Tooltip("Search radius around player while dashing to find nearby targets.")]
    [SerializeField] private float damageableSearchRadius = 1.5f;

    [Tooltip("Must be within this distance (from ClosestPoint) to count as an actual hit.")]
    [SerializeField] private float hitRadius = 0.55f;

    [Tooltip("Max allowed Z separation (2.5D). Tighten if you ever hit things behind/in front.")]
    [SerializeField] private float depthWindow = 0.60f;

    [Tooltip("If player is ABOVE target's top by this amount, we treat it as jumping over and do NOT hit.")]
    [SerializeField] private float overheadClearanceY = 0.05f;

    [Tooltip("Extra buffer so we damage after we've passed the target, not exactly at the same X.")]
    [SerializeField] private float passBufferX = 0.05f;

    [Tooltip("Damage applied to damageables (IndieKit destructibles often use 1 HP).")]
    [SerializeField] private float attackDamage = 999f;

    [Tooltip("Optional: assign your player's main collider. If null, we auto-pick the first non-trigger collider on this GameObject.")]
    [SerializeField] private Collider playerBodyCollider;

    // Track what we've already damaged in this one attack so we don't double-hit.
    private readonly HashSet<Component> damagedThisAttack = new HashSet<Component>();
    private readonly HashSet<Collider> ignoredCollidersThisAttack = new HashSet<Collider>();
    // -------------------------------

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private Jumper jumper;
    private int katanaLayerIndex = -1;

    private bool attackInProgress;
    private Coroutine dashRoutine;

    // Gravity suspend
    private Rigidbody rb3D;
    private bool gravitySuspended;
    private bool savedUseGravity;

    public bool IsDashingOrReturning => attackInProgress;
    private bool isDashMovementInProgress;
    public bool IsDashMovementInProgress => isDashMovementInProgress;
    public bool IsAttacking => attackInProgress;

    private int attackId = 0;
    public int AttackId => attackId;

    private void Awake()
    {
        swipeDownDetector = GetComponent<SwipeDownDetector>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);

        jumper = GetComponent<Jumper>();
        rb3D = GetComponent<Rigidbody>();

        // Auto-pick a body collider if not assigned (used only to ignore blocking collisions during dash)
        if (playerBodyCollider == null)
        {
            var cols = GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && !cols[i].isTrigger)
                {
                    playerBodyCollider = cols[i];
                    break;
                }
            }
        }
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
        {
            animator.ResetTrigger(attackTriggerName);
            animator.SetTrigger(attackTriggerName);
        }

        dashRoutine = StartCoroutine(DashRoutine());
    }

    private void AbortDashRoutine()
    {
        isDashMovementInProgress = false;
        attackInProgress = false;
        dashRoutine = null;

        RestoreIgnoredCollisions();
        damagedThisAttack.Clear();

        if (gravitySuspended)
            RestoreGravity();
    }

    private IEnumerator DashRoutine()
    {
        attackInProgress = true;
        attackId++;

        damagedThisAttack.Clear();
        RestoreIgnoredCollisions();

        try
        {
            bool startedInAir = jumper != null && !jumper.IsGrounded;
            if (suspendGravityDuringAirDash && startedInAir)
                SuspendGravity();

            float gateWaitStartTime = Time.time;
            const float maxGateWaitSeconds = 1.0f;

            IEnumerator freshStart = WaitForFreshAttackStateStart();
            while (freshStart.MoveNext())
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

                yield return freshStart.Current;
            }

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

            isDashMovementInProgress = true;

            yield return MoveOverTime_WithPassToDestroy(start, dashTarget, dashDuration, dashDir);

            yield return MoveXOverTime(returnDuration);

            isDashMovementInProgress = false;

            if (gravitySuspended)
                RestoreGravity();

            yield return WaitForAttackAnimationToFinish();

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            if (gravitySuspended)
                RestoreGravity();

            isDashMovementInProgress = false;
            attackInProgress = false;
            dashRoutine = null;

            RestoreIgnoredCollisions();
            damagedThisAttack.Clear();
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

    private void SuspendGravity()
    {
        if (rb3D == null) return;

        gravitySuspended = true;
        savedUseGravity = rb3D.useGravity;
        rb3D.useGravity = false;

        // Zero out vertical motion while gravity is suspended
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
    private IEnumerator MoveOverTime_WithPassToDestroy(Vector3 from, Vector3 to, float duration, Vector3 dashDir)
    {
        if (duration <= 0f)
        {
            transform.position = to;
            yield break;
        }

        _prevDashPos = transform.position;

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

            // ✅ sweep from previous -> current (no misses)
            TryDamageBySweep(_prevDashPos, p);

            _prevDashPos = p;

            yield return null;
        }

        // final snap
        Vector3 finalPos = gravitySuspended ? new Vector3(to.x, transform.position.y, to.z) : to;
        transform.position = finalPos;

        TryDamageBySweep(_prevDashPos, finalPos);
        _prevDashPos = finalPos;
    }
    private void TryDamageBySweep(Vector3 fromPos, Vector3 toPos)
    {
        if (!attackInProgress) return;

        // A capsule volume from last frame to this frame
        Collider[] cols = Physics.OverlapCapsule(
            fromPos,
            toPos,
            sweepRadius,
            damageableMask,
            QueryTriggerInteraction.Collide
        );

        if (cols == null || cols.Length == 0) return;

        Vector3 playerPos = toPos;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null) continue;

            // Ignore triggers (detection volumes, etc.)
            if (col.isTrigger) continue;

            var damageable = col.GetComponentInParent<IndieKit.IDamageable>();
            if (damageable == null) continue;

            // Optional: prevent dash from getting blocked by the target collider
            if (playerBodyCollider != null)
                Physics.IgnoreCollision(playerBodyCollider, col, true);

            // Overhead rule (prevents "jump-over kills")
            // IMPORTANT tweak: use the player's Y, but allow a little leeway
            if (playerPos.y > col.bounds.max.y + overheadClearanceY)
                continue;

            // 2.5D depth gating (if you want it)
            Vector3 closest = col.ClosestPoint(playerPos);
            float zDelta = Mathf.Abs(closest.z - playerPos.z);
            if (zDelta > depthWindow)
                continue;

            // Apply damage at the closest point
            damageable.ApplyDamage(attackDamage, closest);
        }
    }

    private void TryDamageAtPoint(Vector3 playerPos)
    {
        Collider[] cols = Physics.OverlapSphere(
            playerPos,
            sweepRadius,
            damageableMask,
            QueryTriggerInteraction.Collide
        );

        if (cols == null || cols.Length == 0) return;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider col = cols[i];
            if (col == null) continue;
            if (col.isTrigger) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            if (playerPos.y > col.bounds.max.y + overheadClearanceY)
                continue;

            Vector3 hitPoint = col.ClosestPoint(playerPos);
            damageable.ApplyDamage(attackDamage, hitPoint);
        }
    }


    private void TryDamageBarrelsWhenPassed(Vector3 dashDir)
    {
        if (!attackInProgress) return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            damageableSearchRadius,
            damageableMask,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0) return;

        Vector3 playerPos = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            // IMPORTANT: ignore triggers (detection colliders, reach triggers, etc.)
            if (col.isTrigger) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            // One target can only be damaged once per attack, even with multiple colliders
            Component key = damageable as Component;
            if (key == null) key = col.transform;

            if (damagedThisAttack.Contains(key))
                continue;

            // Prevent blocking during dash (only for non-trigger colliders)
            if (playerBodyCollider != null && !ignoredCollidersThisAttack.Contains(col))
            {
                Physics.IgnoreCollision(playerBodyCollider, col, true);
                ignoredCollidersThisAttack.Add(col);
            }

            // Closest point for stable distance + pass-through detection
            Vector3 closest = col.ClosestPoint(playerPos);

            // Must be close enough overall
            float sqrDist = (closest - playerPos).sqrMagnitude;
            if (sqrDist > hitRadius * hitRadius)
                continue;

            // 2.5D depth gating
            float zDelta = Mathf.Abs(closest.z - playerPos.z);
            if (zDelta > depthWindow)
                continue;

            // Asymmetric vertical rule:
            // If player is clearly ABOVE the target's TOP, treat it as "jumped over" (no hit).
            if (playerPos.y > col.bounds.max.y + overheadClearanceY)
                continue;

            // X "passed" check → slice moment
            float targetX = closest.x;

            if (dashDir.x >= 0f)
            {
                if (playerPos.x >= targetX + passBufferX)
                {
                    damageable.ApplyDamage(attackDamage, closest);
                    damagedThisAttack.Add(key);
                }
            }
            else
            {
                if (playerPos.x <= targetX - passBufferX)
                {
                    damageable.ApplyDamage(attackDamage, closest);
                    damagedThisAttack.Add(key);
                }
            }
        }
    }

    private void RestoreIgnoredCollisions()
    {
        if (playerBodyCollider == null) { ignoredCollidersThisAttack.Clear(); return; }

        foreach (var col in ignoredCollidersThisAttack)
        {
            if (col == null) continue;
            Physics.IgnoreCollision(playerBodyCollider, col, false);
        }

        ignoredCollidersThisAttack.Clear();
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

        RestoreIgnoredCollisions();
        damagedThisAttack.Clear();
    }
}
