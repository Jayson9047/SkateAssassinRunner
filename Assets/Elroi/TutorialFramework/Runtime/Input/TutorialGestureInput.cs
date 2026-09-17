using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Elroi.Tutorials
{
    public sealed class TutorialGestureInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private readonly TutorialGestureRecognizer recognizer = new TutorialGestureRecognizer();
        private TutorialGesture requiredGesture;
        private TutorialGestureSettings settings;
        private Action completion;
        private bool listening;

        public void Begin(TutorialGesture gesture, TutorialGestureSettings gestureSettings, Action onCompleted)
        {
            requiredGesture = gesture;
            settings = gestureSettings;
            completion = onCompleted;
            recognizer.Reset();
            listening = gesture != TutorialGesture.None;
        }

        public void End()
        {
            listening = false;
            completion = null;
            recognizer.Reset();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (listening) recognizer.PointerDown(eventData.position, Time.unscaledTime);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!listening) return;
            if (!recognizer.PointerUp(eventData.position, Time.unscaledTime, requiredGesture, settings)) return;
            listening = false;
            completion?.Invoke();
        }
    }
}
