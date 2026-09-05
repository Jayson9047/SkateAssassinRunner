using System.Collections;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Skate Runner's loading-screen-specific presentation. The public
/// MMSceneLoadingManager.LoadScene entry point remains unchanged.
/// </summary>
[DisallowMultipleComponent]
public sealed class SkateRunnerLoadingSceneManager : MMSceneLoadingManager
{
    [Header("Skate Runner Loading Presentation")]
    [SerializeField, Min(0f)] private float minimumDisplayDuration = 3f;
    [SerializeField, Min(0.01f)] private float visualProgressSpeed = 1.75f;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private Color progressFillColor = new Color(1f, 0.36f, 0.05f, 1f);

    protected override void Start()
    {
        Time.timeScale = 1f;
        _tween = new MMTweenType(MMTween.MMTweenCurve.EaseOutCubic);

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.value = 0f;
        }

        if (progressFill != null)
        {
            progressFill.color = progressFillColor;
            progressFill.raycastTarget = false;
        }

        SetDisplayedProgress(0f);

        if (string.IsNullOrWhiteSpace(_sceneToLoad))
        {
            Debug.LogWarning("[SkateRunnerLoading] Opened directly with no destination scene. Waiting at 0% for authoring preview.", this);
            return;
        }

        StartCoroutine(LoadSkateRunnerScene());
    }

    // Progress is advanced by the active loading coroutine, so this scene adds
    // no permanent per-frame Update workload.
    protected override void Update() { }

    private IEnumerator LoadSkateRunnerScene()
    {
        float shownAt = Time.realtimeSinceStartup;
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.BeforeEntryFade);

        if (StartFadeDuration > 0f)
        {
            LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.EntryFade);
            MMFadeOutEvent.Trigger(StartFadeDuration, _tween);
            yield return WaitRealtime(StartFadeDuration);
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.AfterEntryFade);
        // Give the fully-authored loading frame one render before beginning IO.
        yield return null;

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadDestinationScene);
        _asyncOperation = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
        if (_asyncOperation == null)
        {
            Debug.LogError($"[SkateRunnerLoading] Could not begin loading '{_sceneToLoad}'.", this);
            yield break;
        }

        _asyncOperation.allowSceneActivation = false;
        float displayed = 0f;

        while (_asyncOperation.progress < 0.9f)
        {
            float realNormalized = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
            displayed = AdvanceWithoutRegression(displayed, realNormalized);
            SetDisplayedProgress(displayed);
            yield return null;
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadProgressComplete);

        while (displayed < 1f)
        {
            displayed = AdvanceWithoutRegression(displayed, 1f);
            SetDisplayedProgress(displayed);
            yield return null;
        }

        SetDisplayedProgress(1f);
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.InterpolatedLoadProgressComplete);

        float earliestActivation = shownAt + minimumDisplayDuration;
        while (Time.realtimeSinceStartup < earliestActivation)
        {
            yield return null;
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.BeforeSceneActivation);
        if (ExitFadeDuration > 0f)
        {
            LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.ExitFade);
            MMFadeInEvent.Trigger(ExitFadeDuration, _tween);
            yield return WaitRealtime(ExitFadeDuration);
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.DestinationSceneActivation);
        _asyncOperation.allowSceneActivation = true;
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadTransitionComplete);
    }

    private float AdvanceWithoutRegression(float current, float target)
    {
        target = Mathf.Max(current, Mathf.Clamp01(target));
        return Mathf.MoveTowards(current, target, visualProgressSpeed * Time.unscaledDeltaTime);
    }

    private void SetDisplayedProgress(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(normalized);
        }

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
        }
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float endTime = Time.realtimeSinceStartup + Mathf.Max(0f, duration);
        while (Time.realtimeSinceStartup < endTime)
        {
            yield return null;
        }
    }
}
