using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Elroi.Tutorials
{
    // Observes the real pointer click without replacing the target's existing handlers.
    public sealed class TutorialTargetClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var callback = Clicked;
            Clicked = null;
            callback?.Invoke();
        }
    }
}
