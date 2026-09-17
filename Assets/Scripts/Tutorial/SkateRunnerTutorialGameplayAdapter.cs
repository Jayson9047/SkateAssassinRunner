using System.Collections;
using Elroi.Tutorials;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

public sealed class SkateRunnerTutorialGameplayAdapter : MonoBehaviour, ITutorialGameplayAdapter, ITutorialAsyncGameplayAdapter
{
    [SerializeField] private TapOnlyMainActionZone mainActionZone;
    [SerializeField] private SwipeRightAttackDetector dashDetector;
    [SerializeField] private SwipeDownDetector downDetector;

    [Header("Composite Actions")]
    [SerializeField, Min(0f)] private float tutorialDoubleTapReplayInterval = 0.10f;

    public void SetGameplayInputBlocked(bool blocked)
    {
        if (LevelManager.Instance == null) return;
        if (blocked) LevelManager.Instance.LockGameplayInputs();
        else LevelManager.Instance.UnlockGameplayInputs();
    }

    public bool CanExecuteGameplayAction(string actionId, out string reason)
    {
        ResolveLiveReferences();
        switch (actionId)
        {
            case "Jump":
            case "DoubleJump":
                reason = mainActionZone == null ? "TapOnlyMainActionZone is not available." : null;
                return mainActionZone != null;
            case "DashAttack":
            case "AirDashAttack":
                reason = dashDetector == null ? "SwipeRightAttackDetector is not available." : null;
                return dashDetector != null;
            case "DownAttack":
                reason = downDetector == null ? "SwipeDownDetector is not available." : null;
                return downDetector != null;
            default:
                reason = $"Unknown Skate Runner tutorial action ID '{actionId}'.";
                return false;
        }
    }

    public void ExecuteGameplayAction(string actionId)
    {
        ResolveLiveReferences();
        switch (actionId)
        {
            case "Jump":
                mainActionZone?.TriggerTutorialMainAction();
                break;
            case "DoubleJump":
                StartCoroutine(ExecuteDoubleJumpRoutine());
                break;
            case "DashAttack":
            case "AirDashAttack":
                dashDetector?.TriggerTutorialDashAttack();
                break;
            case "DownAttack":
                downDetector?.TriggerTutorialDownAttack();
                break;
        }
    }

    public IEnumerator ExecuteGameplayActionRoutine(string actionId)
    {
        ResolveLiveReferences();
        if (actionId == "DoubleJump")
        {
            yield return ExecuteDoubleJumpRoutine();
            yield break;
        }

        ExecuteGameplayAction(actionId);
    }

    private IEnumerator ExecuteDoubleJumpRoutine()
    {
        if (mainActionZone == null) yield break;

        mainActionZone.TriggerTutorialMainAction();
        if (tutorialDoubleTapReplayInterval > 0f)
            yield return new WaitForSecondsRealtime(tutorialDoubleTapReplayInterval);
        mainActionZone.TriggerTutorialMainAction();
    }

    private void ResolveLiveReferences()
    {
        if (mainActionZone == null) mainActionZone = FindFirstObjectByType<TapOnlyMainActionZone>();
        if (dashDetector == null) dashDetector = FindFirstObjectByType<SwipeRightAttackDetector>();
        if (downDetector == null) downDetector = FindFirstObjectByType<SwipeDownDetector>();

        if (dashDetector != null)
        {
            TutorialTargetMarker marker = dashDetector.GetComponent<TutorialTargetMarker>();
            if (marker == null) marker = dashDetector.gameObject.AddComponent<TutorialTargetMarker>();
            if (string.IsNullOrWhiteSpace(marker.TargetId)) marker.TargetId = "Tutorial.Player";
            TutorialTargetRegistry.Register(marker);
        }
    }
}
