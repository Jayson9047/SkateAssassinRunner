using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CurrencyPackProductId
{
    FeaturedGem13000 = 0,
    FeaturedCash110000 = 1,
    Cash200For5Gems = 2,
    Cash4000For100Gems = 3,
    Cash20000For500Gems = 4,
    Cash40000For1000Gems = 5,
    Cash60000For1500Gems = 6,
    Gems500 = 7,
    Gems1050 = 8,
    Gems1600 = 9,
    Gems2750 = 10,
    Gems5750 = 11
}

public enum CurrencyPackPurchaseType
{
    RealMoneyToGems = 0,
    RealMoneyToCash = 1,
    GemsToCash = 2
}

/// <summary>
/// Serialized identity/configuration and full-card click forwarding for one repeatable currency product.
/// </summary>
[DisallowMultipleComponent]
public sealed class CurrencyPackShopItem : MonoBehaviour
{
    [Header("Product")]
    [SerializeField] private CurrencyPackProductId productId;
    [SerializeField] private CurrencyPackPurchaseType purchaseType;
    [SerializeField] private int gemsCost;
    [SerializeField] private int gemsGranted;
    [SerializeField] private int cashGranted;
    [SerializeField] private string realMoneyDisplayPrice;
    [SerializeField] private string cardCostText;
    [SerializeField] private string displayProductName;

    [Header("Future Store Integration")]
    [SerializeField] private string storeProductId;

    [Header("Scene References")]
    [SerializeField] private CurrencyPackShopController controller;
    [SerializeField] private Button fullCardButton;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Temporary Processing Presentation")]
    [SerializeField, Range(0.1f, 1f)] private float normalAlpha = 1f;
    [SerializeField, Range(0.1f, 1f)] private float processingAlpha = 0.82f;

    private bool isProcessing;

    public CurrencyPackProductId ProductId => productId;
    public CurrencyPackPurchaseType PurchaseType
    {
        get
        {
            ShopPricingCatalog.CurrencyPackPrice p;
            if (!TryGetPrice(out p)) return purchaseType;
            if (p.paymentType == ShopPaymentType.Gems) return CurrencyPackPurchaseType.GemsToCash;
            return p.gemsGranted > 0 ? CurrencyPackPurchaseType.RealMoneyToGems : CurrencyPackPurchaseType.RealMoneyToCash;
        }
    }
    public int GemsCost { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) && p.paymentType == ShopPaymentType.Gems ? p.cost : 0; } }
    public int GemsGranted { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? p.gemsGranted : 0; } }
    public int CashGranted { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? p.cashGranted : 0; } }
    public string RealMoneyDisplayPrice { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? p.realMoneyPrice : string.Empty; } }
    public string CardCostText { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? ShopPricingCatalog.FormatCardPrice(p.paymentType, p.cost, p.realMoneyPrice) : "UNAVAILABLE"; } }
    public string DisplayProductName { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? p.displayName : string.Empty; } }
    public string StoreProductId { get { ShopPricingCatalog.CurrencyPackPrice p; return TryGetPrice(out p) ? p.storeProductId : string.Empty; } }
    public bool IsProcessing => isProcessing;

    private void OnEnable()
    {
        if (fullCardButton != null)
        {
            fullCardButton.onClick.RemoveListener(RequestPurchase);
            fullCardButton.onClick.AddListener(RequestPurchase);
        }

        RefreshPresentation();
    }

    private void OnDisable()
    {
        if (fullCardButton != null)
            fullCardButton.onClick.RemoveListener(RequestPurchase);

        SetProcessing(false);
    }

    public void RefreshPresentation()
    {
        if (costText != null)
            costText.text = CardCostText;

        SetProcessing(false);
    }

    public void SetProcessing(bool processing)
    {
        isProcessing = processing;

        if (fullCardButton != null)
            fullCardButton.interactable = !processing;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = processing ? processingAlpha : normalAlpha;
            canvasGroup.interactable = !processing;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public string BuildConfirmationMessage()
    {
        CurrencyPackPurchaseType configuredType = PurchaseType;
        if (configuredType == CurrencyPackPurchaseType.GemsToCash)
        {
            return "You're about to spend " + FormatAmount(GemsCost) +
                   " Gems to buy " + FormatAmount(CashGranted) +
                   " Cash. Are you sure?";
        }

        int amount = configuredType == CurrencyPackPurchaseType.RealMoneyToGems
            ? GemsGranted
            : CashGranted;
        string currencyName = configuredType == CurrencyPackPurchaseType.RealMoneyToGems
            ? "Gems"
            : "Cash";

        return "You're about to spend " + RealMoneyDisplayPrice +
               " to buy " + FormatAmount(amount) + " " +
               currencyName + ". Are you sure?";
    }

    private void RequestPurchase()
    {
        if (isProcessing || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private bool TryGetPrice(out ShopPricingCatalog.CurrencyPackPrice price)
    {
        price = null;
        return controller != null && controller.TryGetPrice(productId, out price);
    }

    private static string FormatAmount(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
