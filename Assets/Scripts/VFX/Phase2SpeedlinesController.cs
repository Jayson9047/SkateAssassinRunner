using MoreMountains.InfiniteRunnerEngine;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Phase2SpeedlinesController : MonoBehaviour
{
    public static Phase2SpeedlinesController ActiveInstance { get; private set; }

    [Header("Authored Setup")]
    [SerializeField] private GameObject speedlinesRoot;
    [SerializeField] private Animator speedlinesAnimator;
    [SerializeField] private string speedlinesStateName = "speedlines";
    [SerializeField] private Phase2CameraDirector cameraDirector;

    [Header("Fullscreen UI Layout")]
    [SerializeField] private RectTransform speedlineViewport;
    [SerializeField] private RectTransform speedlineRotator;
    [SerializeField] private float speedlineRotationDegrees = -25f;
    [SerializeField] private Vector2 speedlineScreenOffset;
    [SerializeField] private bool autoCoverRotatedViewport = true;
    [SerializeField, Min(1f)] private float overscanMultiplier = 1.05f;
    [SerializeField, Min(0.01f)] private float animationSpeed = 1f;

    [Header("Enemy-Above-Speedlines Rendering")]
    [SerializeField] private Camera subjectOverlayCamera;
    [SerializeField] private int subjectVisualLayer = 16;

    private bool showRequested;
    private bool observedRuthlessMode;
    private bool visible;
    private bool editorTestOverride;
    private int speedlinesStateHash;
    private bool warnedMissingSetup;
    private Transform subjectRoot;
    private readonly List<GameObject> subjectVisualObjects = new List<GameObject>(24);
    private readonly List<int> subjectOriginalLayers = new List<int>(24);

    public bool IsVisible => visible;
    public bool IsWaitingForCamera => showRequested;

    private void Awake()
    {
        speedlinesStateHash = Animator.StringToHash(speedlinesStateName);
        if (speedlinesAnimator != null)
            speedlinesAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        ApplyLayout();
        HideImmediate();
    }

    private void OnEnable()
    {
        ActiveInstance = this;
        ApplyLayout();
    }

    private void Update()
    {
        bool ruthlessActive = IsRuthlessModeActive();

        if (visible && !ruthlessActive && !editorTestOverride)
        {
            HideImmediate();
            return;
        }

        if (!showRequested) return;

        if (ruthlessActive)
            observedRuthlessMode = true;
        else if (observedRuthlessMode)
        {
            HideImmediate();
            return;
        }

        if (ruthlessActive && cameraDirector != null && cameraDirector.IsCollisionCameraSettled)
            ShowNow(false);
    }

    public void ShowWhenCollisionCameraSettled()
    {
        if (speedlinesRoot == null || speedlinesAnimator == null || cameraDirector == null)
        {
            WarnMissingSetupOnce();
            HideImmediate();
            return;
        }
        HideRootOnly();
        showRequested = true;
        observedRuthlessMode = IsRuthlessModeActive();
    }

    public void HideImmediate()
    {
        showRequested = false;
        observedRuthlessMode = false;
        editorTestOverride = false;
        HideRootOnly();
    }

    public void ShowImmediateForTest()
    {
        ShowNow(true);
    }

    public void SetTestRotation(float degrees)
    {
        speedlineRotationDegrees = degrees;
        ApplyLayout();
    }

    /// <summary>Registers only the launched Enemy Type 3 visual root; no physics object is modified.</summary>
    public void RegisterSubject(Transform root)
    {
        RestoreSubjectLayers();
        subjectRoot = root;
        if (visible)
            ApplySubjectLayer();
    }

    private void ShowNow(bool testOverride)
    {
        showRequested = false;
        observedRuthlessMode = IsRuthlessModeActive();
        editorTestOverride = testOverride;
        if (speedlinesRoot == null || speedlinesAnimator == null) return;

        speedlinesRoot.SetActive(true);
        speedlinesAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        speedlinesAnimator.speed = animationSpeed;
        ApplyLayout();
        ApplySubjectLayer();
        if (subjectOverlayCamera != null)
        {
            Phase2SubjectCameraSync sync = subjectOverlayCamera.GetComponent<Phase2SubjectCameraSync>();
            if (sync != null) sync.SynchronizeNow();
            subjectOverlayCamera.enabled = true;
        }
        speedlinesAnimator.Play(speedlinesStateHash, 0, 0f);
        speedlinesAnimator.Update(0f);
        visible = true;
    }

    private bool IsRuthlessModeActive()
    {
        if (RuthlessTapModeController.Instance != null)
            return RuthlessTapModeController.Instance.IsActive;
        return LevelManager.Instance != null && LevelManager.Instance.RuthlessTapModeEntered;
    }

    private void HideRootOnly()
    {
        visible = false;
        if (speedlinesRoot != null) speedlinesRoot.SetActive(false);
        if (subjectOverlayCamera != null) subjectOverlayCamera.enabled = false;
        RestoreSubjectLayers();
    }

    private void ApplySubjectLayer()
    {
        RestoreSubjectLayers();
        if (subjectRoot == null || subjectVisualLayer < 0 || subjectVisualLayer > 31)
            return;

        Renderer[] renderers = subjectRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            GameObject visualObject = renderers[i].gameObject;
            if (visualObject.GetComponent<Collider>() != null)
            {
                Debug.LogWarning("[Phase 2 Speedlines] A visual object also owns a Collider and was left on its gameplay layer: " + visualObject.name, visualObject);
                continue;
            }
            if (subjectVisualObjects.Contains(visualObject))
                continue;

            subjectVisualObjects.Add(visualObject);
            subjectOriginalLayers.Add(visualObject.layer);
            visualObject.layer = subjectVisualLayer;
        }
    }

    private void RestoreSubjectLayers()
    {
        for (int i = 0; i < subjectVisualObjects.Count; i++)
        {
            if (subjectVisualObjects[i] != null)
                subjectVisualObjects[i].layer = subjectOriginalLayers[i];
        }
        subjectVisualObjects.Clear();
        subjectOriginalLayers.Clear();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        overscanMultiplier = Mathf.Max(1f, overscanMultiplier);
        animationSpeed = Mathf.Max(0.01f, animationSpeed);
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (speedlineViewport == null || speedlineRotator == null)
            return;

        Rect rect = speedlineViewport.rect;
        float width = Mathf.Max(1f, rect.width);
        float height = Mathf.Max(1f, rect.height);
        float radians = speedlineRotationDegrees * Mathf.Deg2Rad;
        float cosine = Mathf.Abs(Mathf.Cos(radians));
        float sine = Mathf.Abs(Mathf.Sin(radians));
        Vector2 coveredSize = autoCoverRotatedViewport
            ? new Vector2(width * cosine + height * sine, width * sine + height * cosine)
            : new Vector2(width, height);

        speedlineRotator.localRotation = Quaternion.Euler(0f, 0f, speedlineRotationDegrees);
        speedlineRotator.anchoredPosition = speedlineScreenOffset;
        speedlineRotator.sizeDelta = coveredSize * overscanMultiplier;
    }

    private void WarnMissingSetupOnce()
    {
        if (warnedMissingSetup) return;
        warnedMissingSetup = true;
        Debug.LogWarning("[Phase 2 Speedlines] Missing root, Animator, or camera director; presentation is disabled and gameplay continues.", this);
    }

    private void OnDisable()
    {
        HideImmediate();
        if (ActiveInstance == this) ActiveInstance = null;
    }
}
