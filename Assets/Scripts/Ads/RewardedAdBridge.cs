using System;
using Elroi.DailyMissions;
using UnityEngine;

public static class RewardedAdBridge
{
    private static bool busy;

    public static bool ShowRewardedAd(string placementId, Action onCompleted, Action onFailedOrClosed = null)
    {
        if (busy) return false;
        busy = true;
        bool finished = false;
        Action success = () =>
        {
            if (finished) return;
            finished = true;
            busy = false;
            try
            {
                onCompleted?.Invoke();
                DailyMissionProgress.ReportRewardedAdCompleted();
            }
            catch (Exception e) { Debug.LogException(e); }
        };
        Action failure = () =>
        {
            if (finished) return;
            finished = true;
            busy = false;
            onFailedOrClosed?.Invoke();
        };

        // TODO: Connect Mobile Monetization Pro V2 rewarded-ad flow here.
        // Invoke the completion callback only after the rewarded ad
        // finishes successfully and the reward is approved.
        SimulateSuccess(placementId, success, failure);
        return true;
    }

    private static void SimulateSuccess(string placementId, Action completed, Action failed) => completed();
}
