using System.Collections;
using Elroi.DailyMissions;
using Elroi.Tutorials;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scene-authored home lessons, driven by real navigation and reward transactions.</summary>
[DisallowMultipleComponent]
public sealed class SkateRunnerHomeTutorial : MonoBehaviour
{
    public enum Stage
    {
        Rewards, DayOne, RewardsConfirm, RewardsBack,
        Missions, CollectMission, MissionConfirm, MissionBack,
        Gems, Cash, Inventory, Swords, Abilities, Rollerblades,
        InventoryBack, Shop, ShopIntro, ShopBack, Finish
    }

    [SerializeField] private TutorialManager tutorials;
    [SerializeField] private HomeFullscreenPopupManager navigation;
    [SerializeField] private DailyRewardsPage rewards;
    [SerializeField] private CrystalRewardRevealPopup rewardPopup;
    [SerializeField] private Button rewardOk;
    [SerializeField] private ScrollRect dailyMissionScroll;
    [SerializeField] private RectTransform gemMissionRow;
    [SerializeField] private UIClickToggle shopSwordsTab;
    [SerializeField] private CanvasGroup transitionBlocker;

    private float previousTimeScale;
    private bool ownsPause;
    private bool previousNavigationEvents;
    private EventSystem eventSystem;
    private bool lessonCompleted;
    public bool IsRunning { get; private set; }
    public Stage CurrentStage { get; private set; }

    private IEnumerator Start()
    {
        if (!SkateRunnerFirstRunProgress.NeedsHomeTour) yield break;
        // The loading screen owns its own time/audio restoration. Let it finish first.
        while (FindAnyObjectByType<SkateRunnerLoadingSceneManager>() != null || SkateRunnerConnectivityGate.IsBlocked)
            yield return null;
        yield return null;
        if (tutorials == null || navigation == null || rewards == null || rewardPopup == null || rewardOk == null)
        {
            Debug.LogError("[Home Tutorial] Missing scene references. Tour remains pending for the next visit.", this);
            yield break;
        }

        IsRunning = true;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            previousNavigationEvents = eventSystem.sendNavigationEvents;
            eventSystem.sendNavigationEvents = false;
            eventSystem.SetSelectedGameObject(null);
        }
        SetBlocker(true);
        tutorials.TutorialCompleted += OnLessonCompleted;
        int next = Mathf.Clamp(SkateRunnerFirstRunProgress.HomeStep, 0, (int)Stage.Finish);

        while (next <= (int)Stage.Finish)
        {
            CurrentStage = (Stage)next;
            PreparePage(CurrentStage);
            // Navigation pulses and layout settle using unscaled time.
            yield return new WaitForSecondsRealtime(0.3f);

            if (CurrentStage == Stage.DayOne && rewards.IsDayOneClaimed)
            {
                next++;
                SkateRunnerFirstRunProgress.SaveHomeStep(next);
                continue;
            }
            if (CurrentStage == Stage.CollectMission && DailyMissionProgress.IsClaimed(DailyMissionId.CollectGems))
            {
                next++;
                SkateRunnerFirstRunProgress.SaveHomeStep(next);
                continue;
            }
            if (CurrentStage == Stage.RewardsConfirm || CurrentStage == Stage.MissionConfirm)
            {
                // Closing the app after a grant must never grant again just to replay its popup.
                if (!rewardPopup.IsShowing)
                {
                    next++;
                    SkateRunnerFirstRunProgress.SaveHomeStep(next);
                    continue;
                }
                while (rewardPopup.IsShowing && (!rewardOk.gameObject.activeInHierarchy || !rewardOk.IsInteractable()))
                    yield return null;
            }

            var lesson = tutorials.GetTutorial(CurrentStage.ToString());
            if (lesson == null)
            {
                Debug.LogError("[Home Tutorial] Missing lesson: " + CurrentStage, this);
                Release();
                yield break;
            }
            // Daily progress can reset at midnight while a partially finished tour is saved.
            // Explain the real requirement instead of presenting an unclickable claim button.
            bool missionExpired = CurrentStage == Stage.CollectMission
                && DailyMissionProgress.GetProgress(DailyMissionId.CollectGems) < 30;
            var originalCompletion = lesson.CompletionType;
            var originalCopy = lesson.IntroductionText;
            var originalInstruction = lesson.BottomInstructionText;
            if (missionExpired)
            {
                lesson.CompletionType = TutorialCompletionType.OkButton;
                lesson.IntroductionText = "Daily missions <wave a=0.25><font=\"SkateRunner Tutorial Action\">refresh each day</font></wave>. Collect <bounce a=0.3><font=\"SkateRunner Tutorial Action\">30 Gems</font></bounce> today, then claim this bonus here!";
                lesson.BottomInstructionText = "<wave a=0.2>More missions. More rewards.</wave>";
            }

            lessonCompleted = false;
            if (!tutorials.TryPlayTutorial(lesson.TutorialName))
            {
                lesson.CompletionType = originalCompletion;
                lesson.IntroductionText = originalCopy;
                lesson.BottomInstructionText = originalInstruction;
                Release();
                yield break;
            }
            SetBlocker(false);
            bool dayChangedWhileWaiting = false;
            while (!lessonCompleted)
            {
                if (CurrentStage == Stage.CollectMission && !missionExpired
                    && DailyMissionProgress.GetProgress(DailyMissionId.CollectGems) < 30)
                {
                    dayChangedWhileWaiting = true;
                    tutorials.StopCurrentTutorial();
                    break;
                }
                yield return null;
            }
            SetBlocker(true);
            lesson.CompletionType = originalCompletion;
            lesson.IntroductionText = originalCopy;
            lesson.BottomInstructionText = originalInstruction;
            // TutorialManager releases its pointer/completion lock at the end of the frame.
            yield return new WaitForEndOfFrame();
            yield return null;

            if (dayChangedWhileWaiting) continue;

            if (CurrentStage == Stage.DayOne && !rewards.IsDayOneClaimed) continue;
            if (CurrentStage == Stage.CollectMission && !missionExpired
                && !DailyMissionProgress.IsClaimed(DailyMissionId.CollectGems)) continue;
            if (CurrentStage == Stage.RewardsConfirm || CurrentStage == Stage.MissionConfirm)
                while (rewardPopup.IsShowing) yield return null;
            next++;
            SkateRunnerFirstRunProgress.SaveHomeStep(next);
        }

