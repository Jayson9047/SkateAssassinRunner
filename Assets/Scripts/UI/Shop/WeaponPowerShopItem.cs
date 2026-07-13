using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WeaponPowerPurchaseType
{
    Gems = 0,
    RealMoneyPlaceholder = 1
}

/// <summary>
/// Serialized identity, price, click forwarding, and owned presentation for one
/// existing Weapon Power Shop card.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerShopItem : MonoBehaviour
{
    [Header("Product")]
    [SerializeField] private WeaponPowerId powerId = WeaponPowerId.None;
    [SerializeField] private WeaponPowerPurchaseType purchaseType = WeaponPowerPurchaseType.Gems;
    [SerializeField] private int gemCost;
    [SerializeField] private string realMoneyDisplayPrice = "$0.99 USD";

    [Header("Scene References")]
    [SerializeField] private WeaponPowerShopController controller;
    [SerializeField] private Button clickButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject gemIcon;
    [SerializeField] private CanvasGroup cardCanvasGroup;

    [Header("Owned Presentation")]
    [SerializeField, Range(0.1f, 1f)] private float normalAlpha = 1f;
    [SerializeField, Range(0.1f, 1f)] private float ownedAlpha = 0.55f;

    private bool isOwned;

    public WeaponPowerId PowerId => powerId;
    public WeaponPowerPurchaseType PurchaseType => purchaseType;
    public int GemCost => gemCost;
    public string RealMoneyDisplayPrice => realMoneyDisplayPrice;
    public bool IsOwned => isOwned;
    public string ProductDisplayName => GetPowerDisplayName(powerId) + " Power";

    private void OnEnable()
    {
        if (clickButton == null)
            return;

        clickButton.onClick.RemoveListener(RequestPurchase);
        clickButton.onClick.AddListener(RequestPurchase);
    }

    private void OnDisable()
    {
        if (clickButton != null)
            clickButton.onClick.RemoveListener(RequestPurchase);
    }

    public void RefreshOwnedState()
    {
        isOwned = WeaponPowerOwnershipSave.IsOwned(powerId);

        if (costText != null)
            costText.text = isOwned ? "Owned" : GetConfiguredPriceText();

        if (gemIcon != null)
            gemIcon.SetActive(!isOwned && purchaseType == WeaponPowerPurchaseType.Gems);

        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = isOwned ? ownedAlpha : normalAlpha;
            cardCanvasGroup.interactable = !isOwned;
            cardCanvasGroup.blocksRaycasts = !isOwned;
        }

        if (clickButton != null)
            clickButton.interactable = !isOwned;
    }

    public string GetConfiguredPriceText()
    {
        return purchaseType == WeaponPowerPurchaseType.Gems
            ? gemCost.ToString()
            : realMoneyDisplayPrice;
    }

    public string BuildConfirmationMessage()
    {
        if (purchaseType == WeaponPowerPurchaseType.Gems)
        {
            return "You're about to spend " + gemCost + " Gems to buy " +
                   ProductDisplayName + ". Are you sure?";
        }

        return "You're about to spend " + realMoneyDisplayPrice + " to buy " +
               ProductDisplayName + ". Are you sure?";
    }

    private void RequestPurchase()
    {
        if (isOwned || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private static string GetPowerDisplayName(WeaponPowerId id)
    {
        switch (id)
        {
            case WeaponPowerId.Fire:
                return "Fire";
            case WeaponPowerId.Ice:
                return "Ice";
            case WeaponPowerId.Electricity:
                return "Electricity";
            case WeaponPowerId.Poison:
                return "Poison";
            case WeaponPowerId.Magic:
                return "Magic";
            default:
                return "Default";
        }
    }
}
