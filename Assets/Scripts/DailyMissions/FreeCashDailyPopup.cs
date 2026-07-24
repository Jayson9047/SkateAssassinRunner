using System;using System.Globalization;using TMPro;using UnityEngine;using UnityEngine.UI;
namespace Elroi.DailyMissions.UI
{
    [Serializable]public sealed class FreeCashDailyState{public string utcDay;public bool[] claimed=new bool[5];public int unlockedStep;}
    public sealed class FreeCashDailyPopup:MonoBehaviour
    {
        public const string SaveKey="FreeCashDaily.StateV1";static readonly int[] Cash={100,500,1000,2000,4000},Gems={0,0,0,3,5};
        [SerializeField] FreeCashRewardRowUI[] rows;[SerializeField] TMP_Text resetTimerText;[SerializeField] Button closeButton;[SerializeField] RewardGrantedPopup rewardPopup;
        [SerializeField] HomeUIBinder homeUIBinder;[SerializeField] Sprite cashIcon,gemIcon;FreeCashDailyState state;bool processing;float nextTick;
        void Awake(){if(closeButton){closeButton.onClick.RemoveListener(Close);closeButton.onClick.AddListener(Close);}for(int i=0;i<rows.Length;i++){int n=i;if(rows[i])rows[i].SetHandler(()=>Claim(n));}}
        void OnEnable(){processing=false;EnsureDay();Refresh();}void OnDisable()=>processing=false;
        void Update(){if(Time.unscaledTime<nextTick)return;nextTick=Time.unscaledTime+1;if(EnsureDay()&&rewardPopup)rewardPopup.Close();Refresh();}
        public void Open(){gameObject.SetActive(true);transform.SetAsLastSibling();}public void Close(){processing=false;gameObject.SetActive(false);}
        void Claim(int i){if(processing||i<0||i>=5)return;EnsureDay();if(state.claimed[i]||i!=state.unlockedStep)return;processing=true;Refresh();if(i==0)Complete(i);else if(!RewardedAdBridge.ShowRewardedAd("free_cash_step_"+(i+1),()=>Complete(i),()=>{processing=false;Refresh();})){processing=false;Refresh();}}
        void Complete(int i){EnsureDay();if(!processing||state.claimed[i]||i!=state.unlockedStep){processing=false;Refresh();return;}CurrencyChangeResult r;if(!CurrencyRewardService.TryGrantCurrency(Cash[i],Gems[i],CurrencyGrantSource.FreeCash,true,out r)){processing=false;Refresh();return;}state.claimed[i]=true;state.unlockedStep=Mathf.Min(5,i+1);ES3.Save(SaveKey,state);if(homeUIBinder)homeUIBinder.AnimateBalances(r.previousCash,r.newCash,r.previousGems,r.newGems);if(rewardPopup)rewardPopup.Show("REWARD CLAIMED!",Cash[i],Gems[i],cashIcon,gemIcon);processing=false;Refresh();}
        bool EnsureDay(){string today=DateTime.UtcNow.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);if(state==null)state=ES3.Load(SaveKey,new FreeCashDailyState());if(state.claimed!=null&&state.claimed.Length==5&&state.utcDay==today)return false;state=new FreeCashDailyState{utcDay=today,claimed=new bool[5],unlockedStep=0};ES3.Save(SaveKey,state);processing=false;return true;}
        void Refresh(){if(state==null)return;for(int i=0;i<rows.Length&&i<5;i++)if(rows[i])rows[i].Bind(Cash[i],Gems[i],i>0,state.claimed[i],i==state.unlockedStep,processing);TimeSpan t=DateTime.UtcNow.Date.AddDays(1)-DateTime.UtcNow;if(t<TimeSpan.Zero)t=TimeSpan.Zero;if(resetTimerText)resetTimerText.text=$"RESETS IN {(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";}
    }
}
