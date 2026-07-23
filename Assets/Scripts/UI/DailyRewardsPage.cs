using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardsPage : MonoBehaviour
{
    [Serializable]
    private class RewardDay
    {
        public Transform root;
        public GameObject clearObject;
        public GameObject focusObject;
        public Image icon;
        public Button button;
        public int cash;
        public int gems;
    }

    private const string CashSaveKey = "TotalCash";
    private const string GemsSaveKey = "TotalGems";
    private const string ClaimedSaveKeyPrefix = "DailyRewards_Day";
    private const string CurrentClaimableDaySaveKey = "DailyRewards_CurrentClaimableDay";
    private const string LastClaimedUtcSaveKey = "DailyRewards_LastClaimedUtc";
    private const string LegacyNextUnlockUtcTicksSaveKey = "DailyRewards_NextUnlockUtcTicks";

    [Header("Daily Unlock Time")]
    [SerializeField, Range(0, 23)] private int unlockHour = 0;
    [SerializeField, Range(0, 59)] private int unlockMinute = 0;
    [SerializeField, Tooltip("0 means UTC. Use this to test other time zones, for example -5 for EST or 5.5 for IST.")]
    private float unlockTimeZoneOffsetHours = 0f;

    [Header("Reward Popup")]
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private Image popupIcon;
    [SerializeField] private TMP_Text popupRewardText;
    [SerializeField] private Button popupOkButton;

    [Header("Currency Display")]
    [SerializeField] private HomeUIBinder homeUIBinder;

    private readonly List<RewardDay> rewardDays = new();
    private bool claimProcessing;

    private void Awake()
    {
        BuildRewardDays();
        BindButtons();
        BindPopup();
        ResolveHomeUIBinder();
        EnsureRewardState();
        RefreshState();
    }

    private void OnEnable()
    {
        ResolveHomeUIBinder();
        EnsureRewardState();
        RefreshState();
    }

    private void BuildRewardDays()
    {
        rewardDays.Clear();

        AddRewardDay("Day1_List", 0, 50);
        AddRewardDay("Day2_List", 500, 0);
        AddRewardDay("Day3_List", 0, 10);
        AddRewardDay("Day4_List", 0, 50);
        AddRewardDay("Day5_List", 1000, 0);
        AddRewardDay("Day6_List", 0, 20);
        AddRewardDay("Reward_Day7", 5000, 150);
    }

    private void AddRewardDay(string dayObjectName, int cash, int gems)
    {
        Transform root = FindRewardRoot(dayObjectName);
        if (root == null)
        {
            Debug.LogWarning($"DailyRewardsPage: Could not find reward day '{dayObjectName}'.", this);
            return;
        }

        RewardDay day = new RewardDay
        {
            root = root,
            clearObject = FindChild(root, "Clear")?.gameObject,
            focusObject = FindChild(root, "Focus")?.gameObject,
            icon = FindRewardIcon(root),
            cash = cash,
            gems = gems
        };

        day.button = root.GetComponent<Button>();
        if (day.button == null)
        {
            day.button = root.gameObject.AddComponent<Button>();
        }

        Image targetGraphic = root.GetComponent<Image>();
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
            day.button.targetGraphic = targetGraphic;
        }

        day.button.transition = Selectable.Transition.None;
        rewardDays.Add(day);
    }

    private Transform FindRewardRoot(string dayObjectName)
    {
        Transform groupReward = transform.Find("Popup_RewardWeek/Group_Reward");
        Transform searchRoot = groupReward != null ? groupReward : transform;
        Transform found = FindChild(searchRoot, dayObjectName);

        if (found != null)
        {
            return found;
        }

        string alternateName = dayObjectName.Replace("_List", "_LIst");
        return alternateName == dayObjectName ? null : FindChild(searchRoot, alternateName);
    }

    private Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private Image FindRewardIcon(Transform root)
    {
        Image bestImage = null;
        int bestScore = int.MinValue;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image == null || image.sprite == null)
            {
                continue;
            }

            if (HasParentNamed(image.transform, "Clear") || HasParentNamed(image.transform, "Focus"))
            {
                continue;
            }

            string imageName = image.name.ToLowerInvariant();
            int score = 0;

            if (imageName == "icon")
            {
                score += 100;
            }
            else if (imageName.Contains("icon"))
            {
                score += 60;
            }
            else
            {
                score -= 1000;
            }

            if (imageName.Contains("star") || imageName.Contains("gold"))
            {
                score -= 30;
            }

            RectTransform rect = image.transform as RectTransform;
            if (rect != null)
            {
                score += Mathf.RoundToInt(rect.rect.width + rect.rect.height);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestImage = image;
            }
        }

        return bestImage;
    }

    private bool HasParentNamed(Transform child, string parentName)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name == parentName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void BindButtons()
    {
        for (int i = 0; i < rewardDays.Count; i++)
        {
            int dayIndex = i;
            rewardDays[i].button.onClick.RemoveListener(() => ClaimReward(dayIndex));
            rewardDays[i].button.onClick.AddListener(() => ClaimReward(dayIndex));
        }
    }

    private void BindPopup()
    {
        if (rewardPopup == null)
        {
            Transform popupTransform = FindChild(transform, "DailyRewardClaimPopup");
            if (popupTransform != null)
            {
                rewardPopup = popupTransform.gameObject;
            }
        }

        if (rewardPopup != null)
        {
            if (popupIcon == null)
            {
                popupIcon = FindChild(rewardPopup.transform, "Icon")?.GetComponent<Image>();
            }

            if (popupRewardText == null)
            {
                popupRewardText = FindChild(rewardPopup.transform, "RewardText")?.GetComponent<TMP_Text>();
            }

            if (popupOkButton == null)
            {
                popupOkButton = FindChild(rewardPopup.transform, "Button_OK")?.GetComponent<Button>();
            }

            rewardPopup.SetActive(false);
        }

        if (popupOkButton != null)
        {
            popupOkButton.onClick.RemoveListener(CloseRewardPopup);
            popupOkButton.onClick.AddListener(CloseRewardPopup);
        }
    }

    private void RefreshState()
    {
        AdvanceClaimableDayIfReady();

        int currentClaimableDay = GetCurrentClaimableDay();
        int focusDay = GetFocusDay(currentClaimableDay);

        for (int i = 0; i < rewardDays.Count; i++)
        {
            bool claimed = IsClaimed(i);
            bool focused = i == focusDay;

            if (rewardDays[i].clearObject != null)
            {
                rewardDays[i].clearObject.SetActive(claimed);
            }

            if (rewardDays[i].focusObject != null)
            {
                rewardDays[i].focusObject.SetActive(focused);
            }

            if (rewardDays[i].button != null)
            {
                rewardDays[i].button.interactable =
                    !claimProcessing &&
                    !claimed &&
                    i == currentClaimableDay;
            }
        }
    }

    private void EnsureRewardState()
    {
        ClearLegacyUnlockTicksKey();

        int currentClaimableDay = ES3.Load(CurrentClaimableDaySaveKey, -1);
        if (currentClaimableDay < 0 || currentClaimableDay >= rewardDays.Count)
        {
            ES3.Save(CurrentClaimableDaySaveKey, GetFirstUnclaimedDay());
            ES3.Save(LastClaimedUtcSaveKey, string.Empty);
        }
        if (IsClaimed(GetCurrentClaimableDay())
            && string.IsNullOrEmpty(ES3.Load<string>(LastClaimedUtcSaveKey, defaultValue: string.Empty)))
        {
            ES3.Save(LastClaimedUtcSaveKey, GetMostRecentUnlockUtc().AddSeconds(-1).ToString("O"));
        }
    }

    private void ClearLegacyUnlockTicksKey()
    {
        if (ES3.KeyExists(LegacyNextUnlockUtcTicksSaveKey))
        {
            ES3.DeleteKey(LegacyNextUnlockUtcTicksSaveKey);
        }
    }

    private int GetFirstUnclaimedDay()
    {
        for (int i = 0; i < rewardDays.Count; i++)
        {
            if (!IsClaimed(i))
            {
                return i;
            }
        }

        ResetRewardCycle();
        return 0;
    }

    private int GetCurrentClaimableDay()
    {
        int day = ES3.Load(CurrentClaimableDaySaveKey, 0);
        return Mathf.Clamp(day, 0, Mathf.Max(0, rewardDays.Count - 1));
    }

    private void AdvanceClaimableDayIfReady()
    {
        int currentClaimableDay = GetCurrentClaimableDay();
        if (!IsClaimed(currentClaimableDay))
        {
            return;
        }

        string claimedUtcText = ES3.Load<string>(LastClaimedUtcSaveKey, defaultValue: string.Empty);
        if (!TryLoadUtcDate(claimedUtcText, out DateTime claimedUtc))
        {
            return;
        }

        if (!HasUnlockBoundaryPassedSince(claimedUtc))
        {
            return;
        }

        int nextDay = currentClaimableDay + 1;
        if (nextDay >= rewardDays.Count)
        {
            ResetRewardCycle();
            return;
        }

        ES3.Save(CurrentClaimableDaySaveKey, nextDay);
        ES3.Save(LastClaimedUtcSaveKey, string.Empty);
    }

    private int GetFocusDay(int currentClaimableDay)
    {
        int focusDay = currentClaimableDay + 1;
        return focusDay < rewardDays.Count ? focusDay : -1;
    }

    private void ResetRewardCycle()
    {
        for (int i = 0; i < rewardDays.Count; i++)
        {
            ES3.Save(GetClaimedSaveKey(i), false);
        }

        ES3.Save(CurrentClaimableDaySaveKey, 0);
        ES3.Save(LastClaimedUtcSaveKey, string.Empty);
    }

    private bool IsClaimed(int dayIndex)
    {
        return ES3.Load(GetClaimedSaveKey(dayIndex), false);
    }

    private string GetClaimedSaveKey(int dayIndex)
    {
        return $"{ClaimedSaveKeyPrefix}{dayIndex + 1}_Claimed";
    }

    [ContextMenu("DEV Reset Daily Rewards")]
    private void DevResetDailyRewards()
    {
        ResetRewardCycle();
        RefreshState();
    }

    private void ClaimReward(int dayIndex)
    {
        if (claimProcessing ||
            dayIndex < 0 ||
            dayIndex >= rewardDays.Count)
        {
            return;
        }

        AdvanceClaimableDayIfReady();

        if (IsClaimed(dayIndex) || dayIndex != GetCurrentClaimableDay())
        {
            RefreshState();
            return;
        }

        RewardDay day = rewardDays[dayIndex];
        claimProcessing = true;

        if (!TryCompleteRewardTransaction(dayIndex, day))
        {
            claimProcessing = false;
            RefreshState();
            return;
        }

        claimProcessing = false;

        if (homeUIBinder != null)
            homeUIBinder.RefreshFromSave();

        RefreshState();
        OpenRewardPopup(day);
    }

    private bool TryCompleteRewardTransaction(int dayIndex, RewardDay day)
    {
        float previousCash = ES3.Load<float>(CashSaveKey, 0f);
        float previousGems = ES3.Load<float>(GemsSaveKey, 0f);
        bool previousClaimed = ES3.Load(GetClaimedSaveKey(dayIndex), false);
        string previousClaimedUtc = ES3.Load<string>(
            LastClaimedUtcSaveKey,
            defaultValue: string.Empty);

        float newCash = Mathf.Max(0f, previousCash) + Mathf.Max(0, day.cash);
        float newGems = Mathf.Max(0f, previousGems) + Mathf.Max(0, day.gems);
        string claimedUtc = DateTime.UtcNow.ToString("O");

        try
        {
            // Save both balances for every day. This keeps mixed rewards such as
            // Day 7 in one guarded transaction instead of independent partial grants.
            ES3.Save(CashSaveKey, newCash);
            ES3.Save(GemsSaveKey, newGems);
            ES3.Save(GetClaimedSaveKey(dayIndex), true);
            ES3.Save(LastClaimedUtcSaveKey, claimedUtc);
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                ES3.Save(CashSaveKey, previousCash);
                ES3.Save(GemsSaveKey, previousGems);
                ES3.Save(GetClaimedSaveKey(dayIndex), previousClaimed);
                ES3.Save(LastClaimedUtcSaveKey, previousClaimedUtc);
            }
            catch
            {
                // The consolidated error below covers the best-effort rollback too.
            }

            Debug.LogError(
                "DailyRewardsPage: Failed to save the Day " + (dayIndex + 1) +
                " reward. Previous Cash, Gems, and claim state were restored when possible. " +
                exception.Message,
                this);
            return false;
        }
    }

    private void ResolveHomeUIBinder()
    {
        if (homeUIBinder == null)
            homeUIBinder = GetComponentInParent<HomeUIBinder>();
    }

    private bool TryLoadUtcDate(string dateText, out DateTime utcDate)
    {
        if (DateTime.TryParse(
            dateText,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out DateTime parsedDate))
        {
            utcDate = parsedDate.Kind == DateTimeKind.Utc ? parsedDate : parsedDate.ToUniversalTime();
            return true;
        }

        utcDate = default;
        return false;
    }

    private bool HasUnlockBoundaryPassedSince(DateTime claimedUtc)
    {
        return GetMostRecentUnlockUtc() > claimedUtc;
    }

    private DateTime GetMostRecentUnlockUtc()
    {
        TimeSpan offset = TimeSpan.FromHours(unlockTimeZoneOffsetHours);
        DateTime nowUtc = DateTime.UtcNow;
        DateTime localNow = nowUtc + offset;
        DateTime localUnlockToday = localNow.Date
            .AddHours(Mathf.Clamp(unlockHour, 0, 23))
            .AddMinutes(Mathf.Clamp(unlockMinute, 0, 59));

        DateTime localMostRecentUnlock = localNow >= localUnlockToday
            ? localUnlockToday
            : localUnlockToday.AddDays(-1);

        return DateTime.SpecifyKind(localMostRecentUnlock - offset, DateTimeKind.Utc);
    }

    private void OpenRewardPopup(RewardDay day)
    {
        if (!EnsureRewardPopup())
        {
            return;
        }

        if (popupIcon != null)
        {
            Sprite rewardSprite = day.icon != null ? day.icon.sprite : FindRewardIcon(day.root)?.sprite;
            popupIcon.sprite = rewardSprite;
            popupIcon.enabled = popupIcon.sprite != null;
            popupIcon.preserveAspect = true;
        }

        if (popupRewardText != null)
        {
            popupRewardText.text = BuildRewardText(day);
        }

        rewardPopup.SetActive(true);
        rewardPopup.transform.SetAsLastSibling();
    }

    private void CloseRewardPopup()
    {
        if (rewardPopup != null)
        {
            rewardPopup.SetActive(false);
        }
    }

    private string BuildRewardText(RewardDay day)
    {
        if (day.cash > 0 && day.gems > 0)
        {
            return $"+{day.cash:N0} Cash\n+{day.gems:N0} Gems";
        }

        if (day.cash > 0)
        {
            return $"+{day.cash:N0} Cash";
        }

        return $"+{day.gems:N0} Gems";
    }

    private bool EnsureRewardPopup()
    {
        if (rewardPopup != null)
        {
            return true;
        }

        BindPopup();
        if (rewardPopup != null)
        {
            return true;
        }

        Debug.LogWarning("DailyRewardsPage: DailyRewardClaimPopup is missing from the scene.", this);
        return false;
    }
}
