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
    public CurrencyPackPurchaseType PurchaseType => purchaseType;
    public int GemsCost => gemsCost;
    public int GemsGranted => gemsGranted;
    public int CashGranted => cashGranted;
    public string RealMoneyDisplayPrice => realMoneyDisplayPrice;
    public string CardCostText => cardCostText;
    public string DisplayProductName => displayProductName;
    public string StoreProductId => storeProductId;
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
            costText.text = cardCostText;

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
        if (purchaseType == CurrencyPackPurchaseType.GemsToCash)
        {
            return "You're about to spend " + FormatAmount(gemsCost) +
                   " Gems to buy " + FormatAmount(cashGranted) +
                   " Cash. Are you sure?";
        }

        int amount = purchaseType == CurrencyPackPurchaseType.RealMoneyToGems
            ? gemsGranted
            : cashGranted;
        string currencyName = purchaseType == CurrencyPackPurchaseType.RealMoneyToGems
            ? "Gems"
            : "Cash";

        return "You're about to spend " + realMoneyDisplayPrice +
               " to buy " + FormatAmount(amount) + " " +
               currencyName + ". Are you sure?";
    }

    private void RequestPurchase()
    {
        if (isProcessing || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private static string FormatAmount(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
