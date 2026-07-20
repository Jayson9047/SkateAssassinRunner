using System.Collections.Generic;
using UnityEngine;

/// <summary>Coordinates Rollerblade purchases, balances, persistent ownership, and the shared popup.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeShopController : MonoBehaviour
{
    private const string TotalGemsKey = "TotalGems";
    private const string TotalCashKey = "TotalCash";

    [SerializeField] private RollerbladeShopItem[] items;
    [SerializeField] private WeaponPowerPurchasePopup purchasePopup;
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly Dictionary<RollerbladeId, RollerbladeShopItem> itemsById =
        new Dictionary<RollerbladeId, RollerbladeShopItem>();

    private RollerbladeShopItem pendingItem;
    private bool purchaseProcessing;
    private bool mappingBuilt;

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

    public void RequestPurchase(RollerbladeShopItem item)
    {
        BuildItemMapping();

        RollerbladeShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.RollerbladeId, out configuredItem) ||
            configuredItem != item ||
            item.RollerbladeId == RollerbladeId.Default ||
            !RollerbladeOwnershipSave.IsValidRollerbladeId(item.RollerbladeId))
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
            Debug.LogWarning("Rollerblade Shop is missing its shared purchase popup reference.", this);
            pendingItem = null;
        }
    }

    private void ConfirmPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        RollerbladeShopItem item = pendingItem;
        pendingItem = null;

        RollerbladeShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.RollerbladeId, out configuredItem) ||
            configuredItem != item ||
            !RollerbladeOwnershipSave.IsValidRollerbladeId(item.RollerbladeId) ||
            item.RollerbladeId == RollerbladeId.Default)
        {
            CloseAndResetPopup();
            return;
        }

        if (RollerbladeOwnershipSave.IsOwned(item.RollerbladeId))
        {
            item.RefreshOwnedState();
            CloseAndResetPopup();
            return;
        }

        purchaseProcessing = true;

        switch (item.PurchaseType)
        {
            case RollerbladePurchaseType.Gems:
                CompleteCurrencyPurchase(item, TotalGemsKey, item.GemCost, "Gems");
                break;
            case RollerbladePurchaseType.Cash:
                CompleteCurrencyPurchase(item, TotalCashKey, item.CashCost, "Cash");
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

    private void CompleteCurrencyPurchase(
        RollerbladeShopItem item,
        string balanceKey,
        int cost,
        string currencyName)
    {
        bool typeMatches =
            (currencyName == "Gems" && item.PurchaseType == RollerbladePurchaseType.Gems) ||
            (currencyName == "Cash" && item.PurchaseType == RollerbladePurchaseType.Cash);
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

        if (!RollerbladeOwnershipSave.Grant(item.RollerbladeId))
        {
            ES3.Save(balanceKey, currentBalance);
            CloseAndResetPopup();
            return;
        }

        RefreshItems();

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();

        CloseAndResetPopup();
    }

    private void CompleteRealMoneyPlaceholderPurchase(RollerbladeShopItem item)
    {
        if (item.PurchaseType != RollerbladePurchaseType.RealMoneyPlaceholder ||
            item.RollerbladeId != RollerbladeId.InfernoDrift)
        {
            CloseAndResetPopup();
            return;
        }

        // TODO: Add monetization here.
        // Replace this temporary grant with the real Google Play purchase flow.
        // Grant Rollerblade ownership only after a verified successful purchase callback.
        RollerbladeOwnershipSave.Grant(RollerbladeId.InfernoDrift);

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
            RollerbladeShopItem item = items[i];
            if (item == null)
                continue;

            if (itemsById.ContainsKey(item.RollerbladeId))
            {
                Debug.LogWarning(
                    "Duplicate Rollerblade Shop mapping for " + item.RollerbladeId + ".",
                    item);
            }

            itemsById[item.RollerbladeId] = item;
        }

        if (purchasePopup == null || homeUIBinder == null)
            Debug.LogWarning("Rollerblade Shop has one or more missing serialized references.", this);
    }
}
