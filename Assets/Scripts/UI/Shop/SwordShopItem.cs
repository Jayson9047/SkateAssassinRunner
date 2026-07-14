using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public SwordPurchaseType PurchaseType => purchaseType;
    public int GemCost => gemCost;
    public int CashCost => cashCost;
    public string RealMoneyConfirmationPrice => realMoneyConfirmationPrice;
    public bool IsOwned => isOwned;
    public string ProductDisplayName => GetSwordDisplayName(swordId);

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
        isOwned = SwordOwnershipSave.IsOwned(swordId);

        if (costText != null)
            costText.text = isOwned ? "Owned" : GetConfiguredPriceText();

        if (currencyIcon != null)
            currencyIcon.SetActive(!isOwned && purchaseType != SwordPurchaseType.RealMoneyPlaceholder);

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
        switch (purchaseType)
        {
            case SwordPurchaseType.Gems:
                return gemCost.ToString();
            case SwordPurchaseType.Cash:
                return cashCost.ToString();
            default:
                return realMoneyCardPrice;
        }
    }

    public string BuildConfirmationMessage()
    {
        string price;
        switch (purchaseType)
        {
            case SwordPurchaseType.Gems:
                price = gemCost + " Gems";
                break;
            case SwordPurchaseType.Cash:
                price = cashCost + " Cash";
                break;
            default:
                price = realMoneyConfirmationPrice;
                break;
        }

        return "You're about to spend " + price + " to buy " + ProductDisplayName + ". Are you sure?";
    }

    private void RequestPurchase()
    {
        if (isOwned || controller == null)
            return;

        controller.RequestPurchase(this);
    }

    private static string GetSwordDisplayName(SwordId id)
    {
        switch (id)
        {
            case SwordId.Bloodreaver: return "Bloodreaver";
            case SwordId.Emberguard: return "Emberguard";
            case SwordId.GlacierCipher: return "GlacierCipher";
            case SwordId.Gravebreaker: return "Gravebreaker";
            case SwordId.HellForge: return "HellForge";
            case SwordId.Sunspire: return "Sunspire";
            case SwordId.Wyrmshade: return "Wyrmshade";
            default: return "Katana";
        }
    }
}
