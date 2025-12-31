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
    [SerializeField] private float slideDuration = 1.2f;
    [SerializeField] private float downAttackDuration = 1.05f;
    private SwipeRightAttackDetector swipeRightAttackDetector;
    [Header("Slide X Nudge")]
    public Transform startingPosition;
    public float slideForwardX = 0.5f;
    public float slideReturnDuration = 0.12f;
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
    private bool isSlideReturning;
    private Jumper jumper;
    private TapOnlyMainActionZone mainActionTouchZone;

    [Header("DownAttack Slam")]
    public float downAttackFallSpeed = -25f; // tune this
    private MMRigidbodyInterface rbInterface;

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
        // HARD LOCK: DownAttack owns all input
        if (isDownAttacking || (swipeRightAttackDetector != null && swipeRightAttackDetector.IsAttacking) || isSlideReturning)
            return;

        bool isGrounded = jumper != null && jumper.IsGrounded;

        // Katana layer off for both slide and down attack
        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 0f);

        if (!isGrounded)
        {
            StartCoroutine(DownAttackRoutine());
            return;
        }

        // Slide logic (soft lock)
        if (isSliding)
            return;

        // Nudge forward immediately when slide starts
        if (startingPosition != null)
        {
            StartCoroutine(MoveXOverTime(startingPosition.position.x + slideForwardX, slideForwardDuration));
        }

        StartCoroutine(SlideRoutine());

    }



    private IEnumerator DownAttackRoutine()
    {
        isDownAttacking = true;
        downAttackFrozen = false;

        if (animator != null)
            animator.SetTrigger("DownAttack");

        // Let the animation advance to the hold frame
        while (!downAttackFrozen)
        {
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
            if (rbInterface != null)
            {
                Vector3 v = rbInterface.Velocity;
                v.y = downAttackFallSpeed; // force fast downward slam
                rbInterface.Velocity = v;
            }

            yield return null;
        }

        // RELEASE animation (impact + bounce plays)
        animator.speed = 1f;

        // Wait until animation finishes naturally
        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(downAttackStateName) || state.normalizedTime >= 0.98f)
                break;

            yield return null;
        }

        // Restore layer
        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);

        isDownAttacking = false;
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

        yield return new WaitForSeconds(slideDuration);

        // Restore layer
        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);

        if (mainActionTouchZone != null)
            mainActionTouchZone.enabled = true;

        isSliding = false;
        isSlideReturning = true;
        // Return AFTER slide ends (your requirement)
        if (startingPosition != null)
        {
            StartCoroutine(MoveXOverTime(startingPosition.position.x, slideReturnDuration));
        }

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
    isSlideReturning = false;
}

}
