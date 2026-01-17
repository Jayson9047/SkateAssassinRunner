using UnityEngine;
using UnityEngine.EventSystems;

public class RuthlessTapOverlay : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        // Works even when GameplayInputsLocked is true, because we don't check it.
        RuthlessTapModeController.Instance?.RegisterTap();
    }
}
