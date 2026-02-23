using UnityEngine;

[CreateAssetMenu(menuName = "Elroi/VFX/Weapon Power Definition", fileName = "WP_")]
public class WeaponPowerDefinition : ScriptableObject
{
    [Header("ID")]
    public string powerId; // "Fire", "Ice", etc. (optional but useful)

    [Header("Weapon Aura (looping, attached to weapon)")]
    public GameObject weaponAuraPrefab;

    [Header("Slash FX (one-shot, spawned on slash)")]
    public GameObject slashFxPrefab;

    [Header("Optional tuning")]
    public Vector3 auraLocalPositionOffset;
    public Vector3 auraLocalRotationOffset;
    public Vector3 auraLocalScale = Vector3.one;

    public Vector3 slashPositionOffset;
    public Vector3 slashRotationOffset;
    public Vector3 slashScale = Vector3.one;
    public WeaponPowerId weaponPowerId = WeaponPowerId.None;

    [System.Serializable]
    public class WeaponPowerTuningOverride
    {
        [Header("Weapon Key")]
        public string weaponId;

        [Tooltip("Optional: reference the weapon prefab for convenience in editor. Runtime matching uses weaponId.")]
        public GameObject weaponPrefab;

        [Header("Aura Tuning")]
        public Vector3 auraLocalPositionOffset;
        public Vector3 auraLocalRotationOffset;
        public Vector3 auraLocalScale = Vector3.one;

        [Header("Slash Tuning")]
        public Vector3 slashPositionOffset;
        public Vector3 slashRotationOffset;
        public Vector3 slashScale = Vector3.one;
    }

    [Header("Per-Weapon Overrides (optional)")]
    public WeaponPowerTuningOverride[] perWeaponOverrides;

    public bool TryGetOverride(string weaponId, out WeaponPowerTuningOverride found)
    {
        found = null;

        if (string.IsNullOrEmpty(weaponId) || perWeaponOverrides == null)
            return false;

        for (int i = 0; i < perWeaponOverrides.Length; i++)
        {
            var o = perWeaponOverrides[i];
            if (o == null) continue;
            if (!string.IsNullOrEmpty(o.weaponId) && o.weaponId == weaponId)
            {
                found = o;
                return true;
            }
        }

        return false;
    }

    public Vector3 GetAuraPos(string weaponId)
    {
        return TryGetOverride(weaponId, out var o) ? o.auraLocalPositionOffset : auraLocalPositionOffset;
    }
    public Vector3 GetAuraRot(string weaponId)
    {
        return TryGetOverride(weaponId, out var o) ? o.auraLocalRotationOffset : auraLocalRotationOffset;
    }
    public Vector3 GetAuraScale(string weaponId)
    {
        Vector3 s = TryGetOverride(weaponId, out var o) ? o.auraLocalScale : auraLocalScale;
        return (s == Vector3.zero) ? Vector3.one : s;
    }

    public Vector3 GetSlashPos(string weaponId)
    {
        return TryGetOverride(weaponId, out var o) ? o.slashPositionOffset : slashPositionOffset;
    }
    public Vector3 GetSlashRot(string weaponId)
    {
        return TryGetOverride(weaponId, out var o) ? o.slashRotationOffset : slashRotationOffset;
    }
    public Vector3 GetSlashScale(string weaponId)
    {
        Vector3 s = TryGetOverride(weaponId, out var o) ? o.slashScale : slashScale;
        return (s == Vector3.zero) ? Vector3.one : s;
    }
}


public enum WeaponPowerId
{
    None = 0,
    Ice = 1,
    Fire = 2,
    Electricity = 3,
    Poison = 4,
    Magic = 5,
    // add more later
}

