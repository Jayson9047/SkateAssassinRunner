using System;
using System.Collections.Generic;
using UnityEngine;
namespace Elroi.DailyMissions.UI
{
    public sealed class DailyMissionPageController:MonoBehaviour
    {
        [SerializeField] DailyMissionRowUI[] rows;[SerializeField] RewardGrantedPopup rewardPopup;[SerializeField] HomeUIBinder homeUIBinder;
        [SerializeField] Sprite cashIcon,gemIcon;[SerializeField] DailyMissionDefinition[] definitions;
        readonly Dictionary<DailyMissionId,DailyMissionDefinition> map=new Dictionary<DailyMissionId,DailyMissionDefinition>();
        bool processing;float nextTick;string loadedDay;
        void Awake()
        {
            if(definitions==null||definitions.Length!=4)definitions=new[]{D(DailyMissionId.CollectCash,"COLLECT CASH","Collect 5,000 Cash",5000,500,0),D(DailyMissionId.CollectGems,"COLLECT GEMS","Collect 30 Gems",30,0,5),D(DailyMissionId.CompleteLevels,"CROSS 10 LEVELS","Complete 10 Levels",10,2000,5),D(DailyMissionId.WatchRewardedAds,"AD BREAK","Watch 5 Ads",5,2500,5)};
            foreach(var d in definitions)if(d!=null)map[d.id]=d;foreach(var row in rows){if(!row)continue;DailyMissionId id=row.MissionId;row.SetHandler(()=>Claim(id));}
        }
        void OnEnable(){DailyMissionProgress.StateChanged+=Refresh;DailyMissionProgress.DayReset+=ResetPending;DailyMissionProgress.EnsureCurrentDay();loadedDay=DailyMissionProgress.CurrentDay;processing=false;Refresh();}
        void OnDisable(){DailyMissionProgress.StateChanged-=Refresh;DailyMissionProgress.DayReset-=ResetPending;processing=false;}
        void Update(){if(Time.unscaledTime<nextTick)return;nextTick=Time.unscaledTime+1;string before=loadedDay;DailyMissionProgress.EnsureCurrentDay();loadedDay=DailyMissionProgress.CurrentDay;if(before!=loadedDay)ResetPending();Refresh();}
        void OnApplicationFocus(bool f){if(f&&isActiveAndEnabled){DailyMissionProgress.EnsureCurrentDay();Refresh();}}
        public void Refresh(){if(rows==null)return;foreach(var row in rows){DailyMissionDefinition d;if(!row||!map.TryGetValue(row.MissionId,out d))continue;int p=DailyMissionProgress.GetProgress(d.id);bool c=DailyMissionProgress.IsClaimed(d.id);row.Bind(d,p,c,Timer(d,p,c));}}
        void Claim(DailyMissionId id)
        {
            DailyMissionDefinition d;if(processing||!map.TryGetValue(id,out d))return;string day=DailyMissionProgress.CurrentDay;DailyMissionProgress.EnsureCurrentDay();
            if(day!=DailyMissionProgress.CurrentDay||DailyMissionProgress.IsClaimed(id)||DailyMissionProgress.GetProgress(id)<d.target)return;processing=true;
            if(!DailyMissionProgress.TryMarkClaimed(id,d.target)){processing=false;return;}
            CurrencyChangeResult r;if(!CurrencyRewardService.TryGrantCurrency(d.rewardCash,d.rewardGems,CurrencyGrantSource.DailyMissionClaim,false,out r)){DailyMissionProgress.RestoreClaimed(id,false);processing=false;return;}
            if(homeUIBinder)homeUIBinder.AnimateBalances(r.previousCash,r.newCash,r.previousGems,r.newGems);if(rewardPopup)rewardPopup.Show(d.rewardCash>0&&d.rewardGems>0?"TREASURE BOX OPENED!":"REWARD CLAIMED!",d.rewardCash,d.rewardGems,cashIcon,gemIcon);processing=false;Refresh();
        }
        void ResetPending(){processing=false;if(rewardPopup)rewardPopup.Close();Refresh();}
        static string Timer(DailyMissionDefinition d,int p,bool c){TimeSpan t=DailyMissionProgress.NextResetUtc-DailyMissionProgress.UtcNow;if(t<TimeSpan.Zero)t=TimeSpan.Zero;string v=$"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";return c?"RESETS IN "+v:p>=d.target?"CLAIM IN "+v:"ENDS IN "+v;}
        static DailyMissionDefinition D(DailyMissionId id,string t,string d,int target,int cash,int gems){return new DailyMissionDefinition{id=id,title=t,description=d,target=target,rewardCash=cash,rewardGems=gems};}
    }
}
