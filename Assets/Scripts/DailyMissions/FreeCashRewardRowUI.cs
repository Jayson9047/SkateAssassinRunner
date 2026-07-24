using TMPro;using UnityEngine;using UnityEngine.UI;
namespace Elroi.DailyMissions.UI
{
    public sealed class FreeCashRewardRowUI:MonoBehaviour
    {
        [SerializeField] Button claimButton;[SerializeField] TMP_Text buttonText,rewardText;[SerializeField] GameObject lockIndicator,claimedIndicator,adIcon;[SerializeField] CanvasGroup canvasGroup;
        public void SetHandler(UnityEngine.Events.UnityAction a){if(!claimButton)return;claimButton.onClick.RemoveAllListeners();claimButton.onClick.AddListener(a);}
        public void Bind(int cash,int gems,bool ad,bool claimed,bool available,bool processing)
        {
            if(rewardText)rewardText.text=gems>0?$"{cash:N0} CASH + {gems:N0} GEMS":$"{cash:N0} CASH";
            if(claimButton)claimButton.interactable=available&&!claimed&&!processing;
            bool locked=!available&&!claimed;
            if(buttonText)buttonText.text=claimed||locked?"":processing?"WAIT...":ad?"WATCH":"FREE";
            if(lockIndicator)lockIndicator.SetActive(locked);
            if(claimedIndicator)claimedIndicator.SetActive(claimed);
            if(adIcon)adIcon.SetActive(ad&&available&&!claimed);
            if(canvasGroup)canvasGroup.alpha=available||claimed?1:.45f;
        }
    }
}
