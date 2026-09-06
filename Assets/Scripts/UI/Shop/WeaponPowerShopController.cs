using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates Weapon Power purchase requests, ES3 gem transactions, ownership,
/// and the shared confirmation popup. It has no Inventory scene references.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerShopController : MonoBehaviour
{
    private const string TotalGemsKey = "TotalGems";

    [SerializeField] private WeaponPowerShopItem[] items;
    [SerializeField] private ShopPricingCatalog pricingCatalog;
    [SerializeField] private WeaponPowerPurchasePopup purchasePopup;
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly Dictionary<WeaponPowerId, WeaponPowerShopItem> itemsById =
        new Dictionary<WeaponPowerId, WeaponPowerShopItem>();

    private WeaponPowerShopItem pendingItem;
    private bool purchaseProcessing;
    private bool mappingBuilt;

    public ShopPricingCatalog PricingCatalog => pricingCatalog;
    public bool TryGetPrice(WeaponPowerId id, out ShopPricingCatalog.AbilityPrice price)
    {
        price = null;
        return pricingCatalog != null && pricingCatalog.TryGet(id, out price);
    }

    private void OnEnable()
    {
        BuildItemMapping();
        pendingItem = null;
        purchaseProcessing = false;

        if (purchasePopup != null)
            purchasePopup.Close();

        RefreshItems();
    }

    private void OnDisable()
    {
        pendingItem = null;
        purchaseProcessing = false;

        if (purchasePopup != null)
            purchasePopup.Close();
    }

    public void RefreshItems()
    {
        BuildItemMapping();

        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].RefreshOwnedState();
        }
    }

    public void RequestPurchase(WeaponPowerShopItem item)
    {
        BuildItemMapping();

        WeaponPowerShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.PowerId, out configuredItem) ||
            configuredItem != item)
        {
            return;
        }

        item.RefreshOwnedState();
        if (item.IsOwned || purchaseProcessing)
            return;

        pendingItem = item;

        if (purchasePopup != null)
        {
            purchasePopup.ShowConfirmation(
                "CONFIRM PURCHASE",
                item.BuildConfirmationMessage(),
                ConfirmPendingPurchase,
                CancelPendingPurchase);
        }
        else
        {
            Debug.LogWarning("Weapon Power Shop is missing its purchase popup reference.", this);
            pendingItem = null;
        }
    }

    public void ConfirmPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        WeaponPowerShopItem item = pendingItem;
        pendingItem = null;

        WeaponPowerShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.PowerId, out configuredItem) ||
            configuredItem != item ||
            !WeaponPowerOwnershipSave.IsValidPowerId(item.PowerId) ||
            item.PowerId == WeaponPowerId.None)
        {
            CloseAndResetPopup();
            return;
        }

        if (WeaponPowerOwnershipSave.IsOwned(item.PowerId))
        {
            item.RefreshOwnedState();
            CloseAndResetPopup();
            return;
        }

        purchaseProcessing = true;

        if (item.PaymentType == ShopPaymentType.Gems)
        {
            CompleteGemPurchase(item);
            return;
        }

        CompleteRealMoneyPlaceholderPurchase(item);
    }

    public void CancelPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        pendingItem = null;
        CloseAndResetPopup();
    }

    private void CompleteGemPurchase(WeaponPowerShopItem item)
    {
        int cost = item.PriceCost;
        if (cost <= 0 || item.PaymentType != ShopPaymentType.Gems)
        {
            CloseAndResetPopup();
            return;
        }

        float currentGems = Mathf.Max(0f, ES3.Load(TotalGemsKey, 0f));
        if (currentGems + 0.0001f < cost)
        {
            int missingGems = Mathf.CeilToInt(cost - currentGems);
            purchaseProcessing = false;

            if (purchasePopup != null)
            {
                purchasePopup.ShowInformation(
                    "NOT ENOUGH GEMS",
                    "You need " + missingGems + " more Gems to buy " + item.ProductDisplayName + ".");
            }

            return;
        }

        float newBalance = Mathf.Max(0f, currentGems - cost);
        ES3.Save(TotalGemsKey, newBalance);

        if (!WeaponPowerOwnershipSave.Grant(item.PowerId))
        {
            ES3.Save(TotalGemsKey, currentGems);
            CloseAndResetPopup();
            return;
        }

        InventoryNewItemNotifications.RegisterAbility(item.PowerId);

        RefreshItems();

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();

        ShowPurchasedAbility(item);
    }

    private void CompleteRealMoneyPlaceholderPurchase(WeaponPowerShopItem item)
    {
        if (item.PaymentType != ShopPaymentType.RealMoney)
        {
            CloseAndResetPopup();
            return;
        }

        if (!ShopRealMoneyPurchaseBridge.IsPlaceholderPurchaseApproved(item.StoreProductId))
        {
            CloseAndResetPopup();
            return;
        }
        if (!WeaponPowerOwnershipSave.Grant(item.PowerId))
        {
            CloseAndResetPopup();
            return;
        }

        InventoryNewItemNotifications.RegisterAbility(item.PowerId);

        RefreshItems();
        ShowPurchasedAbility(item);
    }

    private void ShowPurchasedAbility(WeaponPowerShopItem item)
    {
        Sprite icon = RewardRevealIconUtility.FindProductSprite(item.transform);
        AnimationClip previewAnimation = RewardRevealIconUtility.FindAbilityPreviewAnimation(item.PowerId);
        string displayName = RewardRevealIconUtility.FindProductTitle(
            item.transform,
            item.ProductDisplayName);
        RewardRevealRequest request = RewardRevealRequest.ForItem(
            RewardRevealType.Ability,
            displayName,
            icon,
            previewAnimation: previewAnimation);
        request.primary.displaySize = RewardRevealIconUtility.FindProductDisplaySize(item.transform);
        CloseAndResetPopup();
        CrystalRewardRevealPopup.TryShow(request);
    }

    private void CloseAndResetPopup()
    {
        pendingItem = null;
        purchaseProcessing = false;

        if (purchasePopup != null)
            purchasePopup.Close();
    }

    private void BuildItemMapping()
    {
        if (mappingBuilt)
            return;

        mappingBuilt = true;
        itemsById.Clear();

        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            WeaponPowerShopItem item = items[i];
            if (item == null)
                continue;

            if (itemsById.ContainsKey(item.PowerId))
                Debug.LogWarning("Duplicate Weapon Power Shop mapping for " + item.PowerId + ".", item);

            itemsById[item.PowerId] = item;
        }

        if (purchasePopup == null || homeUIBinder == null || pricingCatalog == null)
            Debug.LogWarning("Weapon Power Shop has one or more missing serialized references.", this);
    }
}
