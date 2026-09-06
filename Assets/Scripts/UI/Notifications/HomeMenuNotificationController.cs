using Elroi.DailyMissions;
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomeMenuNotificationController : MonoBehaviour
{
    [SerializeField] private NotificationBadgeView inventoryBadge;
    [SerializeField] private NotificationBadgeView rewardsBadge;
    [SerializeField] private NotificationBadgeView missionsBadge;
    [SerializeField] private DailyRewardsPage dailyRewardsPage;
    private Coroutine boundaryRefresh;

    private void OnEnable()
    {
        InventoryNewItemNotifications.Changed += Refresh;
        DailyMissionProgress.StateChanged += Refresh;
        DailyMissionProgress.DayReset += Refresh;
        DailyRewardsPage.StateChanged += Refresh;
        Refresh();
        boundaryRefresh = StartCoroutine(RefreshAtNextBoundary());
    }

    private void OnDisable()
    {
        InventoryNewItemNotifications.Changed -= Refresh;
        DailyMissionProgress.StateChanged -= Refresh;
        DailyMissionProgress.DayReset -= Refresh;
        DailyRewardsPage.StateChanged -= Refresh;
        if (boundaryRefresh != null) StopCoroutine(boundaryRefresh);
        boundaryRefresh = null;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused || !isActiveAndEnabled) return;
        DailyMissionProgress.EnsureCurrentDay();
        if (dailyRewardsPage != null) dailyRewardsPage.RefreshForNotifications();
        Refresh();
        if (boundaryRefresh != null) StopCoroutine(boundaryRefresh);
        boundaryRefresh = StartCoroutine(RefreshAtNextBoundary());
    }

    public void Refresh()
    {
        if (inventoryBadge != null) inventoryBadge.SetCount(InventoryNewItemNotifications.TotalCount);
        if (rewardsBadge != null) rewardsBadge.SetCount(dailyRewardsPage != null ? dailyRewardsPage.ClaimableCount : 0);
        if (missionsBadge != null) missionsBadge.SetCount(DailyMissionProgress.GetUnclaimedCompletedCount());
    }

    private IEnumerator RefreshAtNextBoundary()
    {
        while (isActiveAndEnabled)
        {
            double missionSeconds = Math.Max(0.1, (DailyMissionProgress.NextResetUtc - DailyMissionProgress.UtcNow).TotalSeconds);
            double rewardSeconds = dailyRewardsPage != null ? dailyRewardsPage.SecondsUntilNextUnlock : missionSeconds;
            yield return new WaitForSecondsRealtime((float)Math.Min(Math.Min(missionSeconds, rewardSeconds) + 0.1, 86401));
            DailyMissionProgress.EnsureCurrentDay();
            if (dailyRewardsPage != null) dailyRewardsPage.RefreshForNotifications();
            Refresh();
        }
    }
}
