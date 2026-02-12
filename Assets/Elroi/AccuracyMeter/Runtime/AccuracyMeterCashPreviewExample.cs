using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AccuracyMeter
{
    /// <summary>
    /// Example consumer:
    /// - While hovering partitions, preview earnedCash * multiplier (index-aligned array)
    /// - On stop, lock the preview at landed partition
    ///
    /// This is intentionally NOT inside the driver, because it's game/business logic.
    /// </summary>
    public class AccuracyMeterCashPreviewExample : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private AccuracyMeterArcDriver meter;
        [SerializeField] private Button stopButton;
        [SerializeField] private TMP_Text BonusCashText;
        [SerializeField] private TMP_Text totalEarnedCashText; // drag LevelEndCashEarnedText here

        [Header("Data")]
        private int earnedCash;

        [Tooltip("Index-aligned with partitions (P0..Pn). Example: [2,3,4,3,2]")]
        [SerializeField] private int[] multipliers = { 2, 3, 4, 3, 2 };

        [Header("Behavior")]
        [SerializeField] private bool lockOnStop = true;

        private bool _locked;

        private void Awake()
        {
            if (meter != null)
            {
                // global events (optional if you choose per-partition routing in inspector)
                meter.onHoverChanged.AddListener(OnHoverChanged);
                meter.onStopped.AddListener(OnStopped);
            }

            if (stopButton != null)
            {
                stopButton.onClick.AddListener(() =>
                {
                    if (lockOnStop && _locked) return;
                    meter?.RequestStop();
                });
            }

            // initial display
            PreviewIndex(0);
        }

        private void OnDestroy()
        {
            if (meter != null)
            {
                meter.onHoverChanged.RemoveListener(OnHoverChanged);
                meter.onStopped.RemoveListener(OnStopped);
            }

            if (stopButton != null)
                stopButton.onClick.RemoveAllListeners();
        }

        // -------------------------
        // Event handlers (global)
        // -------------------------

        private void OnHoverChanged(int idx, string partitionName, float t01)
        {
            if (lockOnStop && _locked) return;
            PreviewIndex(idx);
        }

        private void OnStopped(AccuracyMeterArcDriver.AccuracyStopResult result)
        {
            if (!result.IsValid) return;

            if (lockOnStop)
                _locked = true;

            int mult = GetMultiplier(result.PartitionIndex);

            int baseCash = earnedCash;
            int finalCash = baseCash * mult;
            int bonusCash = finalCash - baseCash;

            // lock UI preview to the landed partition result
            SetText(finalCash);

            // TODO: show rewarded ad here.
            // Only proceed to grant bonusCash if ad success.

            // update the "Total Earned Cash" text on the level end screen
            if (totalEarnedCashText != null)
                totalEarnedCashText.text = finalCash.ToString("0");

            if (bonusCash > 0)
                MoreMountains.InfiniteRunnerEngine.SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.AddCash(bonusCash);

            bool success = MoreMountains.InfiniteRunnerEngine.SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null
                ? MoreMountains.InfiniteRunnerEngine.SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.LastLevelSuccess
                : true;

            MoreMountains.InfiniteRunnerEngine.SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.SaveAfterLevelEnd(success);

            // Go home (don’t use LevelSelector unless you want to; this keeps it self-contained)
            MoreMountains.InfiniteRunnerEngine.LevelManager.Instance?.GotoLevel("SkateRunnerStartScreen");
        }

        // -------------------------
        // Public methods for Inspector wiring
        // (Use these with per-partition UnityEvents)
        // -------------------------

        public void Preview_P0() => PreviewIndex(0);
        public void Preview_P1() => PreviewIndex(1);
        public void Preview_P2() => PreviewIndex(2);
        public void Preview_P3() => PreviewIndex(3);
        public void Preview_P4() => PreviewIndex(4);

        public void UnlockPreview() => _locked = false;

        // -------------------------
        // Core preview logic
        // -------------------------

        public void PreviewIndex(int idx)
        {
            int mult = GetMultiplier(idx);
            int preview = earnedCash * mult;
            SetText(preview);
        }

        private int GetMultiplier(int idx)
        {
            if (multipliers == null || multipliers.Length == 0) return 1;
            if (idx < 0 || idx >= multipliers.Length) return 1;
            return Mathf.Max(1, multipliers[idx]);
        }

        private void SetText(int value)
        {
            if (BonusCashText != null)
                BonusCashText.text = value.ToString();
        }

        // Optional: call this from your game when earned cash changes mid-run
        public void SetEarnedCash(int value)
        {
            earnedCash = Mathf.Max(0, value);

            // refresh current preview if not locked
            if (!lockOnStop || !_locked)
                PreviewIndex(0);
        }
    }
}
