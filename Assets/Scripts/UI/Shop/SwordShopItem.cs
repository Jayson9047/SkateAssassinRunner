using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public enum SwordPurchaseType
{
    Gems = 0,
    Cash = 1,
    RealMoneyPlaceholder = 2
}

/// <summary>Explicit identity, configured price, click forwarding, and owned state for one Sword Shop card.</summary>
[DisallowMultipleComponent]
public sealed class SwordShopItem : MonoBehaviour
{
    [Header("Product")]
    [SerializeField] private SwordId swordId = SwordId.Bloodreaver;
    [SerializeField] private SwordPurchaseType purchaseType = SwordPurchaseType.Gems;
    [SerializeField] private int gemCost;
    [SerializeField] private int cashCost;
    [SerializeField] private string realMoneyConfirmationPrice = "$1.99 USD";
    [SerializeField] private string realMoneyCardPrice = "$1.99";

    [Header("Scene References")]
    [SerializeField] private SwordShopController controller;
    [SerializeField] private Button clickButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject currencyIcon;
    [SerializeField] private CanvasGroup cardCanvasGroup;

    [Header("Owned Presentation")]
    [SerializeField, Range(0.1f, 1f)] private float normalAlpha = 1f;
    [SerializeField, Range(0.1f, 1f)] private float ownedAlpha = 0.55f;

    private bool isOwned;

    public SwordId SwordId => swordId;
    public ShopPaymentType PaymentType { get { ShopPricingCatalog.SwordPrice p; return TryGetPrice(out p) ? p.paymentType : ShopPaymentType.RealMoney; } }
    public int PriceCost { get { ShopPricingCatalog.SwordPrice p; return TryGetPrice(out p) ? p.cost : 0; } }
    public string StoreProductId { get { ShopPricingCatalog.SwordPrice p; return TryGetPrice(out p) ? p.storeProductId : string.Empty; } }
    public bool IsOwned => isOwned;
    public string ProductDisplayName => SkateLocalization.GetSwordDisplayName(swordId);

    private void OnEnable()
    {
        SkateLocalization.LocaleChanged += OnLocaleChanged;
        if (clickButton == null)
            return;

        clickButton.onClick.RemoveListener(RequestPurchase);
        clickButton.onClick.AddListener(RequestPurchase);
    }

    private void OnDisable()
    {
        SkateLocalization.LocaleChanged -= OnLocaleChanged;
        if (clickButton != null)
            clickButton.onClick.RemoveListener(RequestPurchase);
    }

    public void RefreshOwnedState()
    {
        isOwned = SwordOwnershipSave.IsOwned(swordId);

        if (costText != null)
            costText.text = isOwned ? SkateLocalization.Get("Common", "common.owned") : GetConfiguredPriceText();

        if (currencyIcon != null)
            currencyIcon.SetActive(!isOwned && PaymentType != ShopPaymentType.RealMoney);

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
        ShopPricingCatalog.SwordPrice price;
        return TryGetPrice(out price)
            ? ShopPricingCatalog.FormatCardPrice(price.paymentType, price.cost, price.realMoneyPrice)
            : SkateLocalization.Get("Shop", "shop.unavailable");
    }

    public string BuildConfirmationMessage()
    {
        ShopPricingCatalog.SwordPrice price;
        if (!TryGetPrice(out price)) return SkateLocalization.Get("Shop", "shop.unavailable");
        return SkateLocalization.BuildShopConfirmation(price.paymentType, price.cost, price.realMoneyPrice, ProductDisplayName);
    }

    private void RequestPurchase()
    {
        if (isOwned || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private bool TryGetPrice(out ShopPricingCatalog.SwordPrice price)
    {
        price = null;
        return controller != null && controller.TryGetPrice(swordId, out price);
    }

    private void OnLocaleChanged(Locale locale) => RefreshOwnedState();
}
