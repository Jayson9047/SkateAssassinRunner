using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Coordinates repeatable Currency Pack confirmations and ES3 Gem/Cash transactions.
/// It owns no Inventory, ownership, or equipment state.
/// </summary>
[DisallowMultipleComponent]
public sealed class CurrencyPackShopController : MonoBehaviour
{
    private const string TotalGemsKey = "TotalGems";
    private const string TotalCashKey = "TotalCash";

    [SerializeField] private CurrencyPackShopItem[] items;
    [SerializeField] private ShopPricingCatalog pricingCatalog;
    [SerializeField] private WeaponPowerPurchasePopup purchasePopup;
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly Dictionary<CurrencyPackProductId, CurrencyPackShopItem> itemsById =
        new Dictionary<CurrencyPackProductId, CurrencyPackShopItem>();

    private CurrencyPackShopItem pendingItem;
    private bool purchaseProcessing;
    private bool mappingBuilt;

    public ShopPricingCatalog PricingCatalog => pricingCatalog;
    public bool TryGetPrice(CurrencyPackProductId id, out ShopPricingCatalog.CurrencyPackPrice price)
    {
        price = null;
        return pricingCatalog != null && pricingCatalog.TryGet(id, out price);
    }

    private void OnEnable()
    {
        BuildItemMapping();
        ResetTransaction(false);
        RefreshItems();

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();
    }

    private void OnDisable()
    {
        bool hadCurrencyTransaction = pendingItem != null || purchaseProcessing;
        ResetTransaction(hadCurrencyTransaction);
    }

