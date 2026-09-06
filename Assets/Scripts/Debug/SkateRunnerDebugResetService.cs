#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Elroi.DailyMissions;
using Elroi.DailyMissions.UI;
using MoreMountains.InfiniteRunnerEngine;
using MoreMountains.Tools;
using UnityEngine;

/// <summary>Development-build-only local profile overrides used by the authored Debug Tools panel.</summary>
public static class SkateRunnerDebugResetService
{
    public const float MaximumDebugCurrency = 1000000000f;
    public const string HomeSceneName = "SkateRunnerStartScreen";

    private static readonly string[] DailyRewardKeys =
    {
        "DailyRewards_Day1_Claimed", "DailyRewards_Day2_Claimed", "DailyRewards_Day3_Claimed",
        "DailyRewards_Day4_Claimed", "DailyRewards_Day5_Claimed", "DailyRewards_Day6_Claimed",
        "DailyRewards_Day7_Claimed", "DailyRewards_CurrentClaimableDay",
        "DailyRewards_LastClaimedUtc", "DailyRewards_NextUnlockUtcTicks"
    };

    public static bool IsAvailable
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }
    }

    public static bool TrySetCash(string text, out float value)
    {
        return TrySetCurrency("TotalCash", text, out value);
    }

    public static bool TrySetGems(string text, out float value)
    {
        return TrySetCurrency("TotalGems", text, out value);
    }

    public static bool ResetInventory()
    {
        if (!IsAvailable) return false;

        // DEBUG ONLY:
        // This resets local entitlement/ownership state for development testing.
        // Do not use this mechanism for production purchase revocation.
        DeleteEs3Key(SwordOwnershipSave.OwnedSwordIdsKey);
        DeleteEs3Key(WeaponPowerOwnershipSave.OwnedWeaponPowerIdsKey);
        DeleteEs3Key(RollerbladeOwnershipSave.OwnedRollerbladeIdsKey);

        SwordSave.Save(SwordId.Katana);
        WeaponPowerSave.Save(WeaponPowerId.None);
        RollerbladeSave.Save(RollerbladeId.Default);
        InventoryNewItemNotifications.ClearAll();
        RefreshInventoryAndShopUi();
        return true;
    }

    public static bool ResetAllGameProgress()
    {
        if (!IsAvailable) return false;

        CloseTransientPopups();

        DeleteEs3Key("TotalCash");
        DeleteEs3Key("TotalGems");
        DeleteEs3Key("LevelNum");
        DeleteEs3Key(SwordOwnershipSave.OwnedSwordIdsKey);
        DeleteEs3Key(WeaponPowerOwnershipSave.OwnedWeaponPowerIdsKey);
        DeleteEs3Key(RollerbladeOwnershipSave.OwnedRollerbladeIdsKey);
        DeleteEs3Key(InventoryNewItemNotifications.SaveKey);
        DeleteEs3Key(DailyMissionProgress.SaveKey);
        DeleteEs3Key(FreeCashDailyPopup.SaveKey);
        DeleteEs3Key("LuckySpin_AvailableSpins");
        DeleteEs3Key("LuckySpin_DailyAdsWatched");
        DeleteEs3Key("LuckySpin_DailyResetDate");
        for (int i = 0; i < DailyRewardKeys.Length; i++) DeleteEs3Key(DailyRewardKeys[i]);

        // Save APIs establish the same effective equipped defaults returned on a fresh install.
        SwordSave.Save(SwordId.Katana);
        WeaponPowerSave.Save(WeaponPowerId.None);
        RollerbladeSave.Save(RollerbladeId.Default);
        DownAttackPowerSave.Save(DownAttackPowerId.None);
        CharacterPowerSave.Save(CharacterPowerId.None);
        SingleHighScoreManager.ResetHighScore();
        PlayerPrefs.Save();

        InventoryNewItemNotifications.ClearAll();
        DailyMissionProgress.ResetForDebugTools();
        return true;
    }

    public static void ReloadHomeScene()
    {
        if (!IsAvailable) return;
        MMSceneLoadingManager.LoadScene(HomeSceneName);
    }

    private static bool TrySetCurrency(string key, string text, out float value)
    {
        value = 0f;
        if (!IsAvailable || string.IsNullOrWhiteSpace(text)) return false;

        float parsed;
        bool parsedOk = float.TryParse(
            text,
            System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
            System.Globalization.CultureInfo.InvariantCulture,
            out parsed);
        if (!parsedOk)
        {
            parsedOk = float.TryParse(text, out parsed);
        }
        if (!parsedOk || float.IsNaN(parsed) || float.IsInfinity(parsed) || parsed < 0f) return false;

        value = Mathf.Clamp(parsed, 0f, MaximumDebugCurrency);
        ES3.Save(key, value);
        RefreshHomeUi();
        return true;
    }

    private static void RefreshHomeUi()
    {
        HomeUIBinder[] binders = UnityEngine.Object.FindObjectsByType<HomeUIBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < binders.Length; i++) binders[i].RefreshFromSave();
    }

    private static void RefreshInventoryAndShopUi()
    {
        CloseTransientPopups();

        SwordShopController[] swordShops = UnityEngine.Object.FindObjectsByType<SwordShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < swordShops.Length; i++) swordShops[i].RefreshItems();
        WeaponPowerShopController[] powerShops = UnityEngine.Object.FindObjectsByType<WeaponPowerShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < powerShops.Length; i++) powerShops[i].RefreshItems();
        RollerbladeShopController[] rollerShops = UnityEngine.Object.FindObjectsByType<RollerbladeShopController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rollerShops.Length; i++) rollerShops[i].RefreshItems();

        SwordInventoryController[] swordInventories = UnityEngine.Object.FindObjectsByType<SwordInventoryController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < swordInventories.Length; i++) swordInventories[i].RefreshAfterExternalInventoryReset();
        WeaponPowerInventoryController[] powerInventories = UnityEngine.Object.FindObjectsByType<WeaponPowerInventoryController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < powerInventories.Length; i++) powerInventories[i].RefreshAfterExternalInventoryReset();
        RollerbladeInventoryController[] rollerInventories = UnityEngine.Object.FindObjectsByType<RollerbladeInventoryController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rollerInventories.Length; i++) rollerInventories[i].RefreshAfterExternalInventoryReset();

        RefreshHomeUi();
        HomeMenuNotificationController[] notifications = UnityEngine.Object.FindObjectsByType<HomeMenuNotificationController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < notifications.Length; i++) notifications[i].Refresh();
    }

    private static void CloseTransientPopups()
    {
        WeaponPowerPurchasePopup[] purchasePopups = UnityEngine.Object.FindObjectsByType<WeaponPowerPurchasePopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < purchasePopups.Length; i++) purchasePopups[i].Close();
        CrystalRewardRevealPopup.CloseActiveImmediate();
    }

    private static void DeleteEs3Key(string key)
    {
        if (ES3.KeyExists(key)) ES3.DeleteKey(key);
    }
}
#endif
