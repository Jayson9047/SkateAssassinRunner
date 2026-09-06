using UnityEngine;

/// <summary>Owns all Inventory category-tab counts independently of individual ScrollRect lifecycles.</summary>
[DisallowMultipleComponent]
public sealed class InventoryNotificationBadgeController : MonoBehaviour
{
    [SerializeField] private NotificationBadgeView swordsBadge;
    [SerializeField] private NotificationBadgeView abilitiesBadge;
    [SerializeField] private NotificationBadgeView rollerbladesBadge;

    private void OnEnable()
    {
        // Defensive removal keeps subscriptions unique if this component is toggled unusually.
        InventoryNewItemNotifications.Changed -= RefreshAllBadges;
        InventoryNewItemNotifications.Changed += RefreshAllBadges;
        RefreshAllBadges();
    }

    private void OnDisable()
    {
        InventoryNewItemNotifications.Changed -= RefreshAllBadges;
    }

    public void RefreshAllBadges()
    {
        if (swordsBadge != null)
            swordsBadge.SetCount(InventoryNewItemNotifications.SwordCount);
        if (abilitiesBadge != null)
            abilitiesBadge.SetCount(InventoryNewItemNotifications.AbilityCount);
        if (rollerbladesBadge != null)
            rollerbladesBadge.SetCount(InventoryNewItemNotifications.RollerbladeCount);
    }
}
