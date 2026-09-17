using UnityEngine;

namespace Elroi.Tutorials
{
    [DisallowMultipleComponent]
    public sealed class TutorialEventEmitter : MonoBehaviour
    {
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private string eventId;

        public string EventId { get => eventId; set => eventId = value; }

        public void Fire()
        {
            if (tutorialManager == null) tutorialManager = GetComponentInParent<TutorialManager>();
            if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (tutorialManager != null) tutorialManager.RaiseEvent(eventId);
            else Debug.LogWarning($"[ELROI Tutorials] Event '{eventId}' has no scene-local TutorialManager.", this);
        }
    }
}
