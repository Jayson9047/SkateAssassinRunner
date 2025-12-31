using MoreMountains.InfiniteRunnerEngine;
using MoreMountains.Tools;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float slideForwardX = 0f;          // keep the system, set to 0 for now
    public float slideForwardDuration = 0.06f;

    public bool IsDownAttacking => isDownAttacking;

    [Header("DownAttack Freeze")]
    [SerializeField] private string downAttackStateName = "DownAttack";
    [SerializeField] private float downAttackHoldNormalizedTime = 0.36f; // Frame 13
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
    public float downAttackFallSpeed = -25f; // tune this
    private MMRigidbodyInterface rbInterface;

    [Header("Slide Speed Boost")]
    [SerializeField] private float slideSpeedFactor = 1.25f;   // tweak in inspector
    [SerializeField] private float slideSpeedDuration = 1.05f; // requirement

    // Cancel flag used when slide interrupts downattack
    private bool downAttackCancelledIntoSlide;

    private void Awake()
    {
        swipeRightAttackDetector = GetComponent<SwipeRightAttackDetector>();
        if (swipeRightAttackDetector == null)
        {
            Debug.LogWarning("SwipeDownDetector: SwipeRightAttackDetector not found.");
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        jumper = GetComponent<Jumper>();
        if (jumper == null)
            Debug.LogError("SwipeDownDetector: Jumper missing.");

        if (animator != null)
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);

        rbInterface = GetComponent<MMRigidbodyInterface>();
        if (rbInterface == null)
            Debug.LogError("SwipeDownDetector: MMRigidbodyInterface missing.");
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
        // Watchdog: if something ever leaves animator frozen unexpectedly, unfreeze.
        // This is cheap and prevents "stuck forever" even if a coroutine got interrupted.
        if (animator != null && animator.speed == 0f && !isDownAttacking)
        {
            animator.speed = 1f;
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

    private void OnSwipeDown()
    {
        // Still block if right-swipe attack is happening
        if (swipeRightAttackDetector != null && swipeRightAttackDetector.IsAttacking)
            return;

        bool isGrounded = jumper != null && jumper.IsGrounded;

        // Airborne: start DownAttack (only if not already doing it)
        if (!isGrounded && !isDownAttacking)
        {
            // Katana layer off for down attack start
            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 0f);

            StartCoroutine(DownAttackRoutine());
            return;
        }

        // Slide logic (soft lock)
        if (isSliding)
            return;

        // Grounded slide:
        // - allowed normally
        // - also allowed during downattack (including bounce touches) and cancels it
        if (!isDownAttacking || (isDownAttacking && isGrounded))
        {
            if (isDownAttacking)
            {
                // Cancel DownAttack into slide (hard cut)
                downAttackCancelledIntoSlide = true;

                // HARD SAFETY: if we froze at frame 13, unfreeze right now
                if (animator != null && animator.speed == 0f)
                    animator.speed = 1f;

                // Release input lock immediately for responsiveness
                isDownAttacking = false;
            }

            // Katana layer off for slide
            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 0f);

            // Nudge forward immediately when slide starts (kept, but set slideForwardX=0 for now)
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

        try
        {
            if (animator != null)
                animator.SetTrigger("DownAttack");

            // Let the animation advance to the hold frame
            while (!downAttackFrozen)
            {
                // If we got cancelled before we even froze, bail
                if (downAttackCancelledIntoSlide)
                    yield break;

                var state = animator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName(downAttackStateName) &&
                    state.normalizedTime >= downAttackHoldNormalizedTime)
                {
                    animator.speed = 0f;        // FREEZE at frame 13
                    downAttackFrozen = true;
                }

                yield return null;
            }

            // WAIT HERE until grounded (first impact) — FORCE SLAM SPEED
            while (jumper != null && !jumper.IsGrounded)
            {
                if (downAttackCancelledIntoSlide)
                    yield break;

                if (rbInterface != null)
                {
                    Vector3 v = rbInterface.Velocity;
                    v.y = downAttackFallSpeed; // force fast downward slam
                    rbInterface.Velocity = v;
                }

                yield return null;
            }

            // If we slide-cancel as soon as we touch ground, stop this coroutine
            if (downAttackCancelledIntoSlide)
                yield break;

            // RELEASE animation (impact + bounce plays)
            if (animator != null)
                animator.speed = 1f;

            // Wait until animation finishes naturally
            while (true)
            {
                // Allow slide-cancel during bounce too
                if (downAttackCancelledIntoSlide)
                    yield break;

                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(downAttackStateName) || state.normalizedTime >= 0.98f)
                    break;

                yield return null;
            }

            // Restore layer (only if we completed normally)
            if (animator != null && katanaLayerIndex >= 0)
                animator.SetLayerWeight(katanaLayerIndex, 1f);
        }
        finally
        {
            // HARD SAFETY: never leave animator frozen
            if (animator != null && animator.speed == 0f)
                animator.speed = 1f;

            // Ensure flags are sane no matter how we exit
            isDownAttacking = false;
            downAttackFrozen = false;
            downAttackCancelledIntoSlide = false;
        }
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.speed = 1f;
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        // Disable jump input during slide
        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = false;

        if (animator != null)
            animator.SetTrigger("Slide");

        // TEMP SPEED BOOST (restores automatically after duration)
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.TemporarilyMultiplySpeed(slideSpeedFactor, slideSpeedDuration);
        }

        yield return new WaitForSeconds(slideDuration);

        // Restore layer
        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);

        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = true;

        isSliding = false;

        // NO RETURN LOGIC ANYMORE
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
            a = a * a * (3f - 2f * a); // smoothstep

            var p = transform.position;
            p.x = Mathf.Lerp(startX, targetX, a);
            transform.position = p;

            yield return null;
        }

        var pEnd = transform.position;
        pEnd.x = targetX;
        transform.position = pEnd;
    }
}
