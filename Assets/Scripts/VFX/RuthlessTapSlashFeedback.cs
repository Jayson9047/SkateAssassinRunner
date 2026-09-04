using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FronkonGames.SpiceUp.Slash;
using SlashFeature = FronkonGames.SpiceUp.Slash.Slash;

/// <summary>One screen-space slash. Retriggering replaces its state; nothing is queued or spawned.</summary>
[DisallowMultipleComponent]
public sealed class RuthlessTapSlashFeedback : MonoBehaviour
{
    [Header("Authored Setup")]
    [Tooltip("Dedicated global Volume containing only Slash. One private runtime profile is cached at initialization; the authored asset stays idle.")]
    [SerializeField] private Volume slashVolume;
    [Tooltip("The gameplay camera, not the UI overlay. Post-processing must already be enabled.")]
    [SerializeField] private Camera gameplayCamera;

    [Header("General")]
    [SerializeField] private bool feedbackEnabled = true;
    [SerializeField, Range(0.10f, 0.35f)] private float slashDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float peakIntensity = 0.85f;
    [Tooltip("Hold full intensity until this fraction of the lifetime, then SmoothStep to zero.")]
    [SerializeField, Range(0f, 0.95f)] private float fadeStartNormalized = 0.4f;
    [Tooltip("Fronkon's reveal reaches full strength at 0.03. Zero would hide the first frame.")]
    [SerializeField, Range(0.001f, 0.1f)] private float startProgress = 0.03f;

    [Header("Rotation")]
    [SerializeField, Range(0f, 359.9f)] private float minimumAngle;
    [SerializeField, Range(0.1f, 360f)] private float maximumAngle = 360f;
    [Tooltip("Circular separation in degrees. Narrow ranges limit this to half their span so a legal next angle always exists.")]
    [SerializeField, Range(0.1f, 179f)] private float minimumConsecutiveAngleDifference = 35f;

    [Header("Size / Impact")]
    [Tooltip("Scales core/smoke widths and expansion. Glow falloff is divided by this value so larger means wider.")]
    [SerializeField, Range(0.25f, 3f)] private float visualImpactScale = 1f;

    [Header("Advanced — Slash Shape")]
    [SerializeField, Range(0f, 1f)] private float splitDist = 0.015f;
    [SerializeField, Range(0f, 1f)] private float distortPower = 0.012f;
    [SerializeField, Range(0.04f, 1f)] private float slashFade = 0.85f;
    [SerializeField, Range(0.0001f, 0.1f)] private float coreWidth = 0.007f;
    [Tooltip("Higher values produce a narrower glow: this is exponential falloff, not a radius.")]
    [SerializeField, Range(1f, 100f)] private float glowSpread = 60f;

    [Header("Advanced — Colors")]
    [SerializeField] private Color glowColor = new Color(1f, 0.82f, 0.68f, 0.8f);
    [Tooltip("Alpha zero hides light smoke, but does not eliminate the vendor shader's noise calculations.")]
    [SerializeField] private Color smokeColor1 = new Color(0.9f, 0.9f, 0.95f, 0.12f);
    [Tooltip("Alpha zero hides dark smoke.")]
    [SerializeField] private Color smokeColor2 = new Color(0.02f, 0.02f, 0.02f, 0.1f);
    [SerializeField] private Color backgroundColor = Color.black;

    [Header("Advanced — Smoke")]
    [SerializeField, Range(0.21f, 1f)] private float smokeFade = 0.55f;
    [SerializeField, Range(0f, 1f)] private float smokeExpand = 0.12f;
    [SerializeField, Range(0.0001f, 1f)] private float smokeSize1 = 0.1f;
    [SerializeField, Range(0.0001f, 1f)] private float smokeSize2 = 0.16f;

    [Header("Advanced — Color Grading")]
    [SerializeField, Range(-1f, 1f)] private float brightness;
    [SerializeField, Range(0f, 10f)] private float contrast = 1f;
    [SerializeField, Range(0.1f, 10f)] private float gamma = 1f;
    [SerializeField, Range(0f, 1f)] private float hue;
    [SerializeField, Range(0f, 2f)] private float saturation = 1f;

    private static RuthlessTapSlashFeedback owner;
    private SlashVolume slash;
    private SlashFeature rendererFeature;
    private VolumeProfile runtimeProfile;
    private bool ownsProfile;
    private bool warned;
    private bool slashActive;
    private bool hasPreviousAngle;
    private float elapsed;
    private float previousAngle;
    private int triggerFrame = -1;
    private uint randomState;

