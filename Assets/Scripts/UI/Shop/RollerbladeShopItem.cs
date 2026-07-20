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
    public RollerbladePurchaseType PurchaseType => purchaseType;
    public int GemCost => gemCost;
    public int CashCost => cashCost;
    public string RealMoneyConfirmationPrice => realMoneyConfirmationPrice;
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
                !isOwned && purchaseType != RollerbladePurchaseType.RealMoneyPlaceholder);
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
        switch (purchaseType)
        {
            case RollerbladePurchaseType.Gems:
                return gemCost.ToString();
            case RollerbladePurchaseType.Cash:
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
            case RollerbladePurchaseType.Gems:
                price = gemCost + " Gems";
                break;
            case RollerbladePurchaseType.Cash:
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
