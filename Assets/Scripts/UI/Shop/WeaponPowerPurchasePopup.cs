using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation and button forwarding for the permanent scene-authored purchase
/// popup. Purchase validation remains in WeaponPowerShopController.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerPurchasePopup : MonoBehaviour
{
    [SerializeField] private WeaponPowerShopController controller;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private TMP_Text yesButtonLabel;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text noButtonLabel;

    public bool IsOpen => gameObject.activeSelf;

    private void OnEnable()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(HandleYes);
            yesButton.onClick.AddListener(HandleYes);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(HandleNoOrOk);
            noButton.onClick.AddListener(HandleNoOrOk);
        }
    }

    private void OnDisable()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(HandleYes);

        if (noButton != null)
            noButton.onClick.RemoveListener(HandleNoOrOk);

        if (controller != null)
            controller.NotifyPopupClosed();
    }

    public void ShowConfirmation(string title, string message)
    {
        ResetButtonState();

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);
    }

    public void ShowInformation(string title, string message)
    {
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        if (yesButton != null)
            yesButton.gameObject.SetActive(false);

        if (noButton != null)
        {
            noButton.gameObject.SetActive(true);
            noButton.interactable = true;
        }

        if (noButtonLabel != null)
            noButtonLabel.text = "OK";

        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        else
            ResetButtonState();
    }

    private void ResetButtonState()
    {
        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(true);
            yesButton.interactable = true;
        }

        if (yesButtonLabel != null)
            yesButtonLabel.text = "YES";

        if (noButton != null)
        {
            noButton.gameObject.SetActive(true);
            noButton.interactable = true;
        }

        if (noButtonLabel != null)
            noButtonLabel.text = "NO";
    }

    private void HandleYes()
    {
        if (yesButton != null)
            yesButton.interactable = false;

        if (controller != null)
            controller.ConfirmPendingPurchase();
        else
            Close();
    }

    private void HandleNoOrOk()
    {
        if (controller != null)
            controller.CancelPendingPurchase();
        else
            Close();
    }
}
