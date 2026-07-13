using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LuckySpinWheel : MonoBehaviour
{
    public enum RewardType
    {
        Cash,
        Gem
    }

    [System.Serializable]
    public class SpinReward
    {
        public string rewardName;
        public RewardType rewardType;
        public int amount;

        [Tooltip("Relative chance. Example: 34 means 34 weight points. Total does not need to equal 100.")]
        public float probabilityWeight;

        [Tooltip("Optional: assign the slot object for your own reference.")]
        public RectTransform slotObject;
    }

    [Header("Wheel Setup")]
    [SerializeField] private RectTransform wheelRect;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button dailySpinButton;
    [SerializeField] private List<SpinReward> rewards = new List<SpinReward>();

    [Header("Daily Spin Rules")]
    [SerializeField] private int maxDailyAdSpins = 8;
    [SerializeField] private int spinsEarnedPerAd = 1;

    [Header("Landing Setup")]
    [SerializeField] private float arrowAngle = 90f;
    [SerializeField] private float startingSlotAngle = 90f;
    [SerializeField] private bool slotsGoClockwise = true;
    [SerializeField] private bool randomizeInsideSlice = true;

    [Header("Spin Feel")]
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minFullSpins = 5;
    [SerializeField] private int maxFullSpins = 8;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Currency Save Keys")]
    [SerializeField] private string cashSaveKey = "TotalCash";
    [SerializeField] private string gemSaveKey = "TotalGems";

    [Header("Spin Save Keys")]
    [SerializeField] private string availableSpinsSaveKey = "LuckySpin_AvailableSpins";
    [SerializeField] private string dailyAdsWatchedSaveKey = "LuckySpin_DailyAdsWatched";
    [SerializeField] private string dailyResetDateSaveKey = "LuckySpin_DailyResetDate";

    [Header("Optional UI")]
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private TMP_Text rewardResultText;
    [SerializeField] private TMP_Text spinsLeftText;
    [SerializeField] private TMP_Text dailySpinCountText;

    [Header("Reward Popup")]
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private Image rewardPopupIcon;
    [SerializeField] private TMP_Text rewardPopupText;
    [SerializeField] private Button rewardPopupOkButton;

    private bool isSpinning;

    private void OnDisable()
    {
        CloseRewardPopup();
    }

    private void Awake()
    {
        CheckDailyReset();

        if (spinButton != null)
            spinButton.onClick.AddListener(Spin);

        if (dailySpinButton != null)
            dailySpinButton.onClick.AddListener(EarnSpinFromAd);

        BindRewardPopup();

        RefreshCurrencyUI();
        RefreshSpinUI();
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        string savedDate = ES3.Load<string>(
            dailyResetDateSaveKey,
            defaultValue: ""
        );

        if (savedDate != today)
        {
            ES3.Save(dailyResetDateSaveKey, today);
            ES3.Save(dailyAdsWatchedSaveKey, 0);
        }
    }

    public void EarnSpinFromAd()
    {
        CheckDailyReset();

        int adsWatchedToday = ES3.Load<int>(
            dailyAdsWatchedSaveKey,
            defaultValue: 0
        );

        if (adsWatchedToday >= maxDailyAdSpins)
        {
            Debug.Log("Lucky Spin: Daily ad spin limit reached.");
            RefreshSpinUI();
            return;
        }

        // Adhook goes here.
        // Show rewarded ad here. Only run the code below after the ad is successfully completed.

        adsWatchedToday++;

        int availableSpins = ES3.Load<int>(
            availableSpinsSaveKey,
            defaultValue: 0
        );
        availableSpins += spinsEarnedPerAd;

        ES3.Save(dailyAdsWatchedSaveKey, adsWatchedToday);
        ES3.Save(availableSpinsSaveKey, availableSpins);

        RefreshSpinUI();

        Debug.Log($"Lucky Spin: Earned {spinsEarnedPerAd} spin. Available spins: {availableSpins}");
    }

    public void Spin()
    {
        if (isSpinning) return;

        CheckDailyReset();

        int availableSpins = ES3.Load<int>(availableSpinsSaveKey, 0);

        if (availableSpins <= 0)
        {
            Debug.Log("Lucky Spin: No spins available. Watch an ad first.");
            RefreshSpinUI();
            return;
        }

        if (wheelRect == null)
        {
            Debug.LogError("LuckySpinWheel: Wheel Rect is missing.");
            return;
        }

        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogError("LuckySpinWheel: No rewards added.");
            return;
        }

        availableSpins--;
        ES3.Save(availableSpinsSaveKey, availableSpins);

        RefreshSpinUI();

        SpinReward selectedReward = PickRewardByProbability();
        int selectedIndex = rewards.IndexOf(selectedReward);

        StartCoroutine(SpinRoutine(selectedIndex, selectedReward));
    }

    private IEnumerator SpinRoutine(int selectedIndex, SpinReward selectedReward)
    {
        isSpinning = true;
        RefreshSpinUI();

        float slotAngle = 360f / rewards.Count;

        float selectedSlotAngle = startingSlotAngle;

        if (slotsGoClockwise)
            selectedSlotAngle -= selectedIndex * slotAngle;
        else
            selectedSlotAngle += selectedIndex * slotAngle;

        float sliceOffset = 0f;

        if (randomizeInsideSlice)
            sliceOffset = UnityEngine.Random.Range(-slotAngle * 0.35f, slotAngle * 0.35f);

        float currentZ = NormalizeAngle(wheelRect.eulerAngles.z);

        float targetZ = arrowAngle - selectedSlotAngle + sliceOffset;
        targetZ = NormalizeAngle(targetZ);

        float spins = UnityEngine.Random.Range(minFullSpins, maxFullSpins + 1) * 360f;

        float deltaToTarget = Mathf.DeltaAngle(currentZ, targetZ);
        float finalZ = currentZ + spins + deltaToTarget;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / spinDuration);
            float curvedT = spinCurve.Evaluate(t);

            float z = Mathf.Lerp(currentZ, finalZ, curvedT);
            wheelRect.rotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        wheelRect.rotation = Quaternion.Euler(0f, 0f, finalZ);

        GiveReward(selectedReward);

        isSpinning = false;
        RefreshSpinUI();
    }

    private SpinReward PickRewardByProbability()
    {
        float totalWeight = 0f;

        foreach (SpinReward reward in rewards)
        {
            if (reward != null && reward.probabilityWeight > 0)
                totalWeight += reward.probabilityWeight;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (SpinReward reward in rewards)
        {
            if (reward == null || reward.probabilityWeight <= 0) continue;

            currentWeight += reward.probabilityWeight;

            if (randomValue <= currentWeight)
                return reward;
        }

        return rewards[0];
    }

    private void GiveReward(SpinReward reward)
    {
        if (reward.rewardType == RewardType.Cash)
        {
            float currentCash = ES3.Load<float>(
                cashSaveKey,
                defaultValue: 0f
            );
            currentCash += reward.amount;

            ES3.Save(cashSaveKey, currentCash);

            if (cashText != null)
                cashText.text = currentCash.ToString("N0");
        }
        else if (reward.rewardType == RewardType.Gem)
        {
            float currentGems = ES3.Load<float>(
                gemSaveKey,
                defaultValue: 0f
            );
            currentGems += reward.amount;

            ES3.Save(gemSaveKey, currentGems);

            if (gemText != null)
                gemText.text = currentGems.ToString("N0");
        }

        if (rewardResultText != null)
            rewardResultText.text = $"+{reward.amount} {reward.rewardType}";

        ShowRewardPopup(reward);

        Debug.Log($"Lucky Spin Saved Reward: {reward.rewardType} +{reward.amount}");
    }

    private void BindRewardPopup()
    {
        if (rewardPopup != null)
            rewardPopup.SetActive(false);

        if (rewardPopupOkButton != null)
        {
            rewardPopupOkButton.onClick.RemoveListener(CloseRewardPopup);
            rewardPopupOkButton.onClick.AddListener(CloseRewardPopup);
        }
    }

    private void ShowRewardPopup(SpinReward reward)
    {
        if (rewardPopup == null || reward == null)
            return;

        if (rewardPopupIcon != null)
        {
            rewardPopupIcon.sprite = FindRewardSprite(reward);
            rewardPopupIcon.enabled = rewardPopupIcon.sprite != null;
            rewardPopupIcon.preserveAspect = true;
        }

        if (rewardPopupText != null)
        {
            string currencyName = reward.rewardType == RewardType.Gem ? "GEMS" : "CASH";
            rewardPopupText.text = $"YOU WON\n+{reward.amount:N0} {currencyName}!";
        }

        rewardPopup.SetActive(true);
        rewardPopup.transform.SetAsLastSibling();
    }

    private Sprite FindRewardSprite(SpinReward reward)
    {
        if (reward.slotObject == null)
            return null;

        Image fallback = null;
        Image[] images = reward.slotObject.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
                continue;

            if (image.name == "Item")
                return image.sprite;

            if (fallback == null)
                fallback = image;
        }

        return fallback != null ? fallback.sprite : null;
    }

    private void CloseRewardPopup()
    {
        if (rewardPopup != null)
            rewardPopup.SetActive(false);
    }

    private void RefreshCurrencyUI()
    {
        float cash = ES3.Load<float>(cashSaveKey, 0);
        float gems = ES3.Load<float>(gemSaveKey, 0);

        if (cashText != null)
            cashText.text = cash.ToString("N0");

        if (gemText != null)
            gemText.text = gems.ToString("N0");
    }

    private void RefreshSpinUI()
    {
        CheckDailyReset();

        int availableSpins = ES3.Load<int>(availableSpinsSaveKey, 0);
        int adsWatchedToday = ES3.Load<int>(dailyAdsWatchedSaveKey, 0);

        if (spinsLeftText != null)
            spinsLeftText.text = availableSpins + " Left";

        if (dailySpinCountText != null)
            dailySpinCountText.text = $"{maxDailyAdSpins - adsWatchedToday}/{maxDailyAdSpins}";

        if (spinButton != null)
            spinButton.interactable = !isSpinning && availableSpins > 0;

        if (dailySpinButton != null)
            dailySpinButton.interactable = adsWatchedToday < maxDailyAdSpins;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}
