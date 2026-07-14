using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Neutral presentation/callback host for the permanent scene-authored purchase
/// popup shared by Ability and Sword Shop controllers.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponPowerPurchasePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private TMP_Text yesButtonLabel;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text noButtonLabel;

    private Action confirmCallback;
    private Action cancelCallback;
    private Action closeCallback;

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

        ClearCallbacks();
    }

    public void ShowConfirmation(
        string title,
        string message,
        Action onConfirm,
        Action onCancel)
    {
        ClearCallbacks();
        confirmCallback = onConfirm;
        cancelCallback = onCancel;
        ResetButtonState();

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);
    }

    public void ShowInformation(string title, string message, Action onClose = null)
    {
        ClearCallbacks();
        closeCallback = onClose;

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
        {
            ClearCallbacks();
            ResetButtonState();
        }
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

        Action callback = confirmCallback;
        ClearCallbacks();

        if (callback != null)
            callback.Invoke();
        else
            Close();
    }

    private void HandleNoOrOk()
    {
        Action callback = cancelCallback ?? closeCallback;
        Close();

        if (callback != null)
            callback.Invoke();
    }

    private void ClearCallbacks()
    {
        confirmCallback = null;
        cancelCallback = null;
        closeCallback = null;
    }
}
