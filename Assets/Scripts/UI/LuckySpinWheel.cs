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
    [SerializeField] private List<SpinReward> rewards = new List<SpinReward>();

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

    [Header("Optional UI")]
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private TMP_Text rewardResultText;

    private bool isSpinning;

    private void Awake()
    {
        if (spinButton != null)
            spinButton.onClick.AddListener(Spin);

        RefreshCurrencyUI();
    }

    public void Spin()
    {
        if (isSpinning) return;
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

        SpinReward selectedReward = PickRewardByProbability();
        int selectedIndex = rewards.IndexOf(selectedReward);

        StartCoroutine(SpinRoutine(selectedIndex, selectedReward));
    }

    private IEnumerator SpinRoutine(int selectedIndex, SpinReward selectedReward)
    {
        isSpinning = true;

        if (spinButton != null)
            spinButton.interactable = false;

        float slotAngle = 360f / rewards.Count;

        float selectedSlotAngle = startingSlotAngle;

        if (slotsGoClockwise)
            selectedSlotAngle -= selectedIndex * slotAngle;
        else
            selectedSlotAngle += selectedIndex * slotAngle;

        float sliceOffset = 0f;

        if (randomizeInsideSlice)
            sliceOffset = Random.Range(-slotAngle * 0.35f, slotAngle * 0.35f);

        float currentZ = NormalizeAngle(wheelRect.eulerAngles.z);

        float targetZ = arrowAngle - selectedSlotAngle + sliceOffset;
        targetZ = NormalizeAngle(targetZ);

        float spins = Random.Range(minFullSpins, maxFullSpins + 1) * 360f;

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

        if (spinButton != null)
            spinButton.interactable = true;

        isSpinning = false;
    }

    private SpinReward PickRewardByProbability()
    {
        float totalWeight = 0f;

        foreach (SpinReward reward in rewards)
        {
            if (reward != null && reward.probabilityWeight > 0)
                totalWeight += reward.probabilityWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
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
            float currentCash = ES3.Load<float>(cashSaveKey, 0);
            currentCash += reward.amount;

            ES3.Save(cashSaveKey, currentCash);
            ES3.StoreCachedFile();

            if (cashText != null)
                cashText.text = currentCash.ToString("N0");
        }
        else if (reward.rewardType == RewardType.Gem)
        {
            float currentGems = ES3.Load<float>(gemSaveKey, 0);
            currentGems += reward.amount;

            ES3.Save(gemSaveKey, currentGems);
            ES3.StoreCachedFile();

            if (gemText != null)
                gemText.text = currentGems.ToString("N0");
        }

        if (rewardResultText != null)
            rewardResultText.text = $"+{reward.amount} {reward.rewardType}";

        Debug.Log($"Lucky Spin Saved Reward: {reward.rewardType} +{reward.amount}");
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

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}