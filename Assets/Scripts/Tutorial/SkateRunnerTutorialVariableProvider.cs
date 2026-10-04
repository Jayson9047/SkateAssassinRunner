using System;
using Elroi.Tutorials;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

public sealed class SkateRunnerTutorialVariableProvider : MonoBehaviour, ITutorialVariableProvider
{
    public event Action<string, TutorialValue> VariableChanged;

    [Header("Gameplay Tutorial Signals")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private PowerMeter powerMeter;
    [SerializeField] private GameObject ruthlessTimer;
    [SerializeField] private GameObject ruthlessTapPrompt;
    [SerializeField] private GameObject missionHud;

    private readonly System.Collections.Generic.Dictionary<string, bool> published =
        new System.Collections.Generic.Dictionary<string, bool>();
    private float normalTimeSince = -1f;

    private void Update()
    {
        // Let kill hit-stop finish before the five-kill lesson owns the pause.
        if (!Mathf.Approximately(Time.timeScale, 1f)) normalTimeSince = -1f;
        else if (normalTimeSince < 0f) normalTimeSince = Time.unscaledTime;
        Publish("PowerSlamTutorialReady");
        Publish("MissionHudTutorialReady");
        Publish("Phase2TutorialReady");
        Publish("RuthlessTimerTutorialReady");

        var mode = RuthlessTapModeController.Instance;
        bool showPrompt = mode != null && mode.IsActive &&
            LevelManager.Instance != null && LevelManager.Instance.RuthlessTapModeEntered &&
            (tutorialManager == null || !tutorialManager.IsTutorialPlaying);
        if (ruthlessTapPrompt != null && ruthlessTapPrompt.activeSelf != showPrompt)
            ruthlessTapPrompt.SetActive(showPrompt);
    }

    private void Publish(string id)
    {
        if (!TryGetValue(id, out TutorialValue value)) return;
        if (published.TryGetValue(id, out bool previous) && previous == value.BooleanValue) return;
        published[id] = value.BooleanValue;
        VariableChanged?.Invoke(id, value);
    }

    private void OnEnable() => SkateRunnerGameManager.OnLevelChanged += HandleLevelChanged;
    private void OnDisable() => SkateRunnerGameManager.OnLevelChanged -= HandleLevelChanged;

    public bool TryGetValue(string variableId, out TutorialValue value)
    {
        if (string.Equals(variableId, "CurrentLevel", StringComparison.Ordinal) && SkateRunnerGameManager.SkateRunnerGameManagerAccessor != null)
        {
            value = TutorialValue.From(SkateRunnerGameManager.SkateRunnerGameManagerAccessor.LevelNum);
            return true;
        }
        var game = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
        var level = SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor;
        var gui = SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor;
        bool available = game != null && game.Status == GameManager.GameStatus.GameInProgress &&
            tutorialManager != null && !tutorialManager.IsSequencerPlaying && !tutorialManager.IsTutorialPlaying;
        bool phase2 = level != null && level.IsPhase2BossActive;
        switch (variableId)
        {
            case "MissionHudTutorialReady":
                var missions = Elroi.Missions.MissionSystem.MissionSystemAccessor;
                value = TutorialValue.From(available && game.LevelNum == 2 && !phase2 &&
                    level != null && level.Phase1ElapsedSeconds >= 0.5f &&
                    missionHud != null && missionHud.activeInHierarchy &&
                    missions != null && missions.ActiveMissions.Count == 2 &&
                    normalTimeSince >= 0f && Time.unscaledTime - normalTimeSince >= 0.2f);
                return true;
            case "PowerSlamTutorialReady":
                value = TutorialValue.From(available && game.LevelNum == 2 && !phase2 && gui != null && gui.IsSlamReady() &&
                    normalTimeSince >= 0f && Time.unscaledTime - normalTimeSince >= 0.2f);
                return true;
            case "Phase2TutorialReady":
                value = TutorialValue.From(available && phase2 && gui != null && gui.IsPhase2HudReady &&
                    powerMeter != null && powerMeter.IsRunning);
                return true;
            case "RuthlessTimerTutorialReady":
                // Catch the award before the slam/camera transition and the real-time tap window.
                value = TutorialValue.From(available && phase2 && gui != null && gui.HasPhase2RuthlessAward &&
                    ruthlessTimer != null && ruthlessTimer.activeInHierarchy &&
                    powerMeter != null && !powerMeter.IsRunning &&
                    (RuthlessTapModeController.Instance == null || !RuthlessTapModeController.Instance.IsActive));
                return true;
        }
        value = default;
        return false;
    }

    private void HandleLevelChanged(int level) => VariableChanged?.Invoke("CurrentLevel", TutorialValue.From(level));
}
