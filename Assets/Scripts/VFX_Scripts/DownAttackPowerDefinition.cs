using UnityEngine;

[CreateAssetMenu(menuName = "Elroi/VFX/Down Attack Power Definition", fileName = "DownAttackPowerDefinition")]
public class DownAttackPowerDefinition : ScriptableObject
{
    [Header("ID")]
    public string powerId = "Default";
    public DownAttackPowerId downAttackPowerId = DownAttackPowerId.Default;

    [Header("FX Prefabs (Particle Systems)")]
    [Tooltip("Spawns on the player when down attack is triggered (air FX).")]
    public GameObject downAttackAirFxPrefab;

    [Tooltip("Spawns on ground impact when slam hits (AOE FX).")]
    public GameObject groundImpactAoeFxPrefab;

    [Header("Air FX Attach (Local Offsets)")]
    public Vector3 airFxLocalPositionOffset = Vector3.zero;
    public Vector3 airFxLocalRotationOffset = Vector3.zero;
    public Vector3 airFxLocalScale = Vector3.one;

    [Header("Ground AOE FX (World Offsets)")]
    [Tooltip("Small lift to avoid z-fighting on the ground.")]
    public float groundFxYLift = 0.02f;

    [Header("Timing")]
    [Tooltip("Delay before showing air FX after swipe down is triggered (seconds).")]
    public float airFxDelay = 0.06f;

    [Tooltip("If your AOE prefab needs scaling based on gameplay radius, enable this.")]
    public bool scaleGroundFxByRadius = true;

    [Header("Ground AOE FX Rotation")]
    [Tooltip("If true, use a fixed Euler rotation for the ground AOE FX.")]
    public bool useFixedGroundRotation = true;

    [Tooltip("Fixed rotation applied to the AOE impact FX (Euler angles). Common: (90,0,0) or (-90,0,0) depending on prefab.")]
    public Vector3 groundFxFixedEulerRotation = new Vector3(-90f, 0f, 0f);

    [Tooltip("If false and this is true, rotate AOE to match player's Y rotation (useful for directional shockwaves).")]
    public bool matchPlayerYawIfNotFixed = false;

    [Tooltip("Many ring/shockwave style FX expect DIAMETER scaling = radius*2. Use multiplier to match your prefab.")]
    public float groundFxRadiusToScaleMultiplier = 2f;

    [Tooltip("Extra multiplier after radius->scale conversion.")]
    public float groundFxScaleMultiplier = 1f;
}

public enum DownAttackPowerId
{
    None = 0,

    // Add your IDs here
    Default = 1,
    Electric = 2,
    Fire = 3,
    // ...
}