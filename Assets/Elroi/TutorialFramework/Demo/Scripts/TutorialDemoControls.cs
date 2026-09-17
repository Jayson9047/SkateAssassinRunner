using UnityEngine;

namespace Elroi.Tutorials.Demo
{
    public sealed class TutorialDemoControls : MonoBehaviour
    {
        [SerializeField] private TutorialManager manager;
        [SerializeField] private GameObject runtimeTarget;

        public void PlayManualRuntimeTarget() => manager?.TryPlayTutorial("Manual Runtime Target", runtimeTarget);
        public void PlayTwoDimensionalTutorial() => manager?.TryPlayTutorial("2D UI Spotlight");
        public void PlayTap() => manager?.TryPlayTutorial("Gesture - Tap");
        public void PlayDoubleTap() => manager?.TryPlayTutorial("Gesture - Double Tap");
        public void PlaySwipeLeft() => manager?.TryPlayTutorial("Gesture - Swipe Left");
        public void PlaySwipeRight() => manager?.TryPlayTutorial("Gesture - Swipe Right");
        public void PlaySwipeUp() => manager?.TryPlayTutorial("Gesture - Swipe Up");
        public void PlaySwipeDown() => manager?.TryPlayTutorial("Gesture - Swipe Down");
        public void RestartAutomaticSequence() => manager?.TryPlaySequencer("Automatic Trigger Showcase");

        public void ConfigureForAuthoring(TutorialManager tutorialManager, GameObject target)
        {
            manager = tutorialManager;
            runtimeTarget = target;
        }
    }
}
