using MoreMountains.InfiniteRunnerEngine;
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

    private bool showRequested;
    private bool observedRuthlessMode;
    private bool visible;
    private bool editorTestOverride;
    private int speedlinesStateHash;
    private bool warnedMissingSetup;

    public bool IsVisible => visible;
    public bool IsWaitingForCamera => showRequested;

    private void Awake()
    {
        speedlinesStateHash = Animator.StringToHash(speedlinesStateName);
        if (speedlinesAnimator != null)
            speedlinesAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        HideImmediate();
    }

    private void OnEnable()
    {
        ActiveInstance = this;
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

    private void ShowNow(bool testOverride)
    {
        showRequested = false;
        observedRuthlessMode = IsRuthlessModeActive();
        editorTestOverride = testOverride;
        if (speedlinesRoot == null || speedlinesAnimator == null) return;

        speedlinesRoot.SetActive(true);
        speedlinesAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
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
