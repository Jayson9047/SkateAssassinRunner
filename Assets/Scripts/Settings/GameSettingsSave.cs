using System;
using UnityEngine;

public enum GraphicsQualityMode
{
    Auto = 0,
    Low = 1,
    High = 2
}

/// <summary>
/// Persistent non-audio settings. Music and SFX remain owned by SoundManager.
/// </summary>
public static class GameSettingsSave
{
    public const string VibrationEnabledKey = "Settings.VibrationEnabled";
    public const string GraphicsQualityModeKey = "Settings.GraphicsQualityMode";
    public const string LanguageCodeKey = "Settings.LanguageCode";

    private const bool DefaultVibrationEnabled = true;
    private const GraphicsQualityMode DefaultGraphicsQualityMode = GraphicsQualityMode.Auto;
    private const string DefaultLanguageCode = "en";

    private static bool vibrationCached;
    private static bool cachedVibrationEnabled;

    public static bool IsVibrationEnabled()
    {
        if (!vibrationCached)
        {
            cachedVibrationEnabled = ES3.Load(VibrationEnabledKey, DefaultVibrationEnabled);
            vibrationCached = true;
        }

        return cachedVibrationEnabled;
    }

    public static void SetVibrationEnabled(bool enabled)
    {
        cachedVibrationEnabled = enabled;
        vibrationCached = true;
        ES3.Save(VibrationEnabledKey, enabled);
    }

    public static GraphicsQualityMode GetGraphicsQualityMode()
    {
        int savedValue = ES3.Load(GraphicsQualityModeKey, (int)DefaultGraphicsQualityMode);
        return Enum.IsDefined(typeof(GraphicsQualityMode), savedValue)
            ? (GraphicsQualityMode)savedValue
            : DefaultGraphicsQualityMode;
    }

    public static void SetGraphicsQualityMode(GraphicsQualityMode mode)
    {
        ES3.Save(GraphicsQualityModeKey, (int)mode);
        ApplyGraphicsQualityMode(mode);
    }

    public static string GetLanguageCode()
    {
        string savedCode = ES3.Load<string>(LanguageCodeKey, defaultValue: DefaultLanguageCode);
        return string.Equals(savedCode, DefaultLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? DefaultLanguageCode
            : DefaultLanguageCode;
    }

    public static void SetLanguageCode(string languageCode)
    {
        // English is the only supported language until a localization backend is connected.
        ES3.Save(LanguageCodeKey, DefaultLanguageCode);
    }

    public static void ApplyGraphicsQualityMode(GraphicsQualityMode mode)
    {
        string targetLevelName = null;

        switch (mode)
        {
            case GraphicsQualityMode.Low:
                targetLevelName = "Fast";
                break;
            case GraphicsQualityMode.High:
                targetLevelName = "Fantastic";
                break;
            case GraphicsQualityMode.Auto:
#if UNITY_ANDROID && !UNITY_EDITOR
                targetLevelName = "Good";
#else
                // In Editor and unsupported platforms, Auto retains the project's current quality.
                return;
#endif
        }

        string[] qualityNames = QualitySettings.names;
        int targetIndex = Array.FindIndex(
            qualityNames,
            name => string.Equals(name, targetLevelName, StringComparison.OrdinalIgnoreCase));

        if (targetIndex >= 0)
        {
            QualitySettings.SetQualityLevel(targetIndex, true);
        }
        else
        {
            Debug.LogWarning($"[Settings] Quality level '{targetLevelName}' is not configured; the current level was retained.");
        }

        // TODO: Replace or augment this simple quality mapping with the
        // future device benchmark and mobile VFX quality-tier system.
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedGraphicsQualityAtStartup()
    {
        ApplyGraphicsQualityMode(GetGraphicsQualityMode());
    }
}
