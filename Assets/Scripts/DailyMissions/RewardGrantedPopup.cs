using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Elroi.DailyMissions.UI
{
    public sealed class RewardGrantedPopup:MonoBehaviour
    {
        [SerializeField] TMP_Text titleText,rewardText;[SerializeField] Image primaryIcon,secondaryIcon;[SerializeField] Button okButton;
        void Awake(){if(okButton){okButton.onClick.RemoveListener(Close);okButton.onClick.AddListener(Close);}}
        public void Show(string title,int cash,int gems,Sprite cashIcon,Sprite gemIcon)
        {
            if(titleText)titleText.text=title;if(rewardText)rewardText.text=cash>0&&gems>0?$"+{cash:N0} CASH\n+{gems:N0} GEMS":cash>0?$"+{cash:N0} CASH":$"+{gems:N0} GEMS";
            Icon(primaryIcon,cash>0?cashIcon:gemIcon,true);Icon(secondaryIcon,gemIcon,cash>0&&gems>0);gameObject.SetActive(true);transform.SetAsLastSibling();
        }
        public void Close()=>gameObject.SetActive(false);
        static void Icon(Image i,Sprite s,bool on){if(!i)return;i.gameObject.SetActive(on);if(s)i.sprite=s;i.preserveAspect=true;}
    }
}
