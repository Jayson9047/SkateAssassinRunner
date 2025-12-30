using MoreMountains.InfiniteRunnerEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwipeDownDetector : MonoBehaviour
{
    [Header("Swipe Settings")]
    public float minSwipeDistance = 100f; // pixels
    public float maxSwipeTime = 0.5f;

    [Header("Keyboard Debug")]
    public KeyCode swipeDownKey = KeyCode.S; // or DownArrow

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Tooltip("Animator layer name that holds the katana overlay.")]
    [SerializeField] private string katanaLayerName = "KatanaLayer";

    [Tooltip("Must match your slide animation duration.")]
    [SerializeField] private float slideDuration = 1.2f;

    private int katanaLayerIndex = -1;

    private Vector2 startPos;
    private float startTime;
    private bool tracking;

    private bool isSliding;

    // Instead of caching a GameObject by name (which can point to an inactive/old instance),
    // cache the actual TouchZone that is currently active in the hierarchy.
    private TapOnlyMainActionZone mainActionTouchZone;

    private void Awake()
    {
        // Animator first (your original code tried to read layer index before null-check)
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            katanaLayerIndex = animator.GetLayerIndex(katanaLayerName);
            if (katanaLayerIndex < 0)
            {
                Debug.LogWarning($"SwipeDownDetector: Animator layer '{katanaLayerName}' not found.");
            }
        }
        else
        {
            Debug.LogWarning("SwipeDownDetector: Animator reference is missing.");
        }
    }

    private void OnEnable()
    {

        // Start trying to locate the correct Main Action TouchZone.
        StartCoroutine(FindMainActionTouchZoneWhenReady());
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restart / loading screen swaps UI -> references become invalid.
        mainActionTouchZone = null;

        StopAllCoroutines();
        StartCoroutine(FindMainActionTouchZoneWhenReady());
    }

    private IEnumerator FindMainActionTouchZoneWhenReady()
    {
        // Retry for ~5 seconds @ 60fps (same as your current approach)
        for (int i = 0; i < 300 && mainActionTouchZone == null; i++)
        {
            TryFindMainActionTouchZoneActive();
            yield return null;
        }

        // Optional debug
        // if (mainActionTouchZone == null) Debug.LogWarning("SwipeDownDetector: Could not find an ACTIVE MainAction TouchZone.");
    }

    private void TryFindMainActionTouchZoneActive()
    {
        if (mainActionTouchZone != null && mainActionTouchZone.gameObject != null)
        {
            // If it somehow became inactive (restart), clear it.
            if (mainActionTouchZone.gameObject.activeInHierarchy) return;
            mainActionTouchZone = null;
        }

        // Resources.FindObjectsOfTypeAll finds inactive objects too — we filter for activeInHierarchy
        var zones = Resources.FindObjectsOfTypeAll<TapOnlyMainActionZone>();
        foreach (var z in zones)
        {
            if (z == null) continue;
            if (!z.gameObject.activeInHierarchy) continue;

            mainActionTouchZone = z;
            return;
        }
    }

    private void Update()
    {
        // Keyboard test
        if (Input.GetKeyDown(swipeDownKey))
        {
            OnSwipeDown();
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
        Debug.Log("PHONE: SWIPE DOWN Entered");
        if (isSliding) return;
        isSliding = true;

        // --- Keep your animation layer logic exactly ---
        if (animator != null && katanaLayerIndex >= 0)
        {
            animator.SetLayerWeight(katanaLayerIndex, 0f);
        }

        // Disable jump input during slide: disable the ACTIVE MainAction TouchZone
        TryFindMainActionTouchZoneActive();
        if (mainActionTouchZone != null)
        {
            // Disabling the component is safer than toggling GO active state across UI rebuilds
            mainActionTouchZone.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Slide");
            Debug.Log("PHONE: SWIPE DOWN");
        }

        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        yield return new WaitForSeconds(slideDuration);

        // --- Keep your animation layer logic exactly ---
        if (animator != null && katanaLayerIndex >= 0)
        {
            animator.SetLayerWeight(katanaLayerIndex, 1f);
        }

        // Re-enable jump input after slide
        // Re-acquire in case restart swapped canvases mid-slide
        TryFindMainActionTouchZoneActive();
        if (mainActionTouchZone != null)
        {
            mainActionTouchZone.enabled = true;
        }

        isSliding = false;
    }
}
