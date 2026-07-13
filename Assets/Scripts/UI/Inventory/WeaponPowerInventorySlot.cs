using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Identifies one existing ability card and forwards full-card clicks to the
/// power inventory controller. Availability is intentionally separate from
/// selection and equipped state.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerInventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private WeaponPowerId powerId = WeaponPowerId.None;
    [SerializeField] private WeaponPowerInventoryController controller;

    private bool isAvailable = true;

    public WeaponPowerId PowerId => powerId;
    public bool IsAvailable => isAvailable;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isAvailable || controller == null)
            return;

        controller.SelectPower(powerId);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = powerId == WeaponPowerId.None || available;

        if (gameObject.activeSelf != isAvailable)
            gameObject.SetActive(isAvailable);
    }
}
