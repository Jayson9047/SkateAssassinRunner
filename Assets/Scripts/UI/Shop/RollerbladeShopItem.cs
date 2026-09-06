using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RollerbladePurchaseType
{
    Gems = 0,
    Cash = 1,
    RealMoneyPlaceholder = 2
}

/// <summary>Explicit identity, configured price, click forwarding, and owned state for one Shop card.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeShopItem : MonoBehaviour
{
    [Header("Product")]
    [SerializeField] private RollerbladeId rollerbladeId = RollerbladeId.UrbanRush;
    [SerializeField] private RollerbladePurchaseType purchaseType = RollerbladePurchaseType.Gems;
    [SerializeField] private int gemCost;
    [SerializeField] private int cashCost;
    [SerializeField] private string realMoneyConfirmationPrice = "$1.99 USD";
    [SerializeField] private string realMoneyCardPrice = "$1.99";

    [Header("Scene References")]
    [SerializeField] private RollerbladeShopController controller;
    [SerializeField] private Button clickButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject currencyIcon;
    [SerializeField] private CanvasGroup cardCanvasGroup;

    [Header("Owned Presentation")]
    [SerializeField, Range(0.1f, 1f)] private float normalAlpha = 1f;
    [SerializeField, Range(0.1f, 1f)] private float ownedAlpha = 0.55f;

    private bool isOwned;

    public RollerbladeId RollerbladeId => rollerbladeId;
    public ShopPaymentType PaymentType { get { ShopPricingCatalog.RollerbladePrice p; return TryGetPrice(out p) ? p.paymentType : ShopPaymentType.RealMoney; } }
    public int PriceCost { get { ShopPricingCatalog.RollerbladePrice p; return TryGetPrice(out p) ? p.cost : 0; } }
    public string StoreProductId { get { ShopPricingCatalog.RollerbladePrice p; return TryGetPrice(out p) ? p.storeProductId : string.Empty; } }
    public bool IsOwned => isOwned;
    public string ProductDisplayName => GetDisplayName(rollerbladeId);

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
        isOwned = RollerbladeOwnershipSave.IsOwned(rollerbladeId);

        if (costText != null)
            costText.text = isOwned ? "Owned" : GetConfiguredPriceText();

        if (currencyIcon != null)
        {
            currencyIcon.SetActive(
                !isOwned && PaymentType != ShopPaymentType.RealMoney);
        }

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
        ShopPricingCatalog.RollerbladePrice price;
        return TryGetPrice(out price)
            ? ShopPricingCatalog.FormatCardPrice(price.paymentType, price.cost, price.realMoneyPrice)
            : "UNAVAILABLE";
    }

    public string BuildConfirmationMessage()
    {
        ShopPricingCatalog.RollerbladePrice price;
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

    private bool TryGetPrice(out ShopPricingCatalog.RollerbladePrice price)
    {
        price = null;
        return controller != null && controller.TryGetPrice(rollerbladeId, out price);
    }

    private static string GetDisplayName(RollerbladeId id)
    {
        switch (id)
        {
            case RollerbladeId.UrbanRush: return "Urban Rush";
            case RollerbladeId.NeonVelocity: return "Neon Velocity";
            case RollerbladeId.FrostbiteGlide: return "Frostbite Glide";
            case RollerbladeId.InfernoDrift: return "Inferno Drift";
            case RollerbladeId.CelestialApex: return "Celestial Apex";
            default: return "Default Rollerblades";
        }
    }
}
