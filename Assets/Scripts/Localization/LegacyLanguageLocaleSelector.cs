using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// One-time compatibility selector for builds that previously stored
/// Settings.LanguageCode in Easy Save. It only runs when the Unity Localization
/// PlayerPrefs selector has no value and never maintains a second language save.
/// </summary>
[Serializable]
public sealed class LegacyLanguageLocaleSelector : IStartupLocaleSelector
{
    private const string MigrationCompleteKey = "SkateRunner.Localization.LegacyMigrationComplete";

    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        if (PlayerPrefs.HasKey(SkateLocalization.SelectedLocalePlayerPrefsKey)
            || PlayerPrefs.GetInt(MigrationCompleteKey, 0) != 0)
        {
            return null;
        }

        PlayerPrefs.SetInt(MigrationCompleteKey, 1);
        float saveLoadStartedAt = Time.realtimeSinceStartup;
        string legacyCode = ES3.Load<string>(GameSettingsSave.LanguageCodeKey, defaultValue: SkateLocalization.DefaultLocaleCode);
        Debug.Log($"[StartupTiming] legacy-language-save-load durationMs={(Time.realtimeSinceStartup - saveLoadStartedAt) * 1000f:0.0}");
        string normalizedCode = SkateLocalization.NormalizeLocaleCode(legacyCode);
        Locale locale = SkateLocalization.IsProductionLocaleCode(normalizedCode)
            ? availableLocales.GetLocale(new LocaleIdentifier(normalizedCode))
            : null;
        if (locale != null)
        {
            PlayerPrefs.SetString(SkateLocalization.SelectedLocalePlayerPrefsKey, locale.Identifier.Code);
        }

        PlayerPrefs.Save();
        return locale;
    }
}