        SkateRunnerFirstRunProgress.CompleteHomeTour();
        navigation.OpenHome();
        Release();
    }

    private void PreparePage(Stage stage)
    {
        switch (stage)
        {
            case Stage.Rewards: case Stage.Missions: case Stage.Gems: case Stage.Cash:
            case Stage.Inventory: case Stage.Shop: case Stage.Finish:
                navigation.OpenHome(); break;
            case Stage.DayOne: case Stage.RewardsBack:
                if (navigation.CurrentPage == null || navigation.CurrentPage.name != "RewardsPage") navigation.OpenRewards();
                break;
            case Stage.CollectMission: case Stage.MissionBack:
                if (navigation.CurrentPage == null || navigation.CurrentPage.name != "MissionPage") navigation.OpenMissions();
                Canvas.ForceUpdateCanvases();
                FocusMission();
                break;
            case Stage.Swords: case Stage.Abilities: case Stage.Rollerblades: case Stage.InventoryBack:
                if (navigation.CurrentPage == null || navigation.CurrentPage.name != "InventoryPage") navigation.OpenInventory();
                break;
            case Stage.ShopIntro: case Stage.ShopBack:
                if (navigation.CurrentPage == null || navigation.CurrentPage.name != "ShopPage") navigation.OpenShop();
                if (stage == Stage.ShopIntro && shopSwordsTab != null) shopSwordsTab.ApplyToggle();
                break;
        }
    }

    private void FocusMission()
    {
        if (dailyMissionScroll == null || gemMissionRow == null || dailyMissionScroll.content == null) return;
        var viewport = dailyMissionScroll.viewport != null ? dailyMissionScroll.viewport : dailyMissionScroll.transform as RectTransform;
        if (viewport == null) return;
        dailyMissionScroll.StopMovement();
        var rowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, gemMissionRow);
        float hidden = dailyMissionScroll.content.rect.height - viewport.rect.height;
        if (hidden > 0f)
            dailyMissionScroll.verticalNormalizedPosition = Mathf.Clamp01(dailyMissionScroll.verticalNormalizedPosition
                - (viewport.rect.center.y - rowBounds.center.y) / hidden);
    }

    private void OnLessonCompleted(TutorialDefinition lesson) => lessonCompleted = true;
    private void SetBlocker(bool active)
    {
        if (transitionBlocker == null) return;
        transitionBlocker.blocksRaycasts = active;
        transitionBlocker.gameObject.SetActive(active);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (IsRunning && tutorials != null) tutorials.StopCurrentTutorial();
        Release();
    }

    private void Release()
    {
        if (tutorials != null) tutorials.TutorialCompleted -= OnLessonCompleted;
        SetBlocker(false);
        if (ownsPause) Time.timeScale = previousTimeScale;
        ownsPause = false;
        if (eventSystem != null) eventSystem.sendNavigationEvents = previousNavigationEvents;
        IsRunning = false;
    }
}
