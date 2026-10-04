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

    [Header("Airborne Enemy Lesson Framing")]
    [SerializeField] private CameraFollowTargetYOnly tutorialCameraFollow;
    [SerializeField] private string airborneEnemyTargetId = "Tutorial.DownAttackTarget";
    [SerializeField] private float airborneLessonCameraOffset = -8f;
    private TutorialManager manager;
    private bool cameraOffsetOwned;
    private float savedCameraOffset;

    private bool ownsInputBlock;
    private bool inputWasAlreadyBlocked;

    private void OnEnable()
    {
        manager = GetComponent<TutorialManager>();
        if (manager == null) return;
        manager.TutorialStarted += FrameAirborneEnemy;
        manager.TutorialCompleted += RestoreLessonCamera;
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.TutorialStarted -= FrameAirborneEnemy;
            manager.TutorialCompleted -= RestoreLessonCamera;
        }
        RestoreLessonCamera(null);
    }

    private void FrameAirborneEnemy(TutorialDefinition lesson)
    {
        if (tutorialCameraFollow == null || lesson.RuntimeTargetId != airborneEnemyTargetId ||
            lesson.GameplayActionId != "DownAttack") return;
        // Keep the grounded enemy visible while the player is paused at the apex.
        savedCameraOffset = tutorialCameraFollow.yOffset;
        cameraOffsetOwned = true;
        tutorialCameraFollow.yOffset += airborneLessonCameraOffset;
    }

    private void RestoreLessonCamera(TutorialDefinition lesson)
    {
        if (!cameraOffsetOwned) return;
        if (tutorialCameraFollow != null) tutorialCameraFollow.yOffset = savedCameraOffset;
        cameraOffsetOwned = false;
    }

    public void SetGameplayInputBlocked(bool blocked)
    {
        if (LevelManager.Instance == null) return;
        if (blocked)
        {
            if (!ownsInputBlock) inputWasAlreadyBlocked = LevelManager.Instance.GameplayInputsLocked;
            ownsInputBlock = true;
            LevelManager.Instance.LockGameplayInputs();
        }
        else if (ownsInputBlock)
        {
            RestoreLessonCamera(null);
            ownsInputBlock = false;
            // Dismissing a lesson must preserve Phase 2's own input lock.
            var level = SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor;
            if (!inputWasAlreadyBlocked && (level == null || !level.IsPhase2BossActive))
                LevelManager.Instance.UnlockGameplayInputs();
        }
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
