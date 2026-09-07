using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Elroi.DailyMissions.UI
{
    public sealed class DailyMissionRowUI : MonoBehaviour
    {
        [SerializeField] DailyMissionId missionId;
        [SerializeField] Image missionIcon, progressFill, rewardIcon;
        [SerializeField] TMP_Text titleText, descriptionText, progressText, timerText, rewardText, claimButtonText;
        [SerializeField] Button claimButton;
        [SerializeField] Graphic normalBackground, completedBackground;
        [SerializeField] GameObject claimedIndicator;
        [SerializeField] NotificationBadgeView notificationBadge;
        public DailyMissionId MissionId => missionId;
        public void Configure(DailyMissionId id)=>missionId=id;
        public void SetHandler(UnityEngine.Events.UnityAction a){if(!claimButton)return;claimButton.onClick.RemoveAllListeners();claimButton.onClick.AddListener(a);}
        public void Bind(DailyMissionDefinition d,string localizedTitle,string localizedDescription,int p,bool claimed,string timer)
        {
            if(titleText)titleText.text=localizedTitle;if(descriptionText)descriptionText.text=localizedDescription;
            if(missionIcon&&d.missionIcon)missionIcon.sprite=d.missionIcon;if(rewardIcon&&d.rewardIcon)rewardIcon.sprite=d.rewardIcon;
            if(progressText)progressText.text=SkateLocalization.FormatNumber(p)+" / "+SkateLocalization.FormatNumber(d.target);if(progressFill)progressFill.fillAmount=Mathf.Clamp01((float)p/d.target);if(timerText)timerText.text=timer;
            if(rewardText)rewardText.text=d.rewardCash>0&&d.rewardGems>0?SkateLocalization.Get("Rewards","rewards.cash_and_gems",SkateLocalization.FormatNumber(d.rewardCash),SkateLocalization.FormatNumber(d.rewardGems)):d.rewardCash>0?SkateLocalization.Get("Rewards","rewards.cash",SkateLocalization.FormatNumber(d.rewardCash)):SkateLocalization.Get("Rewards","rewards.gems",SkateLocalization.FormatNumber(d.rewardGems));
            bool ready=p>=d.target&&!claimed;if(normalBackground)normalBackground.gameObject.SetActive(!ready);if(completedBackground)completedBackground.gameObject.SetActive(ready);
            if(notificationBadge)notificationBadge.SetCount(ready?1:0);
            if(claimButton)claimButton.interactable=ready;
            if(claimButtonText)claimButtonText.text=claimed?"":SkateLocalization.Get("Common","common.claim");
            if(claimedIndicator)claimedIndicator.SetActive(claimed);
        }
    }
}
