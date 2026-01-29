using UnityEngine;
using MoreMountains.MMInterface;

public class SettingsPopup : MMPopup
{
    [Header("Start Button Control")]
    [SerializeField] private GameObject startBtn; // drag StartBtn here

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }
}
