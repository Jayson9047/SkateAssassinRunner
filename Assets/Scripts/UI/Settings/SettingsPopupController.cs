using System;
using System.Collections;
using MoreMountains.InfiniteRunnerEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the Settings popup's values, immediate application, and page navigation.
/// The existing SettingsPopup component continues to own the MMPopup lifecycle.
/// </summary>
public class SettingsPopupController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject mainSettingsPage;
    [SerializeField] private GameObject privacyLegalPage;
    [SerializeField] private GameObject languagePage;
    [SerializeField] private ScrollRect mainScrollRect;
    [SerializeField] private ScrollRect privacyLegalScrollRect;

    [Header("Audio and Haptics")]
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundEffectsToggle;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Image musicSwitchTrack;
    [SerializeField] private Image soundEffectsSwitchTrack;
    [SerializeField] private Image vibrationSwitchTrack;
    [SerializeField] private RectTransform musicSwitchKnob;
    [SerializeField] private RectTransform soundEffectsSwitchKnob;
    [SerializeField] private RectTransform vibrationSwitchKnob;
    [SerializeField] private Sprite switchOnSprite;
    [SerializeField] private Sprite switchOffSprite;
    [SerializeField] private float switchKnobOffset = 25f;

    [Header("Graphics Quality")]
    [SerializeField] private Button autoQualityButton;
    [SerializeField] private Button lowQualityButton;
    [SerializeField] private Button highQualityButton;
    [SerializeField] private Image autoQualityVisual;
    [SerializeField] private Image lowQualityVisual;
    [SerializeField] private Image highQualityVisual;
    [SerializeField] private Sprite qualitySelectedSprite;
    [SerializeField] private Sprite qualityUnselectedSprite;

    [Header("Navigation")]
    [SerializeField] private Button languageButton;
    [SerializeField] private Button privacyLegalButton;
    [SerializeField] private Button privacyBackButton;
    [SerializeField] private Button languageBackButton;
    [SerializeField] private Button englishButton;
    [SerializeField] private TMP_Text currentLanguageLabel;
    [SerializeField] private GameObject englishSelectedVisual;

    [Header("Privacy and Legal Placeholders")]
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private Button termsOfUseButton;
    [SerializeField] private Button eulaButton;
    [SerializeField] private Button dataDeletionButton;
    [SerializeField] private Button restorePurchasesButton;
    [SerializeField] private Button supportButton;
    [SerializeField] private TMP_Text legalStatusText;

    [Header("Social Links")]
    [SerializeField] private Button youtubeButton;
    [SerializeField] private Button instagramButton;
    [SerializeField] private Button tiktokButton;
    [SerializeField] private string youtubeUrl = string.Empty;
    [SerializeField] private string instagramUrl = string.Empty;
    [SerializeField] private string tiktokUrl = string.Empty;

    [Header("Footer")]
    [SerializeField] private TMP_Text versionText;

    private bool listenersBound;
    private bool missingSoundManagerWarningShown;
    private Coroutine scrollResetCoroutine;

    private void Awake()
    {
        if (soundManager == null)
        {
            soundManager = SoundManager.Instance;
        }

        BindListeners();
        ShowMainPage();
        RefreshAllValues();
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    public void HandlePopupOpened()
    {
        ShowMainPage();
        RefreshAllValues();
    }

    public void HandlePopupClosed()
    {
        ShowMainPage();
        SetLegalStatus(string.Empty);
    }

    private void BindListeners()
    {
        if (listenersBound)
        {
            return;
        }

        AddToggleListener(musicToggle, OnMusicChanged);
        AddToggleListener(soundEffectsToggle, OnSoundEffectsChanged);
        AddToggleListener(vibrationToggle, OnVibrationChanged);

        AddButtonListener(autoQualityButton, SelectAutoQuality);
        AddButtonListener(lowQualityButton, SelectLowQuality);
        AddButtonListener(highQualityButton, SelectHighQuality);
        AddButtonListener(languageButton, ShowLanguagePage);
        AddButtonListener(privacyLegalButton, ShowPrivacyLegalPage);
        AddButtonListener(privacyBackButton, ShowMainPage);
        AddButtonListener(languageBackButton, ShowMainPage);
        AddButtonListener(englishButton, SelectEnglish);

        AddButtonListener(privacyPolicyButton, OnPrivacyPolicyPressed);
        AddButtonListener(termsOfUseButton, OnTermsOfUsePressed);
        AddButtonListener(eulaButton, OnEulaPressed);
        AddButtonListener(dataDeletionButton, OnDataDeletionPressed);
        AddButtonListener(restorePurchasesButton, OnRestorePurchasesPressed);
        AddButtonListener(supportButton, OnSupportPressed);

        AddButtonListener(youtubeButton, OpenYouTube);
        AddButtonListener(instagramButton, OpenInstagram);
        AddButtonListener(tiktokButton, OpenTikTok);
        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!listenersBound)
        {
            return;
        }

        RemoveToggleListener(musicToggle, OnMusicChanged);
        RemoveToggleListener(soundEffectsToggle, OnSoundEffectsChanged);
        RemoveToggleListener(vibrationToggle, OnVibrationChanged);

        RemoveButtonListener(autoQualityButton, SelectAutoQuality);
        RemoveButtonListener(lowQualityButton, SelectLowQuality);
        RemoveButtonListener(highQualityButton, SelectHighQuality);
        RemoveButtonListener(languageButton, ShowLanguagePage);
        RemoveButtonListener(privacyLegalButton, ShowPrivacyLegalPage);
        RemoveButtonListener(privacyBackButton, ShowMainPage);
        RemoveButtonListener(languageBackButton, ShowMainPage);
        RemoveButtonListener(englishButton, SelectEnglish);

        RemoveButtonListener(privacyPolicyButton, OnPrivacyPolicyPressed);
        RemoveButtonListener(termsOfUseButton, OnTermsOfUsePressed);
        RemoveButtonListener(eulaButton, OnEulaPressed);
        RemoveButtonListener(dataDeletionButton, OnDataDeletionPressed);
        RemoveButtonListener(restorePurchasesButton, OnRestorePurchasesPressed);
        RemoveButtonListener(supportButton, OnSupportPressed);

        RemoveButtonListener(youtubeButton, OpenYouTube);
        RemoveButtonListener(instagramButton, OpenInstagram);
        RemoveButtonListener(tiktokButton, OpenTikTok);
        listenersBound = false;
    }

    private void RefreshAllValues()
    {
        RefreshAudioValues();

        bool vibrationEnabled = GameSettingsSave.IsVibrationEnabled();
        SetToggleWithoutNotify(vibrationToggle, vibrationEnabled);
        RefreshSwitchVisual(vibrationToggle, vibrationSwitchTrack, vibrationSwitchKnob);

        RefreshQualityVisual(GameSettingsSave.GetGraphicsQualityMode());
        RefreshLanguageVisual(GameSettingsSave.GetLanguageCode());

        if (versionText != null)
        {
            versionText.text = $"Version {Application.version}";
        }
    }

    private void RefreshAudioValues()
    {
        if (soundManager == null || soundManager.Settings == null)
        {
            if (!missingSoundManagerWarningShown)
            {
                Debug.LogWarning("[Settings] SoundManager is unavailable; audio controls could not be refreshed.", this);
                missingSoundManagerWarningShown = true;
            }

            return;
        }

        SetToggleWithoutNotify(musicToggle, soundManager.Settings.MusicOn);
        SetToggleWithoutNotify(soundEffectsToggle, soundManager.Settings.SfxOn);
        RefreshSwitchVisual(musicToggle, musicSwitchTrack, musicSwitchKnob);
        RefreshSwitchVisual(soundEffectsToggle, soundEffectsSwitchTrack, soundEffectsSwitchKnob);
    }

    private void OnMusicChanged(bool enabled)
    {
        if (soundManager != null)
        {
            if (enabled)
            {
                soundManager.MusicOn();
            }
            else
            {
                soundManager.MusicOff();
            }
        }

        RefreshSwitchVisual(musicToggle, musicSwitchTrack, musicSwitchKnob);
    }

    private void OnSoundEffectsChanged(bool enabled)
    {
        if (soundManager != null)
        {
            if (enabled)
            {
                soundManager.SfxOn();
            }
            else
            {
                soundManager.SfxOff();
            }
        }

        RefreshSwitchVisual(soundEffectsToggle, soundEffectsSwitchTrack, soundEffectsSwitchKnob);
    }

    private void OnVibrationChanged(bool enabled)
    {
        GameSettingsSave.SetVibrationEnabled(enabled);
        RefreshSwitchVisual(vibrationToggle, vibrationSwitchTrack, vibrationSwitchKnob);
    }

    private void SelectAutoQuality()
    {
        SelectQuality(GraphicsQualityMode.Auto);
    }

    private void SelectLowQuality()
    {
        SelectQuality(GraphicsQualityMode.Low);
    }

    private void SelectHighQuality()
    {
        SelectQuality(GraphicsQualityMode.High);
    }

    private void SelectQuality(GraphicsQualityMode mode)
    {
        GameSettingsSave.SetGraphicsQualityMode(mode);
        RefreshQualityVisual(mode);
    }

    private void RefreshQualityVisual(GraphicsQualityMode mode)
    {
        SetQualityVisual(autoQualityVisual, mode == GraphicsQualityMode.Auto);
        SetQualityVisual(lowQualityVisual, mode == GraphicsQualityMode.Low);
        SetQualityVisual(highQualityVisual, mode == GraphicsQualityMode.High);
    }

    private void SetQualityVisual(Image visual, bool selected)
    {
        if (visual != null)
        {
            visual.sprite = selected ? qualitySelectedSprite : qualityUnselectedSprite;
            visual.color = selected ? Color.white : new Color(0.82f, 0.88f, 0.95f, 1f);
        }
    }

    public void ShowMainPage()
    {
        SetPageActive(mainSettingsPage, true);
        SetPageActive(privacyLegalPage, false);
        SetPageActive(languagePage, false);
        ResetMainScrollPosition();
    }

    private void ResetMainScrollPosition()
    {
        if (mainScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            mainScrollRect.StopMovement();
            mainScrollRect.verticalNormalizedPosition = 1f;

            if (isActiveAndEnabled)
            {
                if (scrollResetCoroutine != null)
                {
                    StopCoroutine(scrollResetCoroutine);
                }

                scrollResetCoroutine = StartCoroutine(ResetMainScrollPositionNextFrame());
            }
        }
    }

    private IEnumerator ResetMainScrollPositionNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        mainScrollRect.StopMovement();
        mainScrollRect.verticalNormalizedPosition = 1f;
        scrollResetCoroutine = null;
    }

    private static void ResetScrollPosition(ScrollRect scrollRect)
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ShowPrivacyLegalPage()
    {
        SetPageActive(mainSettingsPage, false);
        SetPageActive(privacyLegalPage, true);
        SetPageActive(languagePage, false);
        ResetScrollPosition(privacyLegalScrollRect);
        SetLegalStatus(string.Empty);
    }

    private void ShowLanguagePage()
    {
        SetPageActive(mainSettingsPage, false);
        SetPageActive(privacyLegalPage, false);
        SetPageActive(languagePage, true);
        RefreshLanguageVisual(GameSettingsSave.GetLanguageCode());
    }

    private void SelectEnglish()
    {
        GameSettingsSave.SetLanguageCode("en");
        RefreshLanguageVisual("en");
    }

    private void RefreshLanguageVisual(string languageCode)
    {
        bool englishSelected = string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase);

        if (currentLanguageLabel != null)
        {
            currentLanguageLabel.text = "English";
        }

        if (englishSelectedVisual != null)
        {
            englishSelectedVisual.SetActive(englishSelected);
        }
    }

    private void OnPrivacyPolicyPressed()
    {
        // TODO: Connect the final hosted Privacy Policy URL
        // or Mobile Monetization Pro V2 legal-page integration here.
        SetLegalStatus("Privacy Policy is coming soon.");
    }

    private void OnTermsOfUsePressed()
    {
        // TODO: Connect the final hosted Terms of Use URL
        // or Mobile Monetization Pro V2 integration here.
        SetLegalStatus("Terms of Use are coming soon.");
    }

    private void OnEulaPressed()
    {
        // TODO: Connect the final End User Licence Agreement URL
        // or Mobile Monetization Pro V2 integration here.
        SetLegalStatus("End User Licence Agreement is coming soon.");
    }

    private void OnDataDeletionPressed()
    {
        // TODO: Connect the final data/deletion request page
        // after the production analytics, advertising, account,
        // and monetization data flows are finalized.
        SetLegalStatus("Data and deletion requests are coming soon.");
    }

    private void OnRestorePurchasesPressed()
    {
        // TODO: Connect Mobile Monetization Pro V2 / Google Play
        // restore-purchase entitlement recovery here.
        SetLegalStatus("Restore Purchases is not connected yet.");
    }

    private void OnSupportPressed()
    {
        // TODO: Connect the final support email or support webpage here.
        SetLegalStatus("Support contact is coming soon.");
    }

    private void OpenYouTube()
    {
        OpenConfiguredUrl(youtubeUrl, "YouTube");
    }

    private void OpenInstagram()
    {
        OpenConfiguredUrl(instagramUrl, "Instagram");
    }

    private void OpenTikTok()
    {
        OpenConfiguredUrl(tiktokUrl, "TikTok");
    }

    private void OpenConfiguredUrl(string configuredUrl, string serviceName)
    {
        Uri uri;
        bool valid = !string.IsNullOrWhiteSpace(configuredUrl)
            && Uri.TryCreate(configuredUrl, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        if (!valid)
        {
            Debug.LogWarning($"[Settings] {serviceName} URL is not configured.", this);
            return;
        }

        Application.OpenURL(configuredUrl);
    }

    private void SetLegalStatus(string message)
    {
        if (legalStatusText != null)
        {
            legalStatusText.text = message;
        }
    }

    private void RefreshSwitchVisual(Toggle toggle, Image track, RectTransform knob)
    {
        if (toggle == null)
        {
            return;
        }

        if (track != null)
        {
            track.sprite = toggle.isOn ? switchOnSprite : switchOffSprite;
            track.color = Color.white;
        }

        if (knob != null)
        {
            Vector2 position = knob.anchoredPosition;
            position.x = toggle.isOn ? switchKnobOffset : -switchKnobOffset;
            knob.anchoredPosition = position;
        }
    }

    private static void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private static void SetPageActive(GameObject page, bool active)
    {
        if (page != null && page.activeSelf != active)
        {
            page.SetActive(active);
        }
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private static void AddToggleListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(action);
        }
    }

    private static void RemoveToggleListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(action);
        }
    }
}
