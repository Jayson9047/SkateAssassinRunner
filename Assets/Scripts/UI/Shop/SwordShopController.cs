using System.Collections.Generic;
using UnityEngine;

/// <summary>Coordinates Sword purchases, balances, persistent ownership, and the shared purchase popup.</summary>
[DisallowMultipleComponent]
public sealed class SwordShopController : MonoBehaviour
{
    private const string TotalGemsKey = "TotalGems";
    private const string TotalCashKey = "TotalCash";

    [SerializeField] private SwordShopItem[] items;
    [SerializeField] private ShopPricingCatalog pricingCatalog;
    [SerializeField] private WeaponPowerPurchasePopup purchasePopup;
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly Dictionary<SwordId, SwordShopItem> itemsById =
        new Dictionary<SwordId, SwordShopItem>();

    private SwordShopItem pendingItem;
    private bool purchaseProcessing;
    private bool mappingBuilt;

    public ShopPricingCatalog PricingCatalog => pricingCatalog;
    public bool TryGetPrice(SwordId id, out ShopPricingCatalog.SwordPrice price)
    {
        price = null;
        return pricingCatalog != null && pricingCatalog.TryGet(id, out price);
    }

    private void OnEnable()
    {
        BuildItemMapping();
        pendingItem = null;
        purchaseProcessing = false;
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

    public void RequestPurchase(SwordShopItem item)
    {
        BuildItemMapping();

        SwordShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.SwordId, out configuredItem) ||
            configuredItem != item ||
            item.SwordId == SwordId.Katana ||
            !SwordOwnershipSave.IsValidSwordId(item.SwordId))
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
            Debug.LogWarning("Sword Shop is missing its shared purchase popup reference.", this);
            pendingItem = null;
        }
    }

    private void ConfirmPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        SwordShopItem item = pendingItem;
        pendingItem = null;

        SwordShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.SwordId, out configuredItem) ||
            configuredItem != item ||
            !SwordOwnershipSave.IsValidSwordId(item.SwordId) ||
            item.SwordId == SwordId.Katana)
        {
            CloseAndResetPopup();
            return;
        }

        if (SwordOwnershipSave.IsOwned(item.SwordId))
        {
            item.RefreshOwnedState();
            CloseAndResetPopup();
            return;
        }

        purchaseProcessing = true;

        switch (item.PaymentType)
        {
            case ShopPaymentType.Gems:
                CompleteCurrencyPurchase(item, TotalGemsKey, item.PriceCost, "Gems");
                break;
            case ShopPaymentType.Cash:
                CompleteCurrencyPurchase(item, TotalCashKey, item.PriceCost, "Cash");
                break;
            default:
                CompleteRealMoneyPlaceholderPurchase(item);
                break;
        }
    }

    private void CancelPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        CloseAndResetPopup();
    }

    private void CompleteCurrencyPurchase(SwordShopItem item, string balanceKey, int cost, string currencyName)
    {
        bool typeMatches = (currencyName == "Gems" && item.PaymentType == ShopPaymentType.Gems) ||
                           (currencyName == "Cash" && item.PaymentType == ShopPaymentType.Cash);
        if (!typeMatches || cost <= 0)
        {
            CloseAndResetPopup();
            return;
        }

        float currentBalance = Mathf.Max(0f, ES3.Load(balanceKey, 0f));
        if (currentBalance + 0.0001f < cost)
        {
            int missingAmount = Mathf.CeilToInt(cost - currentBalance);
            purchaseProcessing = false;

            if (purchasePopup != null)
            {
                purchasePopup.ShowInformation(
                    currencyName == "Gems" ? "NOT ENOUGH GEMS" : "NOT ENOUGH CASH",
                    "You need " + missingAmount + " more " + currencyName +
                    " to buy " + item.ProductDisplayName + ".");
            }

            return;
        }

        float newBalance = Mathf.Max(0f, currentBalance - cost);
        ES3.Save(balanceKey, newBalance);

        if (!SwordOwnershipSave.Grant(item.SwordId))
        {
            ES3.Save(balanceKey, currentBalance);
            CloseAndResetPopup();
            return;
        }

        InventoryNewItemNotifications.RegisterSword(item.SwordId);

        SkateRunnerAudioManager.PlayPurchaseSuccess();
        RefreshItems();

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();

        CloseAndResetPopup();
    }

    private void CompleteRealMoneyPlaceholderPurchase(SwordShopItem item)
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
        if (SwordOwnershipSave.Grant(item.SwordId))
        {
            InventoryNewItemNotifications.RegisterSword(item.SwordId);
            SkateRunnerAudioManager.PlayPurchaseSuccess();
        }

        RefreshItems();
        CloseAndResetPopup();
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
            SwordShopItem item = items[i];
            if (item == null)
                continue;

            if (itemsById.ContainsKey(item.SwordId))
                Debug.LogWarning("Duplicate Sword Shop mapping for " + item.SwordId + ".", item);

            itemsById[item.SwordId] = item;
        }

        if (purchasePopup == null || homeUIBinder == null || pricingCatalog == null)
            Debug.LogWarning("Sword Shop has one or more missing serialized references.", this);
    }
}
