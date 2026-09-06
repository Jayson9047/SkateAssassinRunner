#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkateRunnerDebugToolsController : MonoBehaviour
{
    private enum ConfirmationAction { None, ResetInventory, ResetAllProgress }

    [SerializeField] private GameObject debugButtonRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_InputField cashInput;
    [SerializeField] private TMP_InputField gemsInput;
    [SerializeField] private Button setCashButton;
    [SerializeField] private Button setGemsButton;
    [SerializeField] private Button resetInventoryButton;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationTitle;
    [SerializeField] private TMP_Text confirmationMessage;
    [SerializeField] private Button confirmationYesButton;
    [SerializeField] private Button confirmationNoButton;

    private ConfirmationAction pendingAction;

    private void Awake()
    {
        bool available = SkateRunnerDebugResetService.IsAvailable;
        if (debugButtonRoot != null) debugButtonRoot.SetActive(available);
        if (popupRoot != null) popupRoot.SetActive(false);
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
        if (!available) { enabled = false; return; }

        Bind(openButton, Open);
        Bind(setCashButton, SetCash);
        Bind(setGemsButton, SetGems);
        Bind(resetInventoryButton, RequestInventoryReset);
        Bind(resetProgressButton, RequestProgressReset);
        Bind(closeButton, Close);
        Bind(confirmationYesButton, Confirm);
        Bind(confirmationNoButton, CancelConfirmation);
    }

    private void Open()
    {
        if (!SkateRunnerDebugResetService.IsAvailable) return;
        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        CancelConfirmation();
        SetStatus(string.Empty);
    }

    private void Close()
    {
        CancelConfirmation();
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void SetCash()
    {
        float value;
        if (!SkateRunnerDebugResetService.TrySetCash(cashInput != null ? cashInput.text : string.Empty, out value))
        {
            SetStatus("Enter a valid non-negative number.");
            return;
        }
        SetStatus("Cash set to " + value.ToString("N0") + ".");
    }

    private void SetGems()
    {
        float value;
        if (!SkateRunnerDebugResetService.TrySetGems(gemsInput != null ? gemsInput.text : string.Empty, out value))
        {
            SetStatus("Enter a valid non-negative number.");
            return;
        }
        SetStatus("Gems set to " + value.ToString("N0") + ".");
    }

    private void RequestInventoryReset()
    {
        ShowConfirmation(
            ConfirmationAction.ResetInventory,
            "RESET INVENTORY?",
            "This will remove all purchased Swords, Abilities and Rollerblades.");
    }

    private void RequestProgressReset()
    {
        ShowConfirmation(
            ConfirmationAction.ResetAllProgress,
            "RESET ALL GAME PROGRESS?",
            "This will return local progression to a fresh-player state. Settings are preserved.");
    }

    private void ShowConfirmation(ConfirmationAction action, string title, string message)
    {
        pendingAction = action;
        if (confirmationTitle != null) confirmationTitle.text = title;
        if (confirmationMessage != null) confirmationMessage.text = message;
        if (confirmationRoot != null) confirmationRoot.SetActive(true);
    }

    private void Confirm()
    {
        ConfirmationAction action = pendingAction;
        CancelConfirmation();
        if (action == ConfirmationAction.ResetInventory)
        {
            SetStatus(SkateRunnerDebugResetService.ResetInventory()
                ? "Inventory reset complete."
                : "Debug tools are unavailable in this build.");
        }
        else if (action == ConfirmationAction.ResetAllProgress)
        {
            if (!SkateRunnerDebugResetService.ResetAllGameProgress())
            {
                SetStatus("Debug tools are unavailable in this build.");
                return;
            }
            SetStatus("Game progress reset. Reloading...");
            StartCoroutine(ReloadHomeNextFrame());
        }
    }

    private IEnumerator ReloadHomeNextFrame()
    {
        yield return null;
        SkateRunnerDebugResetService.ReloadHomeScene();
    }

    private void CancelConfirmation()
    {
        pendingAction = ConfirmationAction.None;
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }
}
#endif
