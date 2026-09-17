using UnityEngine;

namespace Elroi.Tutorials.Demo
{
    public sealed class TutorialDemoGameplayAdapter : MonoBehaviour, ITutorialGameplayAdapter
    {
        [SerializeField] private Transform actionIndicator;
        public bool InputBlocked { get; private set; }
        public int ExecutedActionCount { get; private set; }
        public string LastActionId { get; private set; }

        public void SetGameplayInputBlocked(bool blocked) => InputBlocked = blocked;

        public bool CanExecuteGameplayAction(string actionId, out string reason)
        {
            reason = string.IsNullOrWhiteSpace(actionId) ? "Action ID is empty." : null;
            return reason == null;
        }

        public void ExecuteGameplayAction(string actionId)
        {
            ExecutedActionCount++;
            LastActionId = actionId;
            if (actionIndicator != null)
                actionIndicator.localScale = Vector3.one * (1f + Mathf.Min(ExecutedActionCount, 5) * 0.1f);
            Debug.Log($"[ELROI Tutorial Demo] Executed gameplay action '{actionId}' exactly once (count {ExecutedActionCount}).", this);
        }

        public void ConfigureForAuthoring(Transform indicator) => actionIndicator = indicator;
    }
}
