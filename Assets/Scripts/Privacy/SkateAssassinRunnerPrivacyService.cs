using System;
using System.Collections;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using MobileMonetizationPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One launch-scoped MMP/UMP privacy flow for Skate Assassin Runner; does not initialize or request ads.</summary>
[DefaultExecutionOrder(-8000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(MobileMonetizationPro_Consent_Controller))]
public sealed class SkateAssassinRunnerPrivacyService : MonoBehaviour
{
    public const string ResourcePath = "SkateAssassinRunnerPrivacyService";
    public static SkateAssassinRunnerPrivacyService Instance { get; private set; }
    public static event Action ConsentStateChanged;

    [SerializeField] private SkateAssassinRunnerPrivacyConfiguration configuration;
    private MobileMonetizationPro_Consent_Controller consentController;
    private InternetConnectivityCheck connectivityChecker;
    private bool launchUpdateStarted;
    private bool sdkCanRequestAds;
    private bool privacyOptionsRequired;

    public bool InitializationFinished { get; private set; }
    public bool IsBusy { get; private set; }
    public string LastError { get; private set; }
    // Only publish Google's answer after the launch update/form callback, never a saved custom consent boolean.
    public bool CanRequestAds => InitializationFinished && !IsBusy && sdkCanRequestAds;
    public bool PrivacyOptionsRequired => privacyOptionsRequired;

#if UNITY_EDITOR
    // Verification seam; omitted from Android players. Normal execution always uses the MMP controller.
    public Action<ConsentRequestParameters, Action<string>> GatherConsentForVerification;
    public Action<Action<string>> ShowPrivacyOptionsForVerification;
    public Func<bool> CanRequestAdsForVerification;
    public Func<PrivacyOptionsRequirementStatus> PrivacyOptionsStatusForVerification;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        ConsentStateChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= EnsureForScene;
        SceneManager.sceneLoaded += EnsureForScene;
        EnsureForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void EnsureForScene(Scene scene, LoadSceneMode mode)
    {
        if (Instance != null || (scene.name != "ElroiBootSplash" && scene.name != "SkateRunnerLoadingScreen"
            && scene.name != "SkateRunnerStartScreen" && scene.name != "SkateRunner")) return;
        var prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab != null) Instantiate(prefab);
        else Debug.LogError("[Skate Assassin Runner Privacy] Missing Resources/" + ResourcePath + " prefab.");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        consentController = GetComponent<MobileMonetizationPro_Consent_Controller>();
        // MMP's automatic Start/listeners/reflection ad callback are replaced by this single orchestration owner.
        consentController.DisplayConsent = false;
        consentController.ResetConsentButton = null;
        consentController.UpdateConsentButton = null;
        consentController.ErrorPopup = null;
        consentController.ErrorText = null;
        consentController.targetScript = null;
        consentController.selectedMethodName = string.Empty;
        MobileAdsEventExecutor.Initialize(); // Unity-thread callback dispatcher only; not MobileAds.Initialize.
    }

    private IEnumerator Start()
    {
        if (Instance != this) yield break;
        // The connectivity bootstrap may be created after this bootstrap during the same scene callback.
        yield return null;
        var gate = SkateRunnerConnectivityGate.Instance;
        if (gate == null)
        {
            Finish("Startup: approved connectivity gate is missing; consent was not requested.", true);
            yield break;
        }
        connectivityChecker = gate.GetComponent<InternetConnectivityCheck>();
        connectivityChecker.ConnectionStatusChanged += OnConnectionChanged;
        OnConnectionChanged(connectivityChecker.HasCheckedConnection && connectivityChecker.IsConnected);
    }

    private void OnDestroy()
    {
        if (connectivityChecker != null) connectivityChecker.ConnectionStatusChanged -= OnConnectionChanged;
        if (Instance == this) Instance = null;
    }

    private void OnConnectionChanged(bool connected)
    {
        if (!connected || launchUpdateStarted || Instance != this) return;
        launchUpdateStarted = true;
        IsBusy = true;
        LastError = null;
        NotifyStateChanged();
        try
        {
            if (configuration == null) throw new InvalidOperationException("Missing privacy configuration.");
            configuration.ApplyDevelopmentResetIfRequested();
            var request = configuration.CreateConsentRequest();
            Action<string> completed = error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (this != null && Instance == this) Finish(error, true);
            });
#if UNITY_EDITOR
            if (GatherConsentForVerification != null) GatherConsentForVerification(request, completed);
            else
#endif
                consentController.GatherConsent(request, completed);
        }
        catch (Exception exception) { Finish("Launch consent: " + exception.Message, true); }
    }

    public bool ShowPrivacyOptions()
    {
        if (IsBusy || !PrivacyOptionsRequired || SkateRunnerConnectivityGate.IsBlocked) return false;
        IsBusy = true;
        LastError = null;
        NotifyStateChanged();
        try
        {
            Action<string> completed = error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (this != null && Instance == this) Finish(error, false);
            });
#if UNITY_EDITOR
            if (ShowPrivacyOptionsForVerification != null) ShowPrivacyOptionsForVerification(completed);
            else
#endif
                consentController.ShowConsentOptionsForm(completed);
        }
        catch (Exception exception) { Finish("Privacy options: " + exception.Message, false); }
        return true;
    }

    private void Finish(string error, bool launch)
    {
        IsBusy = false;
        if (launch) InitializationFinished = true;
        LastError = error;
        try
        {
#if UNITY_EDITOR
            sdkCanRequestAds = CanRequestAdsForVerification != null
                ? CanRequestAdsForVerification() : consentController.CanRequestAds;
            var status = PrivacyOptionsStatusForVerification != null
                ? PrivacyOptionsStatusForVerification() : ConsentInformation.PrivacyOptionsRequirementStatus;
#else
            sdkCanRequestAds = consentController.CanRequestAds;
            var status = ConsentInformation.PrivacyOptionsRequirementStatus;
#endif
            privacyOptionsRequired = status == PrivacyOptionsRequirementStatus.Required;
        }
        catch (Exception exception)
        {
            sdkCanRequestAds = false;
            privacyOptionsRequired = false;
            LastError = (error ?? string.Empty) + " State refresh: " + exception.Message;
        }
        if (!string.IsNullOrEmpty(LastError))
            Debug.LogWarning("[Skate Assassin Runner Privacy] " + LastError + " Game remains available; ad eligibility comes from UMP.", this);
        else
            Debug.Log("[Skate Assassin Runner Privacy] Consent ready. CanRequestAds=" + CanRequestAds
                + "; PrivacyOptionsRequired=" + PrivacyOptionsRequired + ". No ads initialized or requested.", this);
        NotifyStateChanged();
    }

    private static void NotifyStateChanged() { ConsentStateChanged?.Invoke(); }
}
