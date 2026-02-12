using UnityEngine;
using UnityEngine.UI;
using MoreMountains.InfiniteRunnerEngine;

public class LevelEndExitToMenu : MonoBehaviour
{
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private LevelSelector levelSelector;

    private void Reset()
    {
        backToMenuButton = GetComponent<Button>();
        levelSelector = GetComponent<LevelSelector>();
    }

    private void Awake()
    {
        if (backToMenuButton == null) backToMenuButton = GetComponent<Button>();
        if (levelSelector == null) levelSelector = GetComponent<LevelSelector>();

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
    }

    private void OnDestroy()
    {
        if (backToMenuButton != null)
            backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
    }

    private void OnBackToMenuClicked()
    {
        // TODO (Ads):
        // If CashMultiplyAdButton was NOT clicked,
        // show an interstitial ad here before saving & going to main menu.
        // (If multiplier reward was taken, skip interstitial.)

        bool success = SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null
            ? SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.LastLevelSuccess
            : true;

        SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.SaveAfterLevelEnd(success);

        // Keep LevelSelector pure: it just navigates
        levelSelector?.GoToLevel();
    }
}
