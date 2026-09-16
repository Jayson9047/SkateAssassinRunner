using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AccuracyMeter
{
    /// <summary>
    /// Owns the complete level-end multiplier transaction: live preview, meter stop,
    /// rewarded-ad request, successful bonus grant, save, and navigation.
    /// </summary>
    public class AccuracyMeterCashPreviewExample : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private AccuracyMeterArcDriver meter;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text BonusCashText;
        [SerializeField] private TMP_Text BonusGemText;
        [SerializeField] private GameObject bonusGemPreviewRoot;
        [SerializeField] private TMP_Text totalEarnedCashText;
        [SerializeField] private TMP_Text totalEarnedGemsText;

        [Header("Data")]
        private int earnedCash;
        private int earnedGems;
        private int earnedStars;
        private bool levelSucceeded;

        [Tooltip("Index-aligned with partitions (P0..Pn). Example: [2,3,4,3,2]")]
        [SerializeField] private int[] multipliers = { 2, 3, 4, 3, 2 };

        [Header("Behavior")]
        [SerializeField] private bool lockOnStop = true;

        private bool _locked;
        private bool _rewardRequestPending;
        private bool _rewardClaimed;
        private bool _destroyed;
        private int _lastFocusedPartition = -1;
        private int _selectedMultiplier = 1;

        private bool GemsEligibleForMultiplier => levelSucceeded && earnedStars == 3;

        private void Awake()
        {
            if (meter != null)
            {
                meter.onHoverChanged.AddListener(OnHoverChanged);
                meter.onStopped.AddListener(OnStopped);
            }

            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopButtonClicked);

            PreviewIndex(0);
        }

        private void OnDestroy()
        {
            _destroyed = true;

            if (meter != null)
            {
                meter.onHoverChanged.RemoveListener(OnHoverChanged);
                meter.onStopped.RemoveListener(OnStopped);
            }

            if (stopButton != null)
                stopButton.onClick.RemoveListener(OnStopButtonClicked);
        }

        private void OnStopButtonClicked()
        {
            if (_rewardRequestPending || _rewardClaimed || (lockOnStop && _locked))
                return;

            meter?.RequestStop();
        }

        private void OnHoverChanged(int idx, string partitionName, float t01)
        {
            if (_locked || _rewardRequestPending || _rewardClaimed)
                return;

            if (idx >= 0 && idx != _lastFocusedPartition)
            {
                _lastFocusedPartition = idx;
                SkateRunnerAudioManager.PlayLevelEndMultiplierFocus();
            }

            PreviewIndex(idx);
        }

        private void OnStopped(AccuracyMeterArcDriver.AccuracyStopResult result)
        {
            if (!result.IsValid || _rewardRequestPending || _rewardClaimed)
                return;

            _locked = true;
            _selectedMultiplier = GetMultiplier(result.PartitionIndex);
            PreviewMultiplier(_selectedMultiplier);

            _rewardRequestPending = true;
            SetTransactionControlsInteractable(false);

            bool requestAccepted = RewardedAdBridge.ShowRewardedAd(
                "level_end_multiplier",
                CompleteRewardedMultiplier,
                CancelRewardedMultiplier);

            if (!requestAccepted && _rewardRequestPending)
                CancelRewardedMultiplier();
        }

        private void CompleteRewardedMultiplier()
        {
            if (_destroyed || !_rewardRequestPending || _rewardClaimed)
                return;

            _rewardRequestPending = false;
            _rewardClaimed = true;

            CalculateRewardAmounts(
                _selectedMultiplier,
                out int finalCash,
                out int cashBonus,
                out int finalGems,
                out int gemBonus);

            var gameManager = MoreMountains.InfiniteRunnerEngine.SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (cashBonus > 0)
                gameManager?.AddCash(cashBonus);
            if (gemBonus > 0)
                gameManager?.AddGems(gemBonus);

            SetPreviewText(finalCash, finalGems);
            if (totalEarnedCashText != null)
                totalEarnedCashText.text = SkateLocalization.FormatNumber(finalCash);
            if (totalEarnedGemsText != null)
                totalEarnedGemsText.text = SkateLocalization.FormatNumber(finalGems);

            gameManager?.SaveAfterLevelEnd(levelSucceeded);
            MoreMountains.InfiniteRunnerEngine.LevelManager.Instance?.GotoLevel("SkateRunnerStartScreen");
        }

        private void CancelRewardedMultiplier()
        {
            if (_destroyed || !_rewardRequestPending || _rewardClaimed)
                return;

            _rewardRequestPending = false;
            _locked = false;
            SetTransactionControlsInteractable(true);
            meter?.ResumeMeter();
        }

        private void SetTransactionControlsInteractable(bool interactable)
        {
            if (stopButton != null)
                stopButton.interactable = interactable;
            if (continueButton != null)
                continueButton.interactable = interactable;
        }

        public void SetRewardContext(int cash, int gems, int stars, bool success)
        {
            earnedCash = Mathf.Max(0, cash);
            earnedGems = Mathf.Max(0, gems);
            earnedStars = Mathf.Max(0, stars);
            levelSucceeded = success;

            _locked = false;
            _rewardRequestPending = false;
            _rewardClaimed = false;
            _lastFocusedPartition = -1;
            _selectedMultiplier = 1;

            SetTransactionControlsInteractable(true);
            SetGemPreviewVisible(GemsEligibleForMultiplier);
            PreviewIndex(0);
        }

        public void Preview_P0() => PreviewIndex(0);
        public void Preview_P1() => PreviewIndex(1);
        public void Preview_P2() => PreviewIndex(2);
        public void Preview_P3() => PreviewIndex(3);
        public void Preview_P4() => PreviewIndex(4);

        public void UnlockPreview()
        {
            if (_rewardRequestPending || _rewardClaimed)
                return;

            _locked = false;
            SetTransactionControlsInteractable(true);
            meter?.ResumeMeter();
        }

        public void PreviewIndex(int idx)
        {
            PreviewMultiplier(GetMultiplier(idx));
        }

        private void PreviewMultiplier(int multiplier)
        {
            CalculateRewardAmounts(multiplier, out int finalCash, out _, out int finalGems, out _);
            SetPreviewText(finalCash, finalGems);
        }

        private void CalculateRewardAmounts(
            int multiplier,
            out int finalCash,
            out int cashBonus,
            out int finalGems,
            out int gemBonus)
        {
            int safeMultiplier = Mathf.Max(1, multiplier);
            finalCash = earnedCash * safeMultiplier;
            cashBonus = Mathf.Max(0, finalCash - earnedCash);

            finalGems = GemsEligibleForMultiplier ? earnedGems * safeMultiplier : earnedGems;
            gemBonus = Mathf.Max(0, finalGems - earnedGems);
        }

        private int GetMultiplier(int idx)
        {
            if (multipliers == null || multipliers.Length == 0) return 1;
            if (idx < 0 || idx >= multipliers.Length) return 1;
            return Mathf.Max(1, multipliers[idx]);
        }

        private void SetPreviewText(int cashValue, int gemValue)
        {
            if (BonusCashText != null)
                BonusCashText.text = cashValue.ToString();
            if (BonusGemText != null)
                BonusGemText.text = gemValue.ToString();

            SetGemPreviewVisible(GemsEligibleForMultiplier);
        }

        private void SetGemPreviewVisible(bool visible)
        {
            if (bonusGemPreviewRoot != null && bonusGemPreviewRoot.activeSelf != visible)
                bonusGemPreviewRoot.SetActive(visible);
        }

        // Preserved for existing external/Inspector compatibility.
        public void SetEarnedCash(int value)
        {
            earnedCash = Mathf.Max(0, value);
            if (!_locked && !_rewardRequestPending && !_rewardClaimed)
                PreviewIndex(0);
        }
    }
}