    public bool IsSlashActive => slashActive;
    public float CurrentAngle => previousAngle;
    public float CurrentIntensity => slash != null ? slash.intensity.value : 0f;
    public float CurrentProgress => slash != null ? slash.progress.value : 0f;
    public float Duration => slashDuration;
    public bool IsReady => slash != null && rendererFeature != null;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        ClampSettings();
        if (owner != null && owner != this)
        {
            FailSetup("another Ruthless Tap Slash controller already owns the screen effect");
            return;
        }
        if (slash == null && !Initialize()) return;
        owner = this;
        StopImmediate();
    }

    private bool Initialize()
    {
        if (slashVolume == null || slashVolume.sharedProfile == null || !slashVolume.isGlobal ||
            !slashVolume.sharedProfile.TryGet<SlashVolume>(out _))
            return FailSetup("assign a dedicated global Volume/Profile containing SlashVolume");
        if (gameplayCamera == null || !gameplayCamera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) ||
            !cameraData.renderPostProcessing || (cameraData.volumeLayerMask.value & (1 << slashVolume.gameObject.layer)) == 0)
            return FailSetup("the gameplay camera must enable post-processing and include the Slash Volume layer");
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            return FailSetup("an active Universal Render Pipeline is required");

        // Public Fronkon helper uses reflection: initialization only, never in the tap path.
        try
        {
            var features = SlashFeature.Instances;
            int activeFeatures = 0;
            for (int i = 0; i < features.Length; ++i)
                if (features[i] != null && features[i].isActive)
                {
                    rendererFeature = features[i];
                    activeFeatures++;
                }
            if (activeFeatures != 1)
                return FailSetup("the gameplay pipeline must contain exactly one active Fronkon Slash Renderer Feature");
        }
        catch (System.Exception)
        {
            return FailSetup("the Fronkon Slash Renderer Feature could not be resolved");
        }

        ownsProfile = !slashVolume.HasInstantiatedProfile();
        runtimeProfile = slashVolume.profile;
        if (!runtimeProfile.TryGet(out slash)) return FailSetup("the runtime profile has no SlashVolume override");
        slash.SetAllOverridesTo(true);
        slash.active = true;
        ApplyVisualSettings();
        StopImmediate();
        // Cosmetic randomness must not consume Cash/recoil/gameplay's UnityEngine.Random stream.
        randomState = unchecked((uint)System.Environment.TickCount ^ (uint)GetInstanceID());
        if (randomState == 0) randomState = 0x9E3779B9u;
        return true;
    }

    /// <summary>Immediately replaces the current slash at full peak strength. No allocations.</summary>
    public void TriggerSlash()
    {
        if (!isActiveAndEnabled || !feedbackEnabled || slash == null || rendererFeature == null ||
            !rendererFeature.isActive || slashVolume == null || !slashVolume.isActiveAndEnabled ||
            gameplayCamera == null || !gameplayCamera.isActiveAndEnabled) return;
        elapsed = 0f;
        triggerFrame = Time.frameCount;
        previousAngle = ChooseAngle();
        hasPreviousAngle = true;
        slash.angle.value = previousAngle;
        ApplyVisualSettings();
        slash.progress.value = startProgress;
        slash.intensity.value = peakIntensity;
        slashActive = true;
    }

    private void LateUpdate()
    {
        if (!slashActive) return;
        if (!feedbackEnabled || slash == null || slashVolume == null || !slashVolume.isActiveAndEnabled ||
            rendererFeature == null || !rendererFeature.isActive || gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
        {
            StopImmediate();
            return;
        }
        // Pointer events precede LateUpdate: preserve peak strength on the triggering render frame.
        if (Time.frameCount != triggerFrame) Advance(Time.unscaledDeltaTime);
    }

    private void Advance(float deltaTime)
    {
        elapsed += deltaTime;
        float t = Mathf.Clamp01(elapsed / slashDuration);
        if (t >= 1f) { StopImmediate(); return; }
        slash.progress.value = Mathf.Lerp(startProgress, 1f, t);
        float fade = Mathf.Clamp01((t - fadeStartNormalized) / (1f - fadeStartNormalized));
        slash.intensity.value = peakIntensity * (1f - Mathf.SmoothStep(0f, 1f, fade));
    }

    /// <summary>Clears glow, smoke and distortion, retaining angle history across taps.</summary>
    public void StopImmediate()
    {
        slashActive = false;
        elapsed = 0f;
        triggerFrame = -1;
        if (slash == null) return;
        slash.intensity.value = 0f;
        slash.progress.value = 0f;
    }

    private float ChooseAngle()
    {
        float candidate;
        for (int attempt = 0; attempt < 8; ++attempt)
        {
            candidate = Mathf.Lerp(minimumAngle, maximumAngle, NextRandom01());
            if (!hasPreviousAngle || Mathf.Abs(Mathf.DeltaAngle(previousAngle, candidate)) >= minimumConsecutiveAngleDifference)
                return candidate;
        }
        // Farthest legal point is an endpoint or the antipode: bounded deterministic fallback.
        candidate = minimumAngle;
        if (Mathf.Abs(Mathf.DeltaAngle(previousAngle, maximumAngle)) > Mathf.Abs(Mathf.DeltaAngle(previousAngle, candidate)))
            candidate = maximumAngle;
        float opposite = Mathf.Repeat(previousAngle + 180f, 360f);
        if (opposite >= minimumAngle && opposite <= maximumAngle) candidate = opposite;
        return candidate;
    }

    private float NextRandom01()
    {
        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;
        return (randomState >> 8) * (1f / 16777216f);
    }

    private void ApplyVisualSettings()
    {
        slash.useScaledTime.value = false;
        slash.affectSceneView.value = false;
        slash.splitDist.value = splitDist;
        slash.distortPower.value = distortPower;
        slash.slashFade.value = slashFade;
        // Positive widths avoid smoothstep(0, 0, x) in the vendor shader.
        slash.coreWidth.value = Mathf.Max(0.0001f, coreWidth * visualImpactScale);
        slash.glowSpread.value = glowSpread / visualImpactScale;
        slash.glowColor.value = glowColor;
        slash.glowColorBlend.value = ColorBlends.Additive;
        slash.smokeFade.value = smokeFade;
        slash.smokeExpand.value = smokeExpand * visualImpactScale;
        slash.smokeSize1.value = Mathf.Max(0.0001f, smokeSize1 * visualImpactScale);
        slash.smokeSize2.value = Mathf.Max(0.0001f, smokeSize2 * visualImpactScale);
        slash.smokeColor1.value = smokeColor1;
        slash.smokeColor2.value = smokeColor2;
        slash.smokeColor1Blend.value = ColorBlends.Additive;
        slash.smokeColor2Blend.value = ColorBlends.Darken;
        slash.backgroundColor.value = backgroundColor;
        slash.brightness.value = brightness;
        slash.contrast.value = contrast;
        slash.gamma.value = gamma;
        slash.hue.value = hue;
        slash.saturation.value = saturation;
    }

    private bool FailSetup(string reason)
    {
        if (!warned)
        {
            Debug.LogWarning("[Ruthless Tap Slash] Feedback disabled: " + reason + ". Ruthless Tap gameplay is unaffected.", this);
            warned = true;
        }
        enabled = false;
        return false;
    }

    private void OnDisable()
    {
        StopImmediate();
        if (owner == this) owner = null;
    }

    private void OnApplicationPause(bool paused) { if (paused) StopImmediate(); }

    private void OnDestroy()
    {
        StopImmediate();
        if (owner == this) owner = null;
        if (!ownsProfile || runtimeProfile == null) return;
        if (slashVolume != null && slashVolume.HasInstantiatedProfile() && slashVolume.profile == runtimeProfile)
            slashVolume.profile = null;
        for (int i = runtimeProfile.components.Count - 1; i >= 0; --i) Destroy(runtimeProfile.components[i]);
        Destroy(runtimeProfile);
    }

    private void OnValidate() => ClampSettings();

    private void ClampSettings()
    {
        slashDuration = Mathf.Clamp(slashDuration, 0.1f, 0.35f);
        peakIntensity = Mathf.Clamp01(peakIntensity);
        fadeStartNormalized = Mathf.Clamp(fadeStartNormalized, 0f, 0.95f);
        startProgress = Mathf.Clamp(startProgress, 0.001f, 0.1f);
        minimumAngle = Mathf.Clamp(minimumAngle, 0f, 359.9f);
        maximumAngle = Mathf.Clamp(maximumAngle, minimumAngle + 0.1f, 360f);
        float legalSeparation = Mathf.Min(179f, (maximumAngle - minimumAngle) * 0.5f);
        minimumConsecutiveAngleDifference = Mathf.Clamp(minimumConsecutiveAngleDifference, Mathf.Min(0.1f, legalSeparation), legalSeparation);
        visualImpactScale = Mathf.Clamp(visualImpactScale, 0.25f, 3f);
        slashFade = Mathf.Clamp(slashFade, 0.04f, 1f);
        smokeFade = Mathf.Clamp(smokeFade, 0.21f, 1f);
        // Remaining values are clamped by Fronkon's public VolumeParameter setters.
    }
}
