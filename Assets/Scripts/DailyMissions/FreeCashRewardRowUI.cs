using TMPro;using UnityEngine;using UnityEngine.UI;
namespace Elroi.DailyMissions.UI
{
    public sealed class FreeCashRewardRowUI:MonoBehaviour
    {
        [SerializeField] Button claimButton;[SerializeField] TMP_Text buttonText,rewardText;[SerializeField] GameObject lockIndicator,claimedIndicator,adIcon;[SerializeField] CanvasGroup canvasGroup;
        public void SetHandler(UnityEngine.Events.UnityAction a){if(!claimButton)return;claimButton.onClick.RemoveAllListeners();claimButton.onClick.AddListener(a);}
        public void Bind(int cash,int gems,bool ad,bool claimed,bool available,bool processing)
        {
            if(rewardText)rewardText.text=gems>0?SkateLocalization.Get("Rewards","rewards.cash_and_gems",SkateLocalization.FormatNumber(cash),SkateLocalization.FormatNumber(gems)):SkateLocalization.Get("Rewards","rewards.cash",SkateLocalization.FormatNumber(cash));
            if(claimButton)claimButton.interactable=available&&!claimed&&!processing;
            bool locked=!available&&!claimed;
            if(buttonText)buttonText.text=claimed||locked?"":processing?SkateLocalization.Get("Popups","popups.wait"):ad?SkateLocalization.Get("Common","common.watch"):SkateLocalization.Get("Common","common.free");
            if(lockIndicator)lockIndicator.SetActive(locked);
            if(claimedIndicator)claimedIndicator.SetActive(claimed);
            if(adIcon)adIcon.SetActive(ad&&available&&!claimed);
            if(canvasGroup)canvasGroup.alpha=available||claimed?1:.45f;
        }
    }
}
