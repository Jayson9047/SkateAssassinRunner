using UnityEngine;

[CreateAssetMenu(menuName = "Elroi/VFX/Weapon Power Screen Slash Palette", fileName = "WeaponPowerScreenSlashPalette")]
public sealed class WeaponPowerScreenSlashPalette : ScriptableObject
{
    [System.Serializable]
    public struct PowerColorEntry
    {
        public WeaponPowerId power;
        [ColorUsage(false, true)] public Color primaryColor;

        public PowerColorEntry(WeaponPowerId power, Color primaryColor)
        {
            this.power = power;
            this.primaryColor = primaryColor;
        }
    }

    [SerializeField] private PowerColorEntry[] colors =
    {
        new PowerColorEntry(WeaponPowerId.None,        new Color32(0x4E, 0x9D, 0xFF, 0xFF)),
        new PowerColorEntry(WeaponPowerId.Ice,         new Color32(0xD8, 0xF4, 0xFF, 0xFF)),
        new PowerColorEntry(WeaponPowerId.Electricity, new Color32(0x3F, 0x5B, 0xE8, 0xFF)),
        new PowerColorEntry(WeaponPowerId.Fire,        new Color32(0xFF, 0x9F, 0x2F, 0xFF)),
        new PowerColorEntry(WeaponPowerId.Poison,      new Color32(0x63, 0xE0, 0x6B, 0xFF)),
        new PowerColorEntry(WeaponPowerId.Magic,       new Color32(0xA9, 0x5C, 0xFF, 0xFF))
    };

    private readonly Color[] cachedColors = new Color[6];
    private readonly bool[] cachedEntries = new bool[6];
    private bool cacheReady;

    public Color GetPrimaryColor(WeaponPowerId power)
    {
        if (!cacheReady) BuildCache();
        int index = (int)power;
        return index >= 0 && index < cachedColors.Length && cachedEntries[index]
            ? cachedColors[index]
            : new Color32(0x4E, 0x9D, 0xFF, 0xFF);
    }

    public void Warmup()
    {
        if (!cacheReady) BuildCache();
    }

    private void OnEnable() => BuildCache();

#if UNITY_EDITOR
    private void OnValidate() => BuildCache();
#endif

    private void BuildCache()
    {
        for (int i = 0; i < cachedEntries.Length; i++)
        {
            cachedEntries[i] = false;
            cachedColors[i] = default;
        }

        if (colors != null)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                int index = (int)colors[i].power;
                if (index < 0 || index >= cachedColors.Length) continue;
                cachedColors[index] = colors[i].primaryColor;
                cachedEntries[index] = true;
            }
        }

        cacheReady = true;
    }
}
