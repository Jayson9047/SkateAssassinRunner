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
        public DailyMissionId MissionId => missionId;
        public void Configure(DailyMissionId id)=>missionId=id;
        public void SetHandler(UnityEngine.Events.UnityAction a){if(!claimButton)return;claimButton.onClick.RemoveAllListeners();claimButton.onClick.AddListener(a);}
        public void Bind(DailyMissionDefinition d,int p,bool claimed,string timer)
        {
            if(titleText)titleText.text=d.title;if(descriptionText)descriptionText.text=d.description;
            if(missionIcon&&d.missionIcon)missionIcon.sprite=d.missionIcon;if(rewardIcon&&d.rewardIcon)rewardIcon.sprite=d.rewardIcon;
            if(progressText)progressText.text=$"{p:N0} / {d.target:N0}";if(progressFill)progressFill.fillAmount=Mathf.Clamp01((float)p/d.target);if(timerText)timerText.text=timer;
            if(rewardText)rewardText.text=d.rewardCash>0&&d.rewardGems>0?$"{d.rewardCash:N0} CASH\n{d.rewardGems:N0} GEMS":d.rewardCash>0?$"{d.rewardCash:N0} CASH":$"{d.rewardGems:N0} GEMS";
            bool ready=p>=d.target&&!claimed;if(normalBackground)normalBackground.gameObject.SetActive(!ready);if(completedBackground)completedBackground.gameObject.SetActive(ready);
            if(claimButton)claimButton.interactable=ready;
            if(claimButtonText)claimButtonText.text=claimed?"":"CLAIM";
            if(claimedIndicator)claimedIndicator.SetActive(claimed);
        }
    }
}
