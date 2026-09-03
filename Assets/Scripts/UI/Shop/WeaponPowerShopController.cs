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
    [SerializeField] private WeaponPowerPurchasePopup purchasePopup;
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly Dictionary<WeaponPowerId, WeaponPowerShopItem> itemsById =
        new Dictionary<WeaponPowerId, WeaponPowerShopItem>();

    private WeaponPowerShopItem pendingItem;
    private bool purchaseProcessing;
    private bool mappingBuilt;

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

        if (item.PurchaseType == WeaponPowerPurchaseType.Gems)
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
        int cost = item.GemCost;
        if (cost <= 0 || item.PurchaseType != WeaponPowerPurchaseType.Gems)
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

        SkateRunnerAudioManager.PlayPurchaseSuccess();
        RefreshItems();

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();

        CloseAndResetPopup();
    }

    private void CompleteRealMoneyPlaceholderPurchase(WeaponPowerShopItem item)
    {
        if (item.PurchaseType != WeaponPowerPurchaseType.RealMoneyPlaceholder)
        {
            CloseAndResetPopup();
            return;
        }

        // TODO: Add monetization here.
        // Replace this temporary grant with the Google Play/Google Pay purchase flow.
        // Grant Magic ownership only after a verified successful purchase callback.
        if (WeaponPowerOwnershipSave.Grant(item.PowerId))
            SkateRunnerAudioManager.PlayPurchaseSuccess();

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
            WeaponPowerShopItem item = items[i];
            if (item == null)
                continue;

            if (itemsById.ContainsKey(item.PowerId))
                Debug.LogWarning("Duplicate Weapon Power Shop mapping for " + item.PowerId + ".", item);

            itemsById[item.PowerId] = item;
        }

        if (purchasePopup == null || homeUIBinder == null)
            Debug.LogWarning("Weapon Power Shop has one or more missing serialized references.", this);
    }
}
