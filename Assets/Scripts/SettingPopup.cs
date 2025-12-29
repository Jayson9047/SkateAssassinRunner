using UnityEngine;
using MoreMountains.MMInterface;

public class SettingsPopup : MMPopup
{
    [Header("Start Button Control")]
    [SerializeField] private GameObject startBtn; // drag StartBtn here

    public override void Open()
    {
        base.Open();

        if (startBtn != null)
        {
            startBtn.SetActive(false);
        }
    }

    public override void Close()
    {
        base.Close();

        if (startBtn != null)
        {
            startBtn.SetActive(true);
        }
    }
}
