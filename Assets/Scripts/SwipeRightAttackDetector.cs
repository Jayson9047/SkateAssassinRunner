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

    [Header("Attack Freeze / Dash Timing")]
    [SerializeField] private string attackStateName = "DashAttack"; // animator state name in Base Layer
    private float attackMoveStartNormalizedTime = 1f; // <-- set this to Frame 19 normalized
    [SerializeField] private int AttackStartFrame = 15;
    [SerializeField] private float dashStartExtraDelay = 0f; // optional extra delay AFTER reaching frame 19


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
    }

    private void Update()
    {
        attackMoveStartNormalizedTime = AttackStartFrame / 36f; // assuming 30 FPS animation
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
        if (isReturning && dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            isReturning = false;
            // Keep katana layer OFF since we’re chaining into another attack.
            if (animator != null && katanaLayerIndex >= 0)
            {
                animator.SetLayerWeight(katanaLayerIndex, 0f);
            }
        }

        // Katana layer OFF at attack start (your logic)
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

        // Wait until the Attack state reaches the "move start" frame (frame 19)
        while (true)
        {
            if (animator == null)
                break;

            var state = animator.GetCurrentAnimatorStateInfo(0);

            // Make sure we're actually in the attack state before reading normalized time
            if (state.IsName(attackStateName) && state.normalizedTime >= attackMoveStartNormalizedTime)
                break;

            yield return null;
        }

        if (dashStartExtraDelay > 0f)
            yield return new WaitForSeconds(dashStartExtraDelay);

        Vector3 start = transform.position;
        Vector3 dashDir = useLocalX ? transform.right : Vector3.right;
        Vector3 dashTarget = start + dashDir * dashDistanceX;

        // Dash out (your existing move)
        yield return MoveOverTime(start, dashTarget, dashDuration);

        isReturning = true;
        yield return MoveXOverTime(returnDuration);
        isReturning = false;
        
        // Wait until grounded before returning
        yield return WaitUntilGrounded();

        isInAttackDash = false;

        dashRoutine = null;

        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);
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
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            transform.position = Vector3.Lerp(from, to, a);
            yield return null;
        }

        transform.position = to;
    }

    private IEnumerator MoveXOverTime(float duration)
    {
        float fromX = transform.position.x;
        float targetX = startingPosition.position.x;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            a = a * a * (3f - 2f * a);

            Vector3 p = transform.position;
            p.x = Mathf.Lerp(fromX, targetX, a);
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
    }
}
