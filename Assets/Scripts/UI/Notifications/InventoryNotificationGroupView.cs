using System;
using UnityEngine;

public enum InventoryNotificationCategory { Swords, Abilities, Rollerblades }

[DisallowMultipleComponent]
public sealed class InventoryNotificationGroupView : MonoBehaviour
{
    [Serializable]
    public sealed class ItemBadge
    {
        public int itemId;
        public NotificationBadgeView badge;
    }

    [SerializeField] private InventoryNotificationCategory category;
    [SerializeField] private NotificationBadgeView categoryBadge;
    [SerializeField] private ItemBadge[] itemBadges;

    private void OnEnable()
    {
        InventoryNewItemNotifications.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        InventoryNewItemNotifications.Changed -= Refresh;
    }

    public void Refresh()
    {
        int total = 0;
        if (itemBadges == null) { if (categoryBadge != null) categoryBadge.SetCount(0); return; }
        for (int i = 0; i < itemBadges.Length; i++)
        {
            if (itemBadges[i] == null || itemBadges[i].badge == null) continue;
            bool unseen = category == InventoryNotificationCategory.Swords ? InventoryNewItemNotifications.IsSwordUnseen((SwordId)itemBadges[i].itemId) :
                          category == InventoryNotificationCategory.Abilities ? InventoryNewItemNotifications.IsAbilityUnseen((WeaponPowerId)itemBadges[i].itemId) :
                          InventoryNewItemNotifications.IsRollerbladeUnseen((RollerbladeId)itemBadges[i].itemId);
            bool owned = category == InventoryNotificationCategory.Swords ? SwordOwnershipSave.IsOwned((SwordId)itemBadges[i].itemId) :
                         category == InventoryNotificationCategory.Abilities ? WeaponPowerOwnershipSave.IsOwned((WeaponPowerId)itemBadges[i].itemId) :
                         RollerbladeOwnershipSave.IsOwned((RollerbladeId)itemBadges[i].itemId);
            unseen &= owned;
            if (unseen) total++;
            itemBadges[i].badge.SetCount(unseen ? 1 : 0);
        }
        if (categoryBadge != null) categoryBadge.SetCount(total);
    }
}
