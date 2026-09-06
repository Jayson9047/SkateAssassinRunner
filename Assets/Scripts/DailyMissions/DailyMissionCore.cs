using System;
using System.Globalization;
using UnityEngine;

namespace Elroi.DailyMissions
{
    public enum DailyMissionId
    {
        CollectCash = 0,
        CollectGems = 1,
        CompleteLevels = 2,
        WatchRewardedAds = 3
    }

    [Serializable]
    public sealed class DailyMissionDefinition
    {
        public DailyMissionId id;
        public string title;
        public string description;
        public int target;
        public int rewardCash;
        public int rewardGems;
        public Sprite missionIcon;
        public Sprite rewardIcon;
    }

    [Serializable]
    public sealed class DailyMissionState
    {
        public string utcDay;
        public int collectCash;
        public int collectGems;
        public int completeLevels;
        public int watchRewardedAds;
        public bool collectCashClaimed;
        public bool collectGemsClaimed;
        public bool completeLevelsClaimed;
        public bool watchRewardedAdsClaimed;
    }

    public static class DailyMissionProgress
    {
        public const string SaveKey = "DailyMissions.StateV1";
        public static event Action StateChanged;
        public static event Action DayReset;
        private static DailyMissionState state;
        private static bool loaded;

        // TODO: Use trusted server/store time if clock-tampering protection
        // becomes necessary for production.
        public static DateTime UtcNow => DateTime.UtcNow;
        public static string CurrentDay => UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime NextResetUtc => UtcNow.Date.AddDays(1);

        public static bool EnsureCurrentDay()
        {
            if (!loaded)
            {
                state = ES3.Load(SaveKey, new DailyMissionState());
                loaded = true;
            }
            if (state != null && state.utcDay == CurrentDay) return false;
            state = new DailyMissionState { utcDay = CurrentDay };
            Save();
            DayReset?.Invoke();
            StateChanged?.Invoke();
            return true;
        }

        public static void ReportCashCollected(int amount) => Add(DailyMissionId.CollectCash, amount, 5000);
        public static void ReportGemsCollected(int amount) => Add(DailyMissionId.CollectGems, amount, 30);
        public static void ReportLevelCompleted() => Add(DailyMissionId.CompleteLevels, 1, 10);
        public static void ReportRewardedAdCompleted() => Add(DailyMissionId.WatchRewardedAds, 1, 5);

        public static int GetProgress(DailyMissionId id)
        {
            EnsureCurrentDay();
            switch (id)
            {
                case DailyMissionId.CollectCash: return state.collectCash;
                case DailyMissionId.CollectGems: return state.collectGems;
                case DailyMissionId.CompleteLevels: return state.completeLevels;
                case DailyMissionId.WatchRewardedAds: return state.watchRewardedAds;
                default: return 0;
            }
        }

        public static bool IsClaimed(DailyMissionId id)
        {
            EnsureCurrentDay();
            switch (id)
            {
                case DailyMissionId.CollectCash: return state.collectCashClaimed;
                case DailyMissionId.CollectGems: return state.collectGemsClaimed;
                case DailyMissionId.CompleteLevels: return state.completeLevelsClaimed;
                case DailyMissionId.WatchRewardedAds: return state.watchRewardedAdsClaimed;
                default: return false;
            }
        }

        public static int GetUnclaimedCompletedCount()
        {
            EnsureCurrentDay();
            int count = 0;
            if (GetProgress(DailyMissionId.CollectCash) >= 5000 && !IsClaimed(DailyMissionId.CollectCash)) count++;
            if (GetProgress(DailyMissionId.CollectGems) >= 30 && !IsClaimed(DailyMissionId.CollectGems)) count++;
            if (GetProgress(DailyMissionId.CompleteLevels) >= 10 && !IsClaimed(DailyMissionId.CompleteLevels)) count++;
            if (GetProgress(DailyMissionId.WatchRewardedAds) >= 5 && !IsClaimed(DailyMissionId.WatchRewardedAds)) count++;
            return count;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal static void ResetForDebugTools()
        {
            state = new DailyMissionState { utcDay = CurrentDay };
            loaded = true;
            Save();
            DayReset?.Invoke();
            StateChanged?.Invoke();
        }
#endif

        public static bool TryMarkClaimed(DailyMissionId id, int target)
        {
            EnsureCurrentDay();
            if (IsClaimed(id) || GetProgress(id) < target) return false;
            SetClaimed(id, true);
            Save();
            StateChanged?.Invoke();
            return true;
        }

        public static void RestoreClaimed(DailyMissionId id, bool value)
        {
            EnsureCurrentDay();
            SetClaimed(id, value);
            Save();
            StateChanged?.Invoke();
        }

        private static void Add(DailyMissionId id, int amount, int target)
        {
            if (amount <= 0) return;
            EnsureCurrentDay();
            int value = Mathf.Clamp(GetProgress(id) + amount, 0, target);
            switch (id)
            {
                case DailyMissionId.CollectCash: state.collectCash = value; break;
                case DailyMissionId.CollectGems: state.collectGems = value; break;
                case DailyMissionId.CompleteLevels: state.completeLevels = value; break;
                case DailyMissionId.WatchRewardedAds: state.watchRewardedAds = value; break;
            }
            Save();
            StateChanged?.Invoke();
        }

        private static void SetClaimed(DailyMissionId id, bool value)
        {
            switch (id)
            {
                case DailyMissionId.CollectCash: state.collectCashClaimed = value; break;
                case DailyMissionId.CollectGems: state.collectGemsClaimed = value; break;
                case DailyMissionId.CompleteLevels: state.completeLevelsClaimed = value; break;
                case DailyMissionId.WatchRewardedAds: state.watchRewardedAdsClaimed = value; break;
            }
        }

        private static void Save() => ES3.Save(SaveKey, state);
    }

    public enum CurrencyGrantSource
    {
        GameplayRun, LuckySpin, DailyCalendarReward, FreeCash, DailyMissionClaim, CurrencyPack
    }

    public struct CurrencyChangeResult
    {
        public float previousCash, newCash, previousGems, newGems;
    }

    public static class CurrencyRewardService
    {
        public static bool TryGrantCurrency(int cash, int gems, CurrencyGrantSource source,
            bool countTowardDailyMissions, out CurrencyChangeResult result)
        {
            result = default;
            if (cash < 0 || gems < 0 || cash + gems == 0) return false;
            result.previousCash = Mathf.Max(0, ES3.Load<float>("TotalCash", 0));
            result.previousGems = Mathf.Max(0, ES3.Load<float>("TotalGems", 0));
            result.newCash = result.previousCash + cash;
            result.newGems = result.previousGems + gems;
            try
            {
                ES3.Save("TotalCash", result.newCash);
                ES3.Save("TotalGems", result.newGems);
            }
            catch (Exception e)
            {
                try { ES3.Save("TotalCash", result.previousCash); ES3.Save("TotalGems", result.previousGems); }
                catch { }
                Debug.LogError("Currency grant failed; rollback attempted. " + e.Message);
                return false;
            }
            if (countTowardDailyMissions)
            {
                DailyMissionProgress.ReportCashCollected(cash);
                DailyMissionProgress.ReportGemsCollected(gems);
            }
            return true;
        }
    }
}
