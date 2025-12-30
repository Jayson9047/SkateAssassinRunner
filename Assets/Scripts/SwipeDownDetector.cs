using MoreMountains.InfiniteRunnerEngine;
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
    private int katanaLayerIndex = -1;

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private bool isSliding;
    private bool isDownAttacking;
    private bool isSlideReturning;
    private Jumper jumper;
    private TapOnlyMainActionZone mainActionTouchZone;

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

        if (animator != null)
            animator.SetTrigger("DownAttack");

        yield return new WaitForSeconds(downAttackDuration);

        // Restore layer
        if (animator != null && katanaLayerIndex >= 0)
            animator.SetLayerWeight(katanaLayerIndex, 1f);

        isDownAttacking = false;
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
