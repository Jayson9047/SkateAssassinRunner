using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
namespace Elroi.DailyMissions.UI
{
    public sealed class DailyMissionPageController:MonoBehaviour
    {
        [SerializeField] DailyMissionRowUI[] rows;[SerializeField] HomeUIBinder homeUIBinder;
        [SerializeField] Sprite cashIcon,gemIcon;[SerializeField] DailyMissionDefinition[] definitions;
        readonly Dictionary<DailyMissionId,DailyMissionDefinition> map=new Dictionary<DailyMissionId,DailyMissionDefinition>();
        bool processing;string loadedDay;Coroutine resetRoutine;
        void Awake()
        {
            if(definitions==null||definitions.Length!=4)definitions=new[]{D(DailyMissionId.CollectCash,"COLLECT CASH","Collect 5,000 Cash",5000,500,0),D(DailyMissionId.CollectGems,"COLLECT GEMS","Collect 30 Gems",30,0,5),D(DailyMissionId.CompleteLevels,"CROSS 10 LEVELS","Complete 10 Levels",10,2000,5),D(DailyMissionId.WatchRewardedAds,"AD BREAK","Watch 5 Ads",5,2500,5)};
            foreach(var d in definitions)if(d!=null)map[d.id]=d;foreach(var row in rows){if(!row)continue;DailyMissionId id=row.MissionId;row.SetHandler(()=>Claim(id));}
        }
        void OnEnable(){DailyMissionProgress.StateChanged+=Refresh;DailyMissionProgress.DayReset+=ResetPending;SkateLocalization.LocaleChanged+=LocaleChanged;DailyMissionProgress.EnsureCurrentDay();loadedDay=DailyMissionProgress.CurrentDay;processing=false;Refresh();resetRoutine=StartCoroutine(WaitForUtcReset());}
        void OnDisable(){DailyMissionProgress.StateChanged-=Refresh;DailyMissionProgress.DayReset-=ResetPending;SkateLocalization.LocaleChanged-=LocaleChanged;if(resetRoutine!=null)StopCoroutine(resetRoutine);resetRoutine=null;processing=false;}
        void OnApplicationFocus(bool f){if(f&&isActiveAndEnabled){DailyMissionProgress.EnsureCurrentDay();loadedDay=DailyMissionProgress.CurrentDay;Refresh();if(resetRoutine!=null)StopCoroutine(resetRoutine);resetRoutine=StartCoroutine(WaitForUtcReset());}}
        IEnumerator WaitForUtcReset(){while(isActiveAndEnabled){double seconds=Math.Max(0.1,(DailyMissionProgress.NextResetUtc-DailyMissionProgress.UtcNow).TotalSeconds+0.1);yield return new WaitForSecondsRealtime((float)Math.Min(seconds,86401));DailyMissionProgress.EnsureCurrentDay();loadedDay=DailyMissionProgress.CurrentDay;Refresh();}}
        public void Refresh(){if(rows==null)return;foreach(var row in rows){DailyMissionDefinition d;if(!row||!map.TryGetValue(row.MissionId,out d))continue;int p=DailyMissionProgress.GetProgress(d.id);bool c=DailyMissionProgress.IsClaimed(d.id);row.Bind(d,Title(d),Description(d),p,c,Timer(d,p,c),cashIcon,gemIcon);}}
        void LocaleChanged(Locale locale)=>Refresh();
        void Claim(DailyMissionId id)
        {
            DailyMissionDefinition d;if(processing||!map.TryGetValue(id,out d))return;string day=DailyMissionProgress.CurrentDay;DailyMissionProgress.EnsureCurrentDay();
            if(day!=DailyMissionProgress.CurrentDay||DailyMissionProgress.IsClaimed(id)||DailyMissionProgress.GetProgress(id)<d.target)return;processing=true;
            if(!DailyMissionProgress.TryMarkClaimed(id,d.target)){processing=false;return;}
            CurrencyChangeResult r;if(!CurrencyRewardService.TryGrantCurrency(d.rewardCash,d.rewardGems,CurrencyGrantSource.DailyMissionClaim,false,out r)){DailyMissionProgress.RestoreClaimed(id,false);processing=false;return;}
            if(homeUIBinder)homeUIBinder.AnimateBalances(r.previousCash,r.newCash,r.previousGems,r.newGems);
            CrystalRewardRevealPopup.TryShow(RewardRevealRequest.ForCurrencies(d.rewardCash,d.rewardGems,cashIcon,gemIcon));processing=false;Refresh();
        }
        void ResetPending(){processing=false;CrystalRewardRevealPopup.CloseActiveImmediate();Refresh();}
        static string Timer(DailyMissionDefinition d,int p,bool c){TimeSpan t=DailyMissionProgress.NextResetUtc-DailyMissionProgress.UtcNow;if(t<TimeSpan.Zero)t=TimeSpan.Zero;string v=$"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";return SkateLocalization.Get("Common",c?"common.resets_in":p>=d.target?"common.claim_in":"common.ends_in",v);}
        static string Title(DailyMissionDefinition d){switch(d.id){case DailyMissionId.CollectCash:return SkateLocalization.Get("Missions","missions.collect_cash.title");case DailyMissionId.CollectGems:return SkateLocalization.Get("Missions","missions.collect_gems.title");case DailyMissionId.CompleteLevels:return SkateLocalization.Get("Missions","missions.cross_levels.title");default:return SkateLocalization.Get("Missions","missions.ad_break.title");}}
        static string Description(DailyMissionDefinition d){string value=SkateLocalization.FormatNumber(d.target);switch(d.id){case DailyMissionId.CollectCash:return SkateLocalization.Get("Missions","missions.collect_cash.description",value);case DailyMissionId.CollectGems:return SkateLocalization.Get("Missions","missions.collect_gems.description",value);case DailyMissionId.CompleteLevels:return SkateLocalization.Get("Missions","missions.complete_levels.description",value);default:return SkateLocalization.Get("Missions","missions.watch_ads.description",value);}}
        static DailyMissionDefinition D(DailyMissionId id,string t,string d,int target,int cash,int gems){return new DailyMissionDefinition{id=id,title=t,description=d,target=target,rewardCash=cash,rewardGems=gems};}
    }
}
