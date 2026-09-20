using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class SkateRunnerPerformanceManager : MonoBehaviour
{
    private const string ResourceName = "SkateRunnerPerformanceSettings";
    private static SkateRunnerPerformanceManager instance;
    private static SkateRunnerPerformanceSettings settings;

    public static SkateRunnerPerformanceTier CurrentTier { get; private set; } = SkateRunnerPerformanceTier.High;
    public static bool IsInitialized { get; private set; }
    public static bool OptionalEnemy1BloodEnabled =>
        CurrentTier == SkateRunnerPerformanceTier.High || (settings != null && settings.lowEnableEnemy1BloodParticles);
    public static event Action<SkateRunnerPerformanceTier> TierChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        settings = null;
        IsInitialized = false;
        CurrentTier = SkateRunnerPerformanceTier.High;
        TierChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject(nameof(SkateRunnerPerformanceManager));
        instance = host.AddComponent<SkateRunnerPerformanceManager>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        settings = Resources.Load<SkateRunnerPerformanceSettings>(ResourceName);
        if (settings == null)
            Debug.LogWarning("[Performance] Resources/SkateRunnerPerformanceSettings is missing. Safe defaults will be used.");

        CurrentTier = SelectTier(out string reason);
        ApplyCoreSettings(CurrentTier);
        IsInitialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneSettings(CurrentTier);
        TierChanged?.Invoke(CurrentTier);

        Debug.Log($"[Performance] tier={CurrentTier}, reason={reason}, targetFps={Application.targetFrameRate}, " +
                  $"RAM={SystemInfo.systemMemorySize}MB, VRAM={SystemInfo.graphicsMemorySize}MB, " +
                  $"CPU={SystemInfo.processorCount}, shaderLevel={SystemInfo.graphicsShaderLevel}.");
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

private static SkateRunnerPerformanceTier SelectTier(out string reason)
    {
        SkateRunnerPerformanceOverride developmentOverride =
            settings != null ? settings.developerOverride : SkateRunnerPerformanceOverride.Auto;

        if (developmentOverride == SkateRunnerPerformanceOverride.ForceLow)
        {
            reason = "development override";
            return SkateRunnerPerformanceTier.Low;
        }

        if (developmentOverride == SkateRunnerPerformanceOverride.ForceHigh)
        {
            reason = "development override";
            return SkateRunnerPerformanceTier.High;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        int knownSignals = 0;
        int lowSignals = 0;

        EvaluateCapability(
            SystemInfo.systemMemorySize,
            settings != null ? settings.lowSystemMemoryThresholdMb : 4096,
            ref knownSignals,
            ref lowSignals);
        EvaluateCapability(
            SystemInfo.graphicsMemorySize,
            settings != null ? settings.lowGraphicsMemoryThresholdMb : 1024,
            ref knownSignals,
            ref lowSignals);
        EvaluateCapability(
            SystemInfo.processorCount,
            settings != null ? settings.lowProcessorCountThreshold : 6,
            ref knownSignals,
            ref lowSignals);
        EvaluateCapability(
            SystemInfo.graphicsShaderLevel,
            settings != null ? settings.lowShaderLevelThreshold : 40,
            ref knownSignals,
            ref lowSignals);

        if (knownSignals < 3)
        {
            reason = "insufficient reliable Android capability signals";
            return SkateRunnerPerformanceTier.Low;
        }

        if (lowSignals > 0)
        {
            reason = "one or more Android capability thresholds";
            return SkateRunnerPerformanceTier.Low;
        }

        reason = "Android capability thresholds passed";
        return SkateRunnerPerformanceTier.High;
#else
        reason = "Editor/non-Android development default";
        return SkateRunnerPerformanceTier.High;
#endif
    }

private static void EvaluateCapability(int value, int threshold, ref int knownSignals, ref int lowSignals)
    {
        if (value <= 0 || threshold <= 0)
            return;

        knownSignals++;
        if (value <= threshold)
            lowSignals++;
    }

    private static void ApplyCoreSettings(SkateRunnerPerformanceTier tier)
    {
        bool low = tier == SkateRunnerPerformanceTier.Low;
        string qualityName = low
            ? (settings != null ? settings.lowQualityLevelName : "Fast")
            : (settings != null ? settings.highQualityLevelName : "Fantastic");

        int qualityIndex = Array.FindIndex(
            QualitySettings.names,
            name => string.Equals(name, qualityName, StringComparison.OrdinalIgnoreCase));

        if (qualityIndex >= 0)
            QualitySettings.SetQualityLevel(qualityIndex, true);
        else
            Debug.LogWarning($"[Performance] Quality level '{qualityName}' was not found; current quality was retained.");

        QualitySettings.vSyncCount = 0;
        QualitySettings.globalTextureMipmapLimit = low
            ? (settings != null ? settings.lowTextureMipmapLimit : 1)
            : 0;
        Application.targetFrameRate = settings != null ? settings.targetFrameRate : 60;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneSettings(CurrentTier);
        if (instance != null)
            instance.StartCoroutine(instance.ReapplyAfterSceneInitialization());
    }

    private IEnumerator ReapplyAfterSceneInitialization()
    {
        // Infinite Runner Engine's GameManager sets 300 in Start. Reapply once after
        // all scene Start methods; this has no recurring Update or allocation cost.
        yield return null;
        ApplyCoreSettings(CurrentTier);
        ApplySceneSettings(CurrentTier);
    }

    private static void ApplySceneSettings(SkateRunnerPerformanceTier tier)
    {
        if (tier != SkateRunnerPerformanceTier.Low)
            return;

        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            VolumeProfile profile = volumes[i].sharedProfile;
            if (profile == null)
                continue;

            DisableIfPresent<MotionBlur>(profile);
            DisableIfPresent<DepthOfField>(profile);
            DisableIfPresent<FilmGrain>(profile);
            DisableIfPresent<ChromaticAberration>(profile);
            DisableIfPresent<LensDistortion>(profile);
            DisableIfPresent<ScreenSpaceLensFlare>(profile);
            DisableIfPresent<PaniniProjection>(profile);

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.value = Mathf.Min(
                    bloom.intensity.value,
                    settings != null ? settings.lowBloomIntensity : 0.65f);
                bloom.maxIterations.value = Mathf.Min(
                    bloom.maxIterations.value,
                    settings != null ? settings.lowBloomMaxIterations : 3);
            }
        }
    }

    private static void DisableIfPresent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
            component.active = false;
    }
}
