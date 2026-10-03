using System.Collections;
using MobileMonetizationPro;
using MoreMountains.InfiniteRunnerEngine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Persistent MMP connectivity gate for Skate Runner's menus and gameplay.</summary>
[DefaultExecutionOrder(-9000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(InternetConnectivityCheck))]
public sealed class SkateRunnerConnectivityGate : MonoBehaviour
{
    private const string ResourcePath = "SkateRunnerConnectivityGate";
    private static SkateRunnerConnectivityGate instance;
    public static bool IsBlocked => instance != null && !instance.verifiedConnection;
    public static SkateRunnerConnectivityGate Instance => instance;

    [Header("Offline popup")]
    [SerializeField] private GameObject blockingPopup;
    [SerializeField] private Button retryButton;
    [SerializeField] private TMP_Text messageTitle;
    [SerializeField] private TMP_Text messageBody;
    [SerializeField] private TMP_Text retryLabel;

    [Header("Background checks (unscaled seconds)")]
    [SerializeField, Min(1f)] private float onlineCheckInterval = 5f;
    [SerializeField, Min(1f)] private float offlineCheckInterval = 3f;
    [SerializeField, Min(1)] private int requestTimeoutSeconds = 3;
    // Public connectivity probes used by Android's NetworkMonitor. No player identifiers are sent.
    [SerializeField] private string[] probeUrls =
    {
        "https://www.google.com/generate_204",
        "https://connectivitycheck.gstatic.com/generate_204"
    };

    private InternetConnectivityCheck checker;
    private bool verifiedConnection;
    private bool checking;
    private bool labelsReady;
    private bool applicationSuspended;
    private float nextCheckAt;
    private NetworkReachability lastReachability;
    private Coroutine probeCoroutine;
    private UnityWebRequest activeRequest;
    private bool ownsPause;
    private float resumeTimeScale;
    private float resumeFixedDeltaTime;
    private InputManager inputManager;
    private bool inputManagerWasEnabled;
    private bool ownsInputBlock;
    private EventSystem eventSystem;
    private bool navigationWasEnabled;
    private bool ownsNavigationBlock;
    private int boundSceneHandle = -1;

