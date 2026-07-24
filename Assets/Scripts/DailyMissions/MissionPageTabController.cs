using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Elroi.DailyMissions.UI
{
    public sealed class MissionPageTabController:MonoBehaviour
    {
        [SerializeField] GameObject dailyScrollRect,storyScrollRect,mafiaScrollRect;[SerializeField] Button dailyButton,storyButton,mafiaButton;
        [SerializeField] TMP_Text dailyText,storyText,mafiaText;[SerializeField] Color selectedColor=Color.white,unselectedColor=new Color(.25f,.35f,.55f);
        void Awake(){B(dailyButton,ShowDaily);B(storyButton,ShowStory);B(mafiaButton,ShowMafia);}void OnEnable()=>ShowDaily();
        public void ShowDaily()=>Select(dailyScrollRect);public void ShowStory()=>Select(storyScrollRect);public void ShowMafia()=>Select(mafiaScrollRect);
        void Select(GameObject g){if(dailyScrollRect)dailyScrollRect.SetActive(g==dailyScrollRect);if(storyScrollRect)storyScrollRect.SetActive(g==storyScrollRect);if(mafiaScrollRect)mafiaScrollRect.SetActive(g==mafiaScrollRect);if(dailyText)dailyText.color=g==dailyScrollRect?selectedColor:unselectedColor;if(storyText)storyText.color=g==storyScrollRect?selectedColor:unselectedColor;if(mafiaText)mafiaText.color=g==mafiaScrollRect?selectedColor:unselectedColor;}
        static void B(Button b,UnityEngine.Events.UnityAction a){if(!b)return;b.onClick.RemoveListener(a);b.onClick.AddListener(a);}
    }
}
