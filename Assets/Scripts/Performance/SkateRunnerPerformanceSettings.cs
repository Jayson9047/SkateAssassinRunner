using UnityEngine;

public enum SkateRunnerPerformanceTier
{
    Low,
    High
}

public enum SkateRunnerPerformanceOverride
{
    Auto,
    ForceLow,
    ForceHigh
}

[CreateAssetMenu(fileName = "SkateRunnerPerformanceSettings", menuName = "Skate Assassin Runner/Performance Settings")]
public sealed class SkateRunnerPerformanceSettings : ScriptableObject
{
    [Header("Development")]
    [Tooltip("Internal development override. This is intentionally not exposed in the player-facing Settings UI.")]
    public SkateRunnerPerformanceOverride developerOverride = SkateRunnerPerformanceOverride.Auto;

    [Header("Quality Levels")]
    public string lowQualityLevelName = "Fast";
    public string highQualityLevelName = "Fantastic";
    [Header("Frame Rate")]
    [Tooltip("Shared production frame-rate target. Graphics tier changes rendering cost, not gameplay smoothness.")]
    [Min(30)] public int targetFrameRate = 60;
    [Range(0, 2)] public int lowTextureMipmapLimit = 1;

    [Header("Automatic Android Classification")]
    [Tooltip("A device at or below any positive threshold is assigned to Low. Unknown (zero) values are ignored.")]
    [Min(0)] public int lowSystemMemoryThresholdMb = 4096;
    [Min(0)] public int lowGraphicsMemoryThresholdMb = 1024;
    [Min(0)] public int lowProcessorCountThreshold = 6;
    [Min(0)] public int lowShaderLevelThreshold = 40;

    [Header("Low Tier Presentation")]
    [Tooltip("The two sliced Enemy 1 body meshes always remain. Only the short blood particles are optional.")]
    public bool lowEnableEnemy1BloodParticles = false;
    [Range(0f, 2f)] public float lowBloomIntensity = 0.65f;
    [Range(1, 8)] public int lowBloomMaxIterations = 3;
}
