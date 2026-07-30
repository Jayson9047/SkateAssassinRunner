using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Explicit Sword identity and full-card click forwarding for an existing Inventory card.</summary>
[DisallowMultipleComponent]
public sealed class SwordInventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SwordId swordId = SwordId.Katana;
    [SerializeField] private SwordInventoryController controller;
    [SerializeField] private InventoryEquippedCardVisual equippedVisual;

    private bool isAvailable = true;

    public SwordId SwordId => swordId;
    public bool IsAvailable => isAvailable;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isAvailable || controller == null)
            return;

        controller.SelectSword(swordId);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = swordId == SwordId.Katana || available;

        if (gameObject.activeSelf != isAvailable)
            gameObject.SetActive(isAvailable);
    }

    public void SetEquipped(bool equipped)
    {
        if (equippedVisual != null)
            equippedVisual.SetEquipped(equipped);
    }
}
