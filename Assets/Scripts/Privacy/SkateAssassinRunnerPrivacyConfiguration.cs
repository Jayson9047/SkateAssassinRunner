using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>Auditable legal links and development-only privacy QA for Skate Assassin Runner.</summary>
[CreateAssetMenu(menuName = "Skate Assassin Runner/Privacy and Legal Configuration")]
public sealed class SkateAssassinRunnerPrivacyConfiguration : ScriptableObject
{
    public enum LegalPage { Privacy, Terms, DataRequest, Support }
    [SerializeField] private string privacyUrl = "https://www.elroicreativestudios.com/games/skate-assassin-runner/privacy/";
    [SerializeField] private string termsUrl = "https://www.elroicreativestudios.com/games/skate-assassin-runner/terms/";
    [SerializeField] private string dataRequestUrl = "https://www.elroicreativestudios.com/games/skate-assassin-runner/data-request/";
    [SerializeField] private string supportUrl = "https://www.elroicreativestudios.com/games/skate-assassin-runner/support/";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("UMP QA — Editor / Development Builds only")]
    [Tooltip("Disabled by default. Requires an official UMP test-device hash on Android.")]
    [SerializeField] private bool forceEEAForTesting;
    [SerializeField] private List<string> testDeviceHashedIds = new List<string>();
    [Tooltip("Testing only: reset consent on this device once before the next launch update.")]
    [SerializeField] private bool resetTestConsentOnNextLaunch;
    [Tooltip("Increment for another one-time consent reset on the same test device; never resets game progress.")]
    [SerializeField, Min(0)] private int testConsentResetRevision;
    private const string ResetConsumedKey = "SkateAssassinRunner.Privacy.QAResetConsumed";
#endif

    public string GetUrl(LegalPage page)
    {
        switch (page)
        {
            case LegalPage.Terms: return termsUrl;
            case LegalPage.DataRequest: return dataRequestUrl;
            case LegalPage.Support: return supportUrl;
            default: return privacyUrl;
        }
    }

    public bool IsValidUrl(LegalPage page)
    {
        Uri uri;
        return Uri.TryCreate(GetUrl(page), UriKind.Absolute, out uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host == "www.elroicreativestudios.com";
    }

    public ConsentRequestParameters CreateConsentRequest()
    {
        var request = new ConsentRequestParameters();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (forceEEAForTesting)
        {
            var hashes = testDeviceHashedIds.FindAll(id => !string.IsNullOrWhiteSpace(id));
            if (Application.platform == RuntimePlatform.Android && hashes.Count == 0)
                Debug.LogWarning("[Skate Assassin Runner Privacy] EEA QA ignored: enter the UMP test-device hash first.");
            else
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = hashes
                };
        }
#endif
        return request;
    }

    public void ApplyDevelopmentResetIfRequested()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!resetTestConsentOnNextLaunch)
        {
            PlayerPrefs.DeleteKey(ResetConsumedKey);
            return;
        }
        if (PlayerPrefs.GetInt(ResetConsumedKey, -1) == testConsentResetRevision) return;
        ConsentInformation.Reset();
        PlayerPrefs.SetInt(ResetConsumedKey, testConsentResetRevision);
        PlayerPrefs.Save();
        Debug.Log("[Skate Assassin Runner Privacy] One-time development test consent reset.");
#endif
    }
}
