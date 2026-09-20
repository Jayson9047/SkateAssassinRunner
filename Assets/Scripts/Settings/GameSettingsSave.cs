/// <summary>
/// Persistent player-facing non-audio settings. Performance quality is selected
/// internally by SkateRunnerPerformanceManager and is not exposed in the UI.
/// </summary>
public static class GameSettingsSave
{
    public const string VibrationEnabledKey = "Settings.VibrationEnabled";
    public const string LanguageCodeKey = "Settings.LanguageCode";

    private const bool DefaultVibrationEnabled = true;
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

    public static string GetLanguageCode()
    {
        return SkateLocalization.CurrentLocaleCode;
    }

    public static void SetLanguageCode(string languageCode)
    {
        SkateLocalization.SelectLocale(languageCode);
    }
}

