using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>Privacy/legal rows in Skate Assassin Runner. Navigation and audio remain with the existing popup.</summary>
[DisallowMultipleComponent]
public sealed class SkateAssassinRunnerLegalPanelController : MonoBehaviour
{
    [SerializeField] private SkateAssassinRunnerPrivacyConfiguration configuration;
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private Button privacyOptionsButton;
    [SerializeField] private Button termsButton;
    [SerializeField] private Button dataRequestButton;
    [SerializeField] private Button restorePurchasesButton;
    [SerializeField] private Button supportButton;
    [SerializeField] private TMP_Text statusText;
    private string statusKey;

#if UNITY_EDITOR
    // Avoid launching a browser during editor verification; omitted from players.
    public Action<string> OpenUrlForVerification;
#endif

    private void OnEnable()
    {
        Bind(privacyPolicyButton, OpenPrivacy);
        Bind(privacyOptionsButton, OpenPrivacyOptions);
        Bind(termsButton, OpenTerms);
        Bind(dataRequestButton, OpenDataRequest);
        Bind(restorePurchasesButton, RestorePurchases);
        Bind(supportButton, OpenSupport);
        SkateAssassinRunnerPrivacyService.ConsentStateChanged += RefreshAvailability;
        SkateAssassinRunnerPurchaseRestoration.AvailabilityChanged += RefreshAvailability;
        SkateLocalization.LocaleChanged += OnLocaleChanged;
        statusKey = null;
        RefreshAvailability();
        RefreshStatus();
    }

    private void OnDisable()
    {
        Unbind(privacyPolicyButton, OpenPrivacy);
        Unbind(privacyOptionsButton, OpenPrivacyOptions);
        Unbind(termsButton, OpenTerms);
        Unbind(dataRequestButton, OpenDataRequest);
        Unbind(restorePurchasesButton, RestorePurchases);
        Unbind(supportButton, OpenSupport);
        SkateAssassinRunnerPrivacyService.ConsentStateChanged -= RefreshAvailability;
        SkateAssassinRunnerPurchaseRestoration.AvailabilityChanged -= RefreshAvailability;
        SkateLocalization.LocaleChanged -= OnLocaleChanged;
    }

    private void OpenPrivacy() { OpenWebsite(SkateAssassinRunnerPrivacyConfiguration.LegalPage.Privacy); }
    private void OpenTerms() { OpenWebsite(SkateAssassinRunnerPrivacyConfiguration.LegalPage.Terms); }
    private void OpenDataRequest() { OpenWebsite(SkateAssassinRunnerPrivacyConfiguration.LegalPage.DataRequest); }
    private void OpenSupport() { OpenWebsite(SkateAssassinRunnerPrivacyConfiguration.LegalPage.Support); }

    private void OpenWebsite(SkateAssassinRunnerPrivacyConfiguration.LegalPage page)
    {
        if (SkateRunnerConnectivityGate.IsBlocked) return;
        if (configuration == null || !configuration.IsValidUrl(page))
        {
            Debug.LogWarning("[Skate Assassin Runner Legal] Invalid or missing " + page + " WWW HTTPS URL.", this);
            SetStatus("legal.link_unavailable");
            return;
        }
        try
        {
            var url = configuration.GetUrl(page);
#if UNITY_EDITOR
            if (OpenUrlForVerification != null) { OpenUrlForVerification(url); return; }
#endif
            Application.OpenURL(url);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Skate Assassin Runner Legal] Browser could not open " + page + ": " + exception.Message, this);
            SetStatus("legal.link_unavailable");
        }
    }

    private void OpenPrivacyOptions()
    {
        var service = SkateAssassinRunnerPrivacyService.Instance;
        if (service == null || !service.ShowPrivacyOptions()) RefreshAvailability();
    }

    private void RestorePurchases()
    {
        if (SkateRunnerConnectivityGate.IsBlocked) return;
        SetStatus("legal.restore_in_progress");
        if (!SkateAssassinRunnerPurchaseRestoration.TryRestore(success =>
        {
            // This reports completion only after a real registered store backend returns.
            SetStatus(success ? "legal.restore_completed" : "legal.restore_failed");
        })) SetStatus("legal.restore_unavailable");
    }

    private void RefreshAvailability()
    {
        var service = SkateAssassinRunnerPrivacyService.Instance;
        bool required = service != null && service.PrivacyOptionsRequired;
        if (privacyOptionsButton != null)
        {
            privacyOptionsButton.interactable = required && !service.IsBusy;
            privacyOptionsButton.gameObject.SetActive(required);
        }
        if (restorePurchasesButton != null)
            restorePurchasesButton.interactable = SkateAssassinRunnerPurchaseRestoration.IsAvailable;
        if (service != null && !string.IsNullOrEmpty(service.LastError))
            SetStatus("legal.privacy_temporarily_unavailable");
        else if (statusKey == "legal.privacy_temporarily_unavailable") SetStatus(null);
    }

    private void SetStatus(string key) { statusKey = key; RefreshStatus(); }
    private void OnLocaleChanged(Locale locale) { RefreshStatus(); }
    private void RefreshStatus()
    {
        if (statusText != null)
            statusText.text = string.IsNullOrEmpty(statusKey) ? string.Empty : SkateLocalization.Get("Legal", statusKey);
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
    private static void Unbind(Button button, UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }
}
