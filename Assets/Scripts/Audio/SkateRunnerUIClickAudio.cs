using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Authored Buttons use a persistent onClick callback (immune to RemoveAllListeners).
/// Touch controls use their accepted state event; tabs own their actual-change decision.
/// EventSystem fallback supports Buttons created at runtime without an authored callback.
/// </summary>
[DisallowMultipleComponent]
public sealed class SkateRunnerUIClickAudio : MonoBehaviour, IPointerDownHandler,
    IPointerUpHandler, IPointerClickHandler, ISubmitHandler
{
    Button button;
    MMTouchButton touch;
    UIClickToggle tab;
    bool optedOut, persistentButtonCallback, pointerAccepted, groupsInteractable = true;

    void Awake() => Cache();
    void OnEnable()
    {
        pointerAccepted = false;
        Cache();
        if (touch) { touch.ButtonStateChange -= OnTouchState; touch.ButtonStateChange += OnTouchState; }
    }
    void OnDisable()
    {
        if (touch) touch.ButtonStateChange -= OnTouchState;
        // A runtime Button may close its own popup before the same click reaches
        // our fallback handler. Retain that accepted click until dispatch finishes.
    }
    void Cache()
    {
        button = GetComponent<Button>();
        touch = GetComponent<MMTouchButton>();
        tab = GetComponent<UIClickToggle>();
        optedOut = GetComponent<UIClickAudioOptOut>() != null;
        persistentButtonCallback = false;
        if (button)
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(OnButtonClick))
                    persistentButtonCallback = true;
        OnCanvasGroupChanged();
    }
    void OnCanvasGroupChanged()
    {
        groupsInteractable = true;
        for (Transform t = transform; t; t = t.parent)
        {
            bool stop = false;
            foreach (var group in t.GetComponents<CanvasGroup>())
            {
                if (!group.interactable) groupsInteractable = false;
                stop |= group.ignoreParentGroups;
            }
            if (stop) break;
        }
    }
    public bool CanClick => !optedOut && CanInteract;
    public bool CanInteract => isActiveAndEnabled && groupsInteractable &&
        (!button || (button.IsActive() && button.IsInteractable())) &&
        (!touch || (touch.isActiveAndEnabled && touch.Interactable && touch.CurrentState != MMTouchButton.ButtonStates.Disabled));

    public void OnButtonClick()
    {
        if (!tab && !touch && CanClick) SkateRunnerAudioManager.PlayUIButtonClick();
    }
    void OnTouchState(PointerEventData.FramePressState state, PointerEventData data)
    {
        // MMTouchButton has already checked its click filters, buffer and state.
        if (state == PointerEventData.FramePressState.Pressed && !tab && CanClick)
            SkateRunnerAudioManager.PlayUIButtonClick();
    }
    public void OnPointerDown(PointerEventData data) => pointerAccepted = data.button == PointerEventData.InputButton.Left && CanClick;
    public void OnPointerUp(PointerEventData data) => pointerAccepted &= CanClick;
    public void OnPointerClick(PointerEventData data)
    {
        if (button && !touch && !tab && !persistentButtonCallback && pointerAccepted && data.button == PointerEventData.InputButton.Left)
            SkateRunnerAudioManager.PlayUIButtonClick();
        pointerAccepted = false;
    }
    public void OnSubmit(BaseEventData data)
    {
        if (!tab && CanClick && (touch || (button && !persistentButtonCallback)))
            SkateRunnerAudioManager.PlayUIButtonClick();
    }
    public static SkateRunnerUIClickAudio Ensure(GameObject target)
        => target.GetComponent<SkateRunnerUIClickAudio>() ?? target.AddComponent<SkateRunnerUIClickAudio>();
}
