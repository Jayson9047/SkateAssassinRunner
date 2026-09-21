using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the compact audio and haptics controls from the gameplay pause menu.</summary>
public sealed class PauseQuickSettingsPanel : MonoBehaviour
{
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private SettingsPopupController settingsController;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(OpenSettings);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);
        CloseSettings();
    }

    private void OnDisable()
    {
        CloseSettings();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(OpenSettings);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseSettings);
    }

    public void OpenSettings()
    {
        if (panel != null) panel.SetActive(true);
        if (settingsController != null) settingsController.HandlePopupOpened();
    }

    public void CloseSettings()
    {
        if (panel != null) panel.SetActive(false);
    }
}
