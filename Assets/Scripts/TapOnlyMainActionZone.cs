using UnityEngine;
using UnityEngine.EventSystems;
using MoreMountains.InfiniteRunnerEngine;

public class TapOnlyMainActionZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Tap vs Swipe")]
    [Tooltip("Max finger movement (in pixels) that still counts as a tap.")]
    public float tapMaxMovePixels = 25f;

    [Tooltip("Optional: max duration that still counts as a tap (0 = ignore).")]
    public float tapMaxTimeSeconds = 0f;

    private Vector2 _downPos;
    private float _downTime;
    private bool _isTapCandidate;

    public void OnPointerDown(PointerEventData eventData)
    {
        _downPos = eventData.position;
        _downTime = Time.unscaledTime;
        _isTapCandidate = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // If the finger moves too far, it's not a tap anymore
        if (!_isTapCandidate) return;

        float moved = Vector2.Distance(_downPos, eventData.position);
        if (moved > tapMaxMovePixels)
        {
            _isTapCandidate = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isTapCandidate) return;

        if (tapMaxTimeSeconds > 0f && (Time.unscaledTime - _downTime) > tapMaxTimeSeconds)
        {
            return;
        }

        // Confirm movement is still within tap threshold
        float moved = Vector2.Distance(_downPos, eventData.position);
        if (moved > tapMaxMovePixels) return;

        // Fire MM Main Action as a "tap"
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SendMessage("MainActionButtonDown");
            InputManager.Instance.SendMessage("MainActionButtonUp");
        }
    }
}
