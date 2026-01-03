using IndieKit;
using MoreMountains.InfiniteRunnerEngine;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeDownDetector : MonoBehaviour
{
    [Header("Swipe Settings")]
    public float minSwipeDistance = 100f;
    public float maxSwipeTime = 0.5f;

    [Header("Keyboard Debug")]
    public KeyCode swipeDownKey = KeyCode.S;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private string katanaLayerName = "KatanaLayer";
    [SerializeField] private float slideDuration = 0.8f;

    private SwipeRightAttackDetector swipeRightAttackDetector;

    [Header("Slide X Nudge")]
    public Transform startingPosition;
    public float slideForwardX = 0f;
    public float slideForwardDuration = 0.06f;

    public bool IsDownAttacking => isDownAttacking;

    [Header("DownAttack Freeze")]
    [SerializeField] private string downAttackStateName = "DownAttack";
    [SerializeField] private float downAttackHoldNormalizedTime = 0.36f;
    private bool downAttackFrozen;

    private int katanaLayerIndex = -1;

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private bool isSliding;
    private bool isDownAttacking;
    private Jumper jumper;
    private TapOnlyMainActionZone mainActionTouchZone;

    [Header("DownAttack Slam")]
    public float downAttackFallSpeed = -25f;
    private MMRigidbodyInterface rbInterface;

    [Header("Slide Speed Boost")]
    [SerializeField] private float slideSpeedFactor = 1.25f;
    [SerializeField] private float slideSpeedDuration = 1.05f;

    private bool pendingDownAttack;

    private bool downAttackCancelledIntoSlide;

    [Header("DownAttack Ground Impact")]
    [Tooltip("World-space impact zone center (place this on the ground where the slam should affect).")]
    [SerializeField] private Transform groundImpactZoneCenter;

    [Tooltip("Impact radius in world units (circle on the ground).")]
    [SerializeField] private float groundImpactRadius = 2.0f;

    [Tooltip("Only colliders on these layers will be damaged by the slam (barrels/enemies).")]
    [SerializeField] private LayerMask damageableMask = ~0;

    [Tooltip("Damage applied by slam. Set high to guarantee barrel destruction.")]
    [SerializeField] private float slamDamage = 999f;

    [Tooltip("If true, slam impact only triggers once per down attack.")]
    [SerializeField] private bool triggerImpactOncePerDownAttack = true;

    private bool impactTriggeredThisDownAttack = false;

    private readonly HashSet<Component> damagedThisImpact = new HashSet<Component>();

    private void Awake()
    {
        swipeRightAttackDetector = GetComponent<SwipeRightAttackDetector>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        jumper = GetComponent<Jumper>();

        if (animator != null)
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);

        rbInterface = GetComponent<MMRigidbodyInterface>();
    }

    private void OnEnable()
    {
        StartCoroutine(FindMainActionTouchZoneWhenReady());
    }

    private IEnumerator FindMainActionTouchZoneWhenReady()
    {
        for (int i = 0; i < 300 && mainActionTouchZone == null; i++)
        {
            var zones = Resources.FindObjectsOfTypeAll<TapOnlyMainActionZone>();
            foreach (var z in zones)
            {
                if (z != null && z.gameObject.activeInHierarchy)
                {
                    mainActionTouchZone = z;
                    yield break;
                }
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (animator != null && animator.speed == 0f && !isDownAttacking)
        {
            animator.speed = 1f;
        }
        // If we buffered a down attack during air dash, trigger it the moment dash movement is done
        if (pendingDownAttack)
        {
            bool dashMoveBusy = swipeRightAttackDetector != null && swipeRightAttackDetector.IsDashMovementInProgress;
            bool airborne = jumper != null && !jumper.IsGrounded;

            if (!dashMoveBusy && airborne && !isDownAttacking)
            {
                pendingDownAttack = false;
                TriggerDownAttackFromBuffer();
            }
        }
        if (Input.GetKeyDown(swipeDownKey))
            OnSwipeDown();

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
                Mathf.Abs(delta.y) > Mathf.Abs(delta.x) &&
                delta.y < -minSwipeDistance)
            {
                OnSwipeDown();
            }

            tracking = false;
        }
    }

    public void TriggerDownAttackFromBuffer()
    {
        if (isDownAttacking) return;

        if (animator != null && animator.speed == 0f)
            animator.speed = 1f;

        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 0f);

        StartCoroutine(DownAttackRoutine());
    }

    private void OnSwipeDown()
    {
        bool isGrounded = jumper != null && jumper.IsGrounded;

        // If we are airborne and currently dash-moving/returning, buffer the down attack
        if (!isGrounded && swipeRightAttackDetector != null && swipeRightAttackDetector.IsDashMovementInProgress)
        {
            pendingDownAttack = true;
            return;
        }

        // Block grounded slide during dash movement, but we allow buffered air-downattack above
        if (swipeRightAttackDetector != null && swipeRightAttackDetector.IsDashMovementInProgress)
            return;

        // Airborne: start DownAttack (only if not already doing it)
        if (!isGrounded && !isDownAttacking)
        {
            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 0f);

            StartCoroutine(DownAttackRoutine());
            return;
        }

        if (isSliding)
            return;

        // Grounded slide, including cancel from downattack
        if (!isDownAttacking || (isDownAttacking && isGrounded))
        {
            if (isDownAttacking)
            {
                downAttackCancelledIntoSlide = true;

                if (animator != null && animator.speed == 0f)
                    animator.speed = 1f;

                isDownAttacking = false;
            }

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 0f);

            if (startingPosition != null && Mathf.Abs(slideForwardX) > 0.0001f)
            {
                StartCoroutine(MoveXOverTime(startingPosition.position.x + slideForwardX, slideForwardDuration));
            }

            StartCoroutine(SlideRoutine());
        }
    }

    private IEnumerator DownAttackRoutine()
    {
        isDownAttacking = true;
        downAttackFrozen = false;
        downAttackCancelledIntoSlide = false;

        impactTriggeredThisDownAttack = false;
        damagedThisImpact.Clear();

        // Block jump during downattack completely
        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = false;

        try
        {
            if (animator != null)
                animator.SetTrigger("DownAttack");

            while (!downAttackFrozen)
            {
                if (downAttackCancelledIntoSlide)
                    yield break;

                var state = animator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName(downAttackStateName) &&
                    state.normalizedTime >= downAttackHoldNormalizedTime)
                {
                    animator.speed = 0f;
                    downAttackFrozen = true;
                }

                yield return null;
            }

            while (jumper != null && !jumper.IsGrounded)
            {
                if (downAttackCancelledIntoSlide)
                    yield break;

                if (rbInterface != null)
                {
                    Vector3 v = rbInterface.Velocity;
                    v.y = downAttackFallSpeed;
                    rbInterface.Velocity = v;
                }

                yield return null;
            }

            if (downAttackCancelledIntoSlide)
                yield break;

            if (animator != null)
                animator.speed = 1f;

            while (true)
            {
                if (downAttackCancelledIntoSlide)
                    yield break;

                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(downAttackStateName) || state.normalizedTime >= 0.98f)
                    break;

                yield return null;
            }

            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            if (animator != null && animator.speed == 0f)
                animator.speed = 1f;

            // Only re-enable jump if we didn't slide-cancel (slide routine manages its own lock)
            if (!downAttackCancelledIntoSlide && mainActionTouchZone != null)
                mainActionTouchZone.enabled = true;

            isDownAttacking = false;
            downAttackFrozen = false;
            downAttackCancelledIntoSlide = false;

            impactTriggeredThisDownAttack = false;
            damagedThisImpact.Clear();
        }
    }

    // Trigger slam impact when we collide with a damageable during downattack
    private void OnCollisionEnter(Collision collision)
    {
        if (!isDownAttacking) return;

        // If you use triggers instead of collisions on barrels, OnTriggerEnter below will handle it.
        TryTriggerGroundImpactFromCollider(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDownAttacking) return;
        TryTriggerGroundImpactFromCollider(other);
    }

    private void TryTriggerGroundImpactFromCollider(Collider hitCollider)
    {
        if (hitCollider == null) return;

        // Only trigger impact if what we hit is damageable (barrel/enemy)
        var damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (triggerImpactOncePerDownAttack && impactTriggeredThisDownAttack)
            return;

        // Run the actual AOE damage using the ground zone
        DoGroundImpactAOE(hitCollider.bounds.center);

        impactTriggeredThisDownAttack = true;
    }

    private void DoGroundImpactAOE(Vector3 hitPoint)
    {
        if (groundImpactZoneCenter == null)
        {
            Debug.LogWarning("[SwipeDownDetector] Ground Impact Zone Center is not assigned.");
            return;
        }

        damagedThisImpact.Clear();

        Vector3 center = groundImpactZoneCenter.position;

        // Damage all damageables inside the radius
        Collider[] hits = Physics.OverlapSphere(center, groundImpactRadius, damageableMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            // Prevent double-damaging the same object within this impact
            Component key = dmg as Component;
            if (key == null) key = col.transform;

            if (damagedThisImpact.Contains(key))
                continue;

            damagedThisImpact.Add(key);

            dmg.ApplyDamage(slamDamage, hitPoint);
        }
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = false;

        if (animator != null)
            animator.SetTrigger("Slide");

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.TemporarilyMultiplySpeed(slideSpeedFactor, slideSpeedDuration);
        }

        yield return new WaitForSeconds(slideDuration);

        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);

        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = true;

        isSliding = false;
    }

    private IEnumerator MoveXOverTime(float targetX, float duration)
    {
        float startX = transform.position.x;
        if (duration <= 0f)
        {
            var p = transform.position;
            p.x = targetX;
            transform.position = p;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            var p = transform.position;
            p.x = Mathf.Lerp(startX, targetX, a);
            transform.position = p;

            yield return null;
        }

        var pEnd = transform.position;
        pEnd.x = targetX;
        transform.position = pEnd;
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.speed = 1f;

        // Safety: don't leave jump disabled if object is disabled mid-downattack
        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = true;
    }
}
