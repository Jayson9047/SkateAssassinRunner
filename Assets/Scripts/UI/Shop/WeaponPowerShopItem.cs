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
    public ShopPaymentType PaymentType { get { ShopPricingCatalog.AbilityPrice p; return TryGetPrice(out p) ? p.paymentType : ShopPaymentType.RealMoney; } }
    public int PriceCost { get { ShopPricingCatalog.AbilityPrice p; return TryGetPrice(out p) ? p.cost : 0; } }
    public string RealMoneyDisplayPrice { get { ShopPricingCatalog.AbilityPrice p; return TryGetPrice(out p) ? p.realMoneyPrice : string.Empty; } }
    public string StoreProductId { get { ShopPricingCatalog.AbilityPrice p; return TryGetPrice(out p) ? p.storeProductId : string.Empty; } }
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
            gemIcon.SetActive(!isOwned && PaymentType == ShopPaymentType.Gems);

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
        ShopPricingCatalog.AbilityPrice price;
        return TryGetPrice(out price)
            ? ShopPricingCatalog.FormatCardPrice(price.paymentType, price.cost, price.realMoneyPrice)
            : "UNAVAILABLE";
    }

    public string BuildConfirmationMessage()
    {
        ShopPricingCatalog.AbilityPrice price;
        if (!TryGetPrice(out price)) return "This product is not configured in ShopPricingCatalog.";
        return "You're about to spend " +
               ShopPricingCatalog.FormatConfirmationPrice(price.paymentType, price.cost, price.realMoneyPrice) +
               " to buy " + ProductDisplayName + ". Are you sure?";
    }

    private void RequestPurchase()
    {
        if (isOwned || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private bool TryGetPrice(out ShopPricingCatalog.AbilityPrice price)
    {
        price = null;
        return controller != null && controller.TryGetPrice(powerId, out price);
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
