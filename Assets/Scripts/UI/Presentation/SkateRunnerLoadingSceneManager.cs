using System.Collections;
using MoreMountains.Tools;
using MoreMountains.InfiniteRunnerEngine;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
    [SerializeField, Min(0f)] private float readyHoldDuration = 0.2f;
    [SerializeField, Min(1f)] private float readinessTimeout = 45f;

    private CanvasGroup loadingCanvasGroup;
    
    private bool previousAudioListenerPause;
    private bool audioListenerPauseCaptured;
private GameObject presentationRoot;


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

        PreparePersistentPresentation();
        StartCoroutine(LoadSkateRunnerScene());
    }

    // Progress is advanced by the active loading coroutine, so this scene adds
    // no permanent per-frame Update workload.
    protected override void Update() { }

private IEnumerator LoadSkateRunnerScene()
    {
        float startedAt = Time.realtimeSinceStartup;
        float shownAt = startedAt;
        Debug.Log($"[SkateRunnerLoading] stage=begin destination={_sceneToLoad} elapsedMs=0", this);
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.BeforeEntryFade);

        if (StartFadeDuration > 0f)
        {
            LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.EntryFade);
            MMFadeOutEvent.Trigger(StartFadeDuration, _tween);
            yield return WaitRealtime(StartFadeDuration);
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.AfterEntryFade);
        yield return null;

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadDestinationScene);
        float asyncLoadStartedAt = Time.realtimeSinceStartup;
        _asyncOperation = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
        if (_asyncOperation == null)
        {
            Debug.LogError($"[SkateRunnerLoading] Could not begin loading '{_sceneToLoad}'.", this);
            DestroyPersistentPresentation();
            yield break;
        }

        _asyncOperation.allowSceneActivation = false;
        float displayed = 0f;

        while (_asyncOperation.progress < 0.9f)
        {
            float target = Mathf.Clamp(_asyncOperation.progress, 0f, 0.9f);
            displayed = AdvanceWithoutRegression(displayed, target);
            SetDisplayedProgress(displayed);
            yield return null;
        }

        while (displayed < 0.9f)
        {
            displayed = AdvanceWithoutRegression(displayed, 0.9f);
            SetDisplayedProgress(displayed);
            yield return null;
        }

        SetDisplayedProgress(0.9f);
        Debug.Log($"[SkateRunnerLoading] stage=async-loaded elapsedMs={(Time.realtimeSinceStartup - startedAt) * 1000f:0} loadMs={(Time.realtimeSinceStartup - asyncLoadStartedAt) * 1000f:0}", this);
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadProgressComplete);

        float earliestActivation = shownAt + minimumDisplayDuration;
        while (Time.realtimeSinceStartup < earliestActivation)
        {
            yield return null;
        }

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.BeforeSceneActivation);
        float activationStartedAt = Time.realtimeSinceStartup;
        _asyncOperation.allowSceneActivation = true;

        while (!_asyncOperation.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SkateRunnerLoading] stage=scene-activated elapsedMs={(Time.realtimeSinceStartup - startedAt) * 1000f:0} activationMs={(Time.realtimeSinceStartup - activationStartedAt) * 1000f:0}", this);
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.DestinationSceneActivation);

        float readinessStartedAt = Time.realtimeSinceStartup;
        yield return WaitForDestinationReady(_sceneToLoad);

        Debug.Log($"[SkateRunnerLoading] stage=destination-ready elapsedMs={(Time.realtimeSinceStartup - startedAt) * 1000f:0} readinessMs={(Time.realtimeSinceStartup - readinessStartedAt) * 1000f:0}", this);

        while (displayed < 1f)
        {
            displayed = AdvanceWithoutRegression(displayed, 1f);
            SetDisplayedProgress(displayed);
            yield return null;
        }

        SetDisplayedProgress(1f);
        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.InterpolatedLoadProgressComplete);
        yield return WaitRealtime(readyHoldDuration);

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.ExitFade);
        yield return FadeOutPresentation(ExitFadeDuration);

        LoadingSceneEvent.Trigger(_sceneToLoad, LoadingStatus.LoadTransitionComplete);
        Debug.Log($"[SkateRunnerLoading] stage=complete elapsedMs={(Time.realtimeSinceStartup - startedAt) * 1000f:0}", this);
        DestroyPersistentPresentation();
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


private void PreparePersistentPresentation()
    {
        Canvas canvas = progressSlider != null ? progressSlider.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].gameObject.scene == gameObject.scene)
                {
                    canvas = canvases[i];
                    break;
                }
            }
        }

        if (canvas != null)
        {
            presentationRoot = canvas.transform.root.gameObject;
            loadingCanvasGroup = canvas.GetComponent<CanvasGroup>();
            if (loadingCanvasGroup == null)
            {
                loadingCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.interactable = false;
            loadingCanvasGroup.blocksRaycasts = true;
            DontDestroyOnLoad(presentationRoot);
        }

        previousAudioListenerPause = AudioListener.pause;
        audioListenerPauseCaptured = true;
        AudioListener.pause = true;

        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator WaitForDestinationReady(string sceneName)
    {
        float deadline = Time.realtimeSinceStartup + readinessTimeout;
        bool timedOut = false;

        while (!IsDestinationReady(sceneName))
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                timedOut = true;
                break;
            }
            yield return null;
        }

        if (timedOut)
        {
            Debug.LogWarning($"[SkateRunnerLoading] Readiness timed out after {readinessTimeout:0.0}s for '{sceneName}'. The loading overlay will release to avoid a permanent block.", this);
        }

        yield return new WaitForEndOfFrame();
    }

    private static bool IsDestinationReady(string sceneName)
    {
        Scene destination = SceneManager.GetSceneByName(sceneName);
        if (!destination.IsValid() || !destination.isLoaded || SceneManager.GetActiveScene() != destination)
        {
            return false;
        }

        if (Camera.main == null)
        {
            return false;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool destinationCanvasReady = false;
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.scene == destination && canvases[i].enabled)
            {
                destinationCanvasReady = true;
                break;
            }
        }

        if (!destinationCanvasReady)
        {
            return false;
        }

        if (!string.Equals(sceneName, "SkateRunner", System.StringComparison.Ordinal))
        {
            return true;
        }

        SkateAssassinRunnerLevelManager levelManager = SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor;
        if (levelManager == null || levelManager.CurrentPlayableCharacters == null || levelManager.CurrentPlayableCharacters.Count == 0)
        {
            return false;
        }

        if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor == null)
        {
            return false;
        }

        GUIManager guiManager = GUIManager.Instance;
        return guiManager == null || guiManager.Fader == null || guiManager.Fader.color.a <= 0.01f;
    }

    private IEnumerator FadeOutPresentation(float duration)
    {
        if (loadingCanvasGroup == null || duration <= 0f)
        {
            yield break;
        }

        float startedAt = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startedAt < duration)
        {
            float normalized = Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / duration);
            loadingCanvasGroup.alpha = 1f - normalized;
            yield return null;
        }

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = false;
    }

private void DestroyPersistentPresentation()
    {
        RestoreAudioListenerPause();

        if (presentationRoot != null && presentationRoot != gameObject)
        {
            Destroy(presentationRoot);
        }
        Destroy(gameObject);
    }


private void RestoreAudioListenerPause()
    {
        if (!audioListenerPauseCaptured)
        {
            return;
        }

        AudioListener.pause = previousAudioListenerPause;
        audioListenerPauseCaptured = false;
    }

    private void OnDestroy()
    {
        RestoreAudioListenerPause();
    }
}