    public bool IsChecking => checking;
    public int CompletedChecks { get; private set; }

#if UNITY_EDITOR
    private bool? simulatedConnection;
    // Editor-only verification seam. Does not alter the computer's networking.
    public void SetSimulatedConnection(bool? connected) { simulatedConnection = connected; }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= EnsureForScene;
        SceneManager.sceneLoaded += EnsureForScene;
        EnsureForScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void EnsureForScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "ElroiBootSplash" && scene.name != "SkateRunnerLoadingScreen" &&
            scene.name != "SkateRunnerStartScreen" && scene.name != "SkateRunner")
            return;

        if (instance == null)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[Connectivity] Missing Resources/" + ResourcePath + " prefab.");
                return;
            }
            Instantiate(prefab);
        }
        instance.BindScene(scene);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        checker = GetComponent<InternetConnectivityCheck>();
        checker.ManageTimeScale = false;
        checker.ConnectionEvaluator = EvaluateConnection;
        checker.NoInternetConnectionGameObject = blockingPopup;
        checker.ConnectionStatusChanged += HandleConnectionChanged;
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        lastReachability = Application.internetReachability;
        checker.CheckNow(); // Fail closed until the first real probe succeeds.
        BindScene(SceneManager.GetActiveScene());
    }

    private void OnEnable() { SkateLocalization.LocaleChanged += HandleLocaleChanged; }
    private void OnDisable() { SkateLocalization.LocaleChanged -= HandleLocaleChanged; }

    private bool EvaluateConnection()
    {
#if UNITY_EDITOR
        if (simulatedConnection.HasValue) return verifiedConnection;
#endif
        return verifiedConnection && Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void Update()
    {
        if (instance != this || applicationSuspended) return;
        if (!labelsReady && LocalizationSettings.InitializationOperation.IsDone) RefreshLabels();

        var reachability = Application.internetReachability;
        if (reachability != lastReachability)
        {
            lastReachability = reachability;
            verifiedConnection = false;
            checker.CheckNow();
            nextCheckAt = 0f;
        }

        if (!checking && Time.unscaledTime >= nextCheckAt) BeginCheck();
        if (IsBlocked) MaintainBlock();
    }

    private void LateUpdate()
    {
        // Late-running hitstop/scene scripts must not release the offline pause.
        if (instance == this && IsBlocked) MaintainBlock();
    }

    public void Retry()
    {
        if (instance != this || checking || applicationSuspended) return;
        nextCheckAt = 0f;
        BeginCheck();
    }

    private void BeginCheck()
    {
        checking = true;
        RefreshLabels();
        probeCoroutine = StartCoroutine(ProbeConnection());
    }

    private IEnumerator ProbeConnection()
    {
        // Keep Retry's checking state visible and avoid completing before the coroutine is assigned.
        yield return null;
        bool connected = false;
#if UNITY_EDITOR
        if (simulatedConnection.HasValue)
        {
            FinishCheck(simulatedConnection.Value);
            yield break;
        }
#endif
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            foreach (string url in probeUrls)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                using (var request = UnityWebRequest.Get(url))
                {
                    activeRequest = request;
                    request.timeout = requestTimeoutSeconds;
                    request.redirectLimit = 0;
                    request.SetRequestHeader("Cache-Control", "no-cache");
                    yield return request.SendWebRequest();
                    connected = request.result == UnityWebRequest.Result.Success && request.responseCode == 204;
                    activeRequest = null;
                }
                if (connected || Application.internetReachability == NetworkReachability.NotReachable) break;
            }
        }
        FinishCheck(connected);
    }

    private void FinishCheck(bool connected)
    {
        checking = false;
        probeCoroutine = null;
        CompletedChecks++;
        verifiedConnection = connected;
        nextCheckAt = Time.unscaledTime + (connected ? onlineCheckInterval : offlineCheckInterval);
        checker.CheckNow();
        RefreshLabels();
    }

    private void HandleConnectionChanged(bool connected)
    {
        verifiedConnection = connected;
        RefreshPopupVisibility();
        if (connected) ReleaseBlock();
        else MaintainBlock();
    }

    private bool IsContentScene()
    {
        string name = SceneManager.GetActiveScene().name;
        return name == "SkateRunnerStartScreen" || name == "SkateRunner";
    }

    private void BindScene(Scene scene)
    {
        if (boundSceneHandle == scene.handle) return;
        ReleaseBlock();
        boundSceneHandle = scene.handle;
        inputManager = FindFirstObjectByType<InputManager>(FindObjectsInactive.Include);
        eventSystem = EventSystem.current;
        RefreshPopupVisibility();
        if (IsBlocked) MaintainBlock();
    }

    private void RefreshPopupVisibility()
    {
        if (blockingPopup != null) blockingPopup.SetActive(IsBlocked && IsContentScene());
    }

    private void MaintainBlock()
    {
        if (!IsContentScene()) return;
        if (!ownsPause)
        {
            ownsPause = true;
            resumeTimeScale = Time.timeScale;
            resumeFixedDeltaTime = Time.fixedDeltaTime;
        }
        else if (Time.timeScale > 0f)
        {
            // Preserve the intended result of an in-flight hitstop or scene initialization.
            resumeTimeScale = Time.timeScale;
            if (Time.fixedDeltaTime > 0f) resumeFixedDeltaTime = Time.fixedDeltaTime;
        }
        Time.timeScale = 0f;

        if (!ownsInputBlock && inputManager != null)
        {
            inputManagerWasEnabled = inputManager.enabled;
            inputManager.enabled = false;
            ownsInputBlock = true;
        }
        if (!ownsNavigationBlock && eventSystem != null)
        {
            navigationWasEnabled = eventSystem.sendNavigationEvents;
            eventSystem.sendNavigationEvents = false;
            eventSystem.SetSelectedGameObject(null);
            ownsNavigationBlock = true;
        }
    }

    private void ReleaseBlock()
    {
        if (ownsPause)
        {
            Time.timeScale = resumeTimeScale;
            Time.fixedDeltaTime = resumeFixedDeltaTime;
            ownsPause = false;
        }
        if (ownsInputBlock)
        {
            if (inputManager != null) inputManager.enabled = inputManagerWasEnabled;
            ownsInputBlock = false;
        }
        if (ownsNavigationBlock)
        {
            if (eventSystem != null) eventSystem.sendNavigationEvents = navigationWasEnabled;
            ownsNavigationBlock = false;
        }
    }

    private void HandleLocaleChanged(Locale locale) { RefreshLabels(); }

    private void RefreshLabels()
    {
        labelsReady = LocalizationSettings.InitializationOperation.IsDone;
        string title = checking ? "Checking connection..." : "Network connection failed.";
        string body = "Please check your cellular or Wi-Fi connection. We will reconnect automatically.";
        string retry = checking ? "CHECKING..." : "RETRY";
        if (labelsReady)
        {
            title = SkateLocalization.Get("Popups", checking ? "popups.network_checking" : "popups.network_failed");
            body = SkateLocalization.Get("Popups", "popups.network_body");
            retry = SkateLocalization.Get("Popups", checking ? "popups.network_checking_button" : "popups.network_retry");
        }
        if (messageTitle != null) messageTitle.text = title;
        if (messageBody != null) messageBody.text = body;
        if (retryLabel != null) retryLabel.text = retry;
        if (retryButton != null) retryButton.interactable = !checking;
    }

    private void CancelProbe()
    {
        var request = activeRequest;
        if (request != null) request.Abort();
        if (probeCoroutine != null) StopCoroutine(probeCoroutine);
        if (request != null) request.Dispose();
        activeRequest = null;
        probeCoroutine = null;
        checking = false;
    }

    private void OnApplicationPause(bool paused)
    {
        applicationSuspended = paused;
        if (paused) CancelProbe();
        else
        {
            verifiedConnection = false;
            checker.CheckNow();
            nextCheckAt = 0f;
        }
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        CancelProbe();
        ReleaseBlock();
        checker.ConnectionStatusChanged -= HandleConnectionChanged;
        checker.ConnectionEvaluator = null;
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        instance = null;
    }
}
