#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>Editor-only Play Mode visual regression capture. Never grants rewards or buys items.</summary>
public sealed class LocalizationVisualQA : MonoBehaviour
{
    public static string Status { get; private set; } = "Idle";
    private const string Folder = "Temp/LocalizationQA/production";
    private readonly List<string> observations = new List<string>();
    private readonly Dictionary<string, float> englishSizes = new Dictionary<string, float>();
    private static readonly string[] Codes = { "en", "de", "fr", "pt-BR", "es", "nl", "en" };

    public static void RunHomeScreens()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindFirstObjectByType<LocalizationVisualQA>() != null) throw new InvalidOperationException("QA is already running.");
        EditorApplication.isPaused = false;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        var runner = new GameObject("Localization visual QA (temporary)").AddComponent<LocalizationVisualQA>();
        runner.gameObject.hideFlags = HideFlags.DontSave;
        runner.StartCoroutine(runner.HomeScreens());
    }

    private IEnumerator HomeScreens()
    {
        Directory.CreateDirectory(Folder);
        var manager = FindFirstObjectByType<HomeFullscreenPopupManager>();
        foreach (string code in Codes)
        {
            foreach (SettingsPopup popup in FindObjectsByType<SettingsPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) popup.Close();
            manager.OpenHome();
            SkateLocalization.SelectLocale(code);
            yield return Capture(code, "home");
            if (code == "en") CompareEnglishSizes();

            Settings().Open();
            yield return new WaitForSecondsRealtime(0.7f);
            FindFirstObjectByType<SettingsPopupController>().ShowMainPage();
            yield return Capture(code, "settings");
            Button("/Section_General/Button_Language").onClick.Invoke();
            yield return Capture(code, "languages");
            FindFirstObjectByType<SettingsPopupController>().ShowMainPage();
            yield return Capture(code, "settings-reopened");
            Settings().Close();
            yield return new WaitForSecondsRealtime(0.5f);

            manager.OpenMissions();
            yield return Capture(code, "missions");
            var missionTabs = FindFirstObjectByType<Elroi.DailyMissions.UI.MissionPageTabController>();
            missionTabs.ShowStory();
            yield return Capture(code, "missions-story");
            missionTabs.ShowMafia();
            yield return Capture(code, "missions-mafia");
            manager.OpenRewards();
            yield return Capture(code, "rewards");
            manager.OpenShop();
            foreach (string tab in new[] { "Text_Abilities", "Text_Swords", "Text_Rollerblades", "Text_CurrencyPack" })
            {
                Tab("/ShopPage/Shop/Tap /" + tab).ApplyToggle();
                yield return Capture(code, "shop-" + tab.Substring(5));
            }
            var confirmation = FindObjectsByType<WeaponPowerPurchasePopup>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            confirmation.ShowConfirmation(SkateLocalization.Get("Shop", "shop.confirm_purchase"),
                SkateLocalization.BuildShopConfirmation(ShopPaymentType.Cash, 1000, "$0.99 USD", SkateLocalization.GetAbilityDisplayName(WeaponPowerId.Magic)), null, null);
            yield return Capture(code, "purchase-confirmation");
            confirmation.Close();

            manager.OpenInventory();
            foreach (string tab in new[] { "Swords", "Abilities", "RollerBlades" })
            {
                Tab("/InventoryPage/Left/MidScreenLeft/Tap_Menu/GameObject/" + tab).ApplyToggle();
                yield return Capture(code, "inventory-" + tab);
                foreach (ScrollRect scroll in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Where(s => PathOf(s).Contains("InventoryListFrame")))
                { scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; }
                yield return Capture(code, "inventory-" + tab + "-bottom");
            }
            manager.OpenHome();
            var cash = FindFirstObjectByType<Elroi.DailyMissions.UI.FreeCashDailyPopup>(FindObjectsInactive.Include);
            cash.Open();
            yield return Capture(code, "free-cash");
            cash.Close();
            yield return new WaitForSecondsRealtime(0.5f);
            Roulette().Open();
            yield return Capture(code, "spin");
            Roulette().Close();
            yield return new WaitForSecondsRealtime(0.5f);
            CrystalRewardRevealPopup.TryShow(RewardRevealRequest.ForCurrencies(1000, 5));
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture(code, "reward-reveal");
            CrystalRewardRevealPopup.CloseActiveImmediate();
        }

        // Separate repeated switches exercise the same live, visible Settings rows.
        manager.OpenHome();
        Settings().Open();
        yield return new WaitForSecondsRealtime(0.7f);
        FindFirstObjectByType<SettingsPopupController>().ShowMainPage();
        for (int cycle = 0; cycle < 3; cycle++)
        foreach (string code in Codes)
        {
            SkateLocalization.SelectLocale(code);
            yield return new WaitForSecondsRealtime(0.25f);
            Audit(code + "-cycle" + cycle);
        }
        Settings().Close();
        manager.OpenHome();
        yield return Capture("en", "home-final");
        CompareEnglishSizes();
        File.WriteAllLines(Folder + "/observations.txt", observations);
        Status = "Home QA complete";
        Destroy(gameObject);
    }

    public static void RunMissionTabs()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindFirstObjectByType<LocalizationVisualQA>() != null) throw new InvalidOperationException("QA is already running.");
        EditorApplication.isPaused = false;
        var runner = new GameObject("Localization mission tab QA (temporary)").AddComponent<LocalizationVisualQA>();
        runner.StartCoroutine(runner.MissionTabs());
    }

    private IEnumerator MissionTabs()
    {
        var manager = FindFirstObjectByType<HomeFullscreenPopupManager>();
        manager.OpenMissions();
        var tabs = FindFirstObjectByType<Elroi.DailyMissions.UI.MissionPageTabController>();
        foreach (string code in Codes)
        {
            SkateLocalization.SelectLocale(code);
            tabs.ShowStory();
            yield return Capture(code, "missions-story");
            tabs.ShowMafia();
            yield return Capture(code, "missions-mafia");
        }
        manager.OpenHome();
        File.WriteAllLines(Folder + "/mission-tab-observations.txt", observations);
        Status = "Mission tabs QA complete";
        Destroy(gameObject);
    }

    public static void RunGameplayScreens()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        EditorApplication.isPaused = false;
        var runner = new GameObject("Localization gameplay QA (temporary)").AddComponent<LocalizationVisualQA>();
        DontDestroyOnLoad(runner.gameObject);
        runner.StartCoroutine(runner.GameplayScreens());
    }

    private IEnumerator GameplayScreens()
    {
        string code = SkateLocalization.CurrentLocaleCode;
        Directory.CreateDirectory(Folder);
        observations.Add("Cold start locale: " + code + "; saved: " + PlayerPrefs.GetString(SkateLocalization.SelectedLocalePlayerPrefsKey));
        Status = code + " / loading gameplay";
        FindFirstObjectByType<MoreMountains.InfiniteRunnerEngine.StartScreen>().GoToLevel();
        float deadline = Time.realtimeSinceStartup + 45;
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SkateRunner"
            || Elroi.Missions.MissionSystem.MissionSystemAccessor == null
            || Elroi.Missions.MissionSystem.MissionSystemAccessor.ActiveMissions.Count == 0)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Gameplay missions did not initialize.");
            yield return null;
        }
        // Allow the real late-subscribing binder to run; never force-refresh here.
        yield return null;
        yield return null;
        var gui = MoreMountains.InfiniteRunnerEngine.SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor;
        var game = MoreMountains.InfiniteRunnerEngine.GameManager.Instance;
        foreach (var binder in FindObjectsByType<Elroi.Missions.UI.MissionUIBinder>(FindObjectsSortMode.None))
        {
            if (binder.mission1?.missionText) observations.Add("INITIAL MISSION 1: " + binder.mission1.missionText.text);
            if (binder.mission2?.missionText) observations.Add("INITIAL MISSION 2: " + binder.mission2.missionText.text);
        }
        // Let scene fade-in and startup HUD animations settle before visual QA.
        yield return new WaitForSecondsRealtime(2.5f);
        game.Pause();
        gui.SetPause(false);
        yield return Capture(code, "gameplay-missions");
        gui.SetPause(true);
        yield return Capture(code, "pause");
        gui.SetPause(false);
        foreach (Animator animator in gui.GameOverScreen.GetComponentsInChildren<Animator>(true))
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        gui.SetGameOverScreen(true);
        yield return Capture(code, "death");
        gui.SetGameOverScreen(false);
        gui.ShowLevelEndScreen(false);
        yield return Capture(code, "result");
        // No reward/save/exit button is invoked by the presentation test.
        game.UnPause();
        UnityEngine.SceneManagement.SceneManager.LoadScene("SkateRunnerStartScreen");
        yield return null;
        yield return Capture(code, "returned-home");
        observations.Add("Returned Home locale: " + SkateLocalization.CurrentLocaleCode);
        File.WriteAllLines(Folder + "/gameplay-" + code + ".txt", observations);
        Status = "Gameplay QA complete: " + code;
        Destroy(gameObject);
    }

    private IEnumerator Capture(string code, string screen)
    {
        Status = code + " / " + screen;
        yield return new WaitForSecondsRealtime(0.7f);
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        int width = Mathf.Min(1440, screenshot.width);
        int height = Mathf.RoundToInt(screenshot.height * (float)width / screenshot.width);
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(screenshot, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var resized = new Texture2D(width, height, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(Folder + "/" + code + "-" + screen + ".png", resized.EncodeToPNG());
        Destroy(resized);
        Destroy(screenshot);
        Audit(Status);
        if (code == "en")
        {
            foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (!t.GetComponent<LocalizedTMPFitPolicy>() || t.canvasRenderer.cull) continue;
                string key = screen + "|" + PathOf(t) + "|" + t.text;
                if (englishSizes.TryGetValue(key, out float size) && Mathf.Abs(size - t.fontSize) > 0.01f)
                    observations.Add("DRIFT " + key + " " + size + " -> " + t.fontSize);
                else englishSizes[key] = t.fontSize;
            }
        }
    }

    private void Audit(string context)
    {
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (PathOf(t).Contains("Debug")) continue;
            if (t.canvasRenderer.cull || t.transform.lossyScale.x < 0.01f) continue;
            LocalizedTMPFitPolicy fit = t.GetComponent<LocalizedTMPFitPolicy>();
            LocalizeStringEvent localizer = t.GetComponent<LocalizeStringEvent>();
            bool hiddenAction = t.GetComponentInParent<Elroi.DailyMissions.UI.FreeCashRewardRowUI>() != null
                && t.GetComponentInParent<Button>() != null && !t.GetComponentInParent<Button>().interactable;
            if (localizer && !hiddenAction && string.IsNullOrWhiteSpace(t.text)) observations.Add(context + " EMPTY " + PathOf(t));
            if (fit && t.fontSize < fit.maxFontSize * 0.79f) observations.Add(context + " SHRUNK " + PathOf(t));
            if (fit && t.fontSize < 16f && !string.IsNullOrWhiteSpace(t.text)) observations.Add(context + " TOO SMALL " + PathOf(t));
            if (fit && (!t.font || !t.fontSharedMaterial)) observations.Add(context + " MISSING FONT/MATERIAL " + PathOf(t));
            if (fit && fit.fitToSingleLine && t.textInfo.lineCount > 1) observations.Add(context + " WRAPPED " + PathOf(t) + " = " + t.text);
            if (!string.IsNullOrWhiteSpace(t.text) && t.textInfo.characterCount > 0 && t.font && t.font.name.Contains("Liberation")) observations.Add(context + " FONT " + PathOf(t));
            if (PathOf(t).Contains("Section_Audio/Row_") || PathOf(t).Contains("MagicSlot/Text_Name"))
                observations.Add(context + " VISIBLE " + PathOf(t) + " = " + t.text + " size " + t.fontSize);
        }
    }

    private void CompareEnglishSizes()
    {
        foreach (LocalizedTMPFitPolicy fit in FindObjectsByType<LocalizedTMPFitPolicy>(FindObjectsSortMode.None))
        {
            TMP_Text text = fit.GetComponent<TMP_Text>();
            if (!PathOf(text).Contains("HomepageRoot")) continue;
            string key = PathOf(text);
            if (englishSizes.TryGetValue(key, out float size) && Mathf.Abs(size - text.fontSize) > 0.01f)
                observations.Add("DRIFT " + key + " " + size + " -> " + text.fontSize);
            else englishSizes[key] = text.fontSize;
        }
    }

    private static string PathOf(Component t) => AnimationUtility.CalculateTransformPath(t.transform, null);
    private static SettingsPopup Settings() => FindObjectsByType<SettingsPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(p => p.name == "Popup");
    private static SettingsPopup Roulette() => FindObjectsByType<SettingsPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(p => p.name == "Roulette");
    private static Button Button(string suffix) => FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => PathOf(b).EndsWith(suffix));
    private static UIClickToggle Tab(string suffix) => FindObjectsByType<UIClickToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => PathOf(b).EndsWith(suffix));
}
#endif