    public void RefreshItems()
    {
        BuildItemMapping();

        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].RefreshPresentation();
        }
    }

    public void RequestPurchase(CurrencyPackShopItem item)
    {
        BuildItemMapping();

        CurrencyPackShopItem configuredItem;
        if (item == null ||
            purchaseProcessing ||
            pendingItem != null ||
            !itemsById.TryGetValue(item.ProductId, out configuredItem) ||
            configuredItem != item ||
            !IsValidConfiguration(item))
        {
            return;
        }

        pendingItem = item;
        item.SetProcessing(true);

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
            Debug.LogWarning("Currency Pack Shop is missing the shared purchase popup.", this);
            ResetTransaction(false);
        }
    }

    private void ConfirmPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        CurrencyPackShopItem item = pendingItem;
        CurrencyPackShopItem configuredItem;
        if (item == null ||
            !itemsById.TryGetValue(item.ProductId, out configuredItem) ||
            configuredItem != item ||
            !item.IsProcessing ||
            !IsValidConfiguration(item))
        {
            ResetTransaction(true);
            return;
        }

        purchaseProcessing = true;

        switch (item.PurchaseType)
        {
            case CurrencyPackPurchaseType.GemsToCash:
                CompleteGemsToCashPurchase(item);
                break;
            case CurrencyPackPurchaseType.RealMoneyToGems:
            case CurrencyPackPurchaseType.RealMoneyToCash:
                CompleteRealMoneyPlaceholderPurchase(item);
                break;
            default:
                ResetTransaction(true);
                break;
        }
    }

    private void CancelPendingPurchase()
    {
        if (purchaseProcessing)
            return;

        ResetTransaction(true);
    }

    private void CompleteGemsToCashPurchase(CurrencyPackShopItem item)
    {
        if (item.PurchaseType != CurrencyPackPurchaseType.GemsToCash ||
            item.GemsCost <= 0 ||
            item.CashGranted <= 0)
        {
            ResetTransaction(true);
            return;
        }

        float currentGems = Mathf.Max(0f, ES3.Load<float>(TotalGemsKey, 0f));
        float currentCash = Mathf.Max(0f, ES3.Load<float>(TotalCashKey, 0f));

        if (currentGems + 0.0001f < item.GemsCost)
        {
            int missingGems = Mathf.CeilToInt(item.GemsCost - currentGems);
            string message = "You need " + FormatAmount(missingGems) +
                             " more Gems to buy " + FormatAmount(item.CashGranted) +
                             " Cash.";

            ResetTransaction(false);

            if (purchasePopup != null)
            {
                purchasePopup.ShowInformation(
                    "NOT ENOUGH GEMS",
                    message,
                    HandleInformationClosed);
            }

            return;
        }

        float newGems = Mathf.Max(0f, currentGems - item.GemsCost);
        float newCash = currentCash + item.CashGranted;

        if (!TrySaveBothBalances(currentGems, currentCash, newGems, newCash))
        {
            ResetTransaction(true);
            return;
        }

        SkateRunnerAudioManager.PlayPurchaseSuccess();
        if (homeUIBinder != null)
            homeUIBinder.AnimateBalances(currentCash, newCash, currentGems, newGems);

        ResetTransaction(true);
    }

    private void CompleteRealMoneyPlaceholderPurchase(CurrencyPackShopItem item)
    {
        bool grantsGems =
            item.PurchaseType == CurrencyPackPurchaseType.RealMoneyToGems &&
            item.GemsGranted > 0;
        bool grantsCash =
            item.PurchaseType == CurrencyPackPurchaseType.RealMoneyToCash &&
            item.CashGranted > 0;

        if (!grantsGems && !grantsCash)
        {
            ResetTransaction(true);
            return;
        }

        if (!ShopRealMoneyPurchaseBridge.IsPlaceholderPurchaseApproved(item.StoreProductId))
        {
            ResetTransaction(true);
            return;
        }

        string balanceKey = grantsGems ? TotalGemsKey : TotalCashKey;
        int amountGranted = grantsGems ? item.GemsGranted : item.CashGranted;
        float previousBalance = Mathf.Max(0f, ES3.Load<float>(balanceKey, 0f));

        try
        {
            ES3.Save(balanceKey, previousBalance + amountGranted);
        }
        catch (Exception exception)
        {
            TryRestoreSingleBalance(balanceKey, previousBalance);
            Debug.LogError(
                "Currency Pack real-money placeholder grant failed for " +
                item.ProductId + ". The previous balance was restored when possible. " +
                exception.Message,
                this);
            ResetTransaction(true);
            return;
        }

        SkateRunnerAudioManager.PlayPurchaseSuccess();
        if (homeUIBinder != null)
        {
            float finalCash = ES3.Load<float>(TotalCashKey, 0f);
            float finalGems = ES3.Load<float>(TotalGemsKey, 0f);
            homeUIBinder.AnimateBalances(
                grantsCash ? previousBalance : finalCash, finalCash,
                grantsGems ? previousBalance : finalGems, finalGems);
        }

        ResetTransaction(true);
    }

    private bool TrySaveBothBalances(
        float previousGems,
        float previousCash,
        float newGems,
        float newCash)
    {
        try
        {
            ES3.Save(TotalGemsKey, newGems);
            ES3.Save(TotalCashKey, newCash);
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                ES3.Save(TotalGemsKey, previousGems);
                ES3.Save(TotalCashKey, previousCash);
            }
            catch
            {
                // The useful error below covers both the transaction and best-effort restore.
            }

            Debug.LogError(
                "Currency Pack Gem-to-Cash save failed. Previous Gem and Cash values " +
                "were restored when possible. " + exception.Message,
                this);
            return false;
        }
    }

    private static void TryRestoreSingleBalance(string balanceKey, float previousBalance)
    {
        try
        {
            ES3.Save(balanceKey, previousBalance);
        }
        catch
        {
            // The caller logs one consolidated transaction error.
        }
    }

    private void HandleInformationClosed()
    {
        ResetTransaction(false);
    }

    private void ResetTransaction(bool closePopup)
    {
        CurrencyPackShopItem item = pendingItem;
        pendingItem = null;
        purchaseProcessing = false;

        if (item != null)
            item.SetProcessing(false);

        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i] != item)
                    items[i].SetProcessing(false);
            }
        }

        if (closePopup && purchasePopup != null)
            purchasePopup.Close();
    }

    private void BuildItemMapping()
    {
        if (mappingBuilt)
            return;

        mappingBuilt = true;
        itemsById.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                CurrencyPackShopItem item = items[i];
                if (item == null)
                    continue;

                if (itemsById.ContainsKey(item.ProductId))
                {
                    Debug.LogWarning(
                        "Duplicate Currency Pack product mapping for " +
                        item.ProductId + ". The duplicate card is ignored.",
                        item);
                    continue;
                }

                itemsById.Add(item.ProductId, item);
            }
        }

        if (purchasePopup == null || homeUIBinder == null || pricingCatalog == null)
            Debug.LogWarning("Currency Pack Shop has one or more missing serialized references.", this);
    }

    private static bool IsValidConfiguration(CurrencyPackShopItem item)
    {
        if (item == null ||
            !Enum.IsDefined(typeof(CurrencyPackProductId), item.ProductId) ||
            !Enum.IsDefined(typeof(CurrencyPackPurchaseType), item.PurchaseType))
        {
            return false;
        }

        switch (item.PurchaseType)
        {
            case CurrencyPackPurchaseType.GemsToCash:
                return item.GemsCost > 0 &&
                       item.CashGranted > 0 &&
                       item.GemsGranted == 0;

            case CurrencyPackPurchaseType.RealMoneyToGems:
                return item.GemsGranted > 0 &&
                       item.CashGranted == 0 &&
                       item.GemsCost == 0 &&
                       !string.IsNullOrEmpty(item.RealMoneyDisplayPrice);

            case CurrencyPackPurchaseType.RealMoneyToCash:
                return item.CashGranted > 0 &&
                       item.GemsGranted == 0 &&
                       item.GemsCost == 0 &&
                       !string.IsNullOrEmpty(item.RealMoneyDisplayPrice);

            default:
                return false;
        }
    }

    private static string FormatAmount(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
