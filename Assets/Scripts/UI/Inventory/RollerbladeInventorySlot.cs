using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Explicit Rollerblade identity and full-card click forwarding for an existing Inventory card.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeInventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RollerbladeId rollerbladeId = RollerbladeId.Default;
    [SerializeField] private RollerbladeInventoryController controller;

    private bool isAvailable = true;

    public RollerbladeId RollerbladeId => rollerbladeId;
    public bool IsAvailable => isAvailable;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isAvailable || controller == null)
            return;

        controller.SelectRollerblade(rollerbladeId);
    }

    public void SetAvailable(bool available)
    {
        isAvailable = rollerbladeId == RollerbladeId.Default || available;

        if (gameObject.activeSelf != isAvailable)
            gameObject.SetActive(isAvailable);
    }
}
