using UnityEngine;
using MoreMountains.MMInterface;

public class SettingsPopup : MMPopup
{
    [Header("Start Button Control")]
    [SerializeField] private GameObject startBtn; // drag StartBtn here

    [Header("Settings UI")]
    [SerializeField] private SettingsPopupController settingsController;

    public override void Open()
    {
        if (CurrentlyOpen)
        {
            return;
        }

        base.Open();
        settingsController?.HandlePopupOpened();
    }

    public override void Close()
    {
        if (!CurrentlyOpen)
        {
            return;
        }

        settingsController?.HandlePopupClosed();
        base.Close();
    }
}
