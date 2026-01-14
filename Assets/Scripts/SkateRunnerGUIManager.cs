using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Tools;
using IndieKit;
using TMPro;
using System.Collections;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Handles all GUI effects and changes
    /// </summary>
    public class SkateRunnerGUIManager : GUIManager, MMEventListener<MMGameEvent>
    {
        [Header("Bounties")]
        [Tooltip("the bounties counter")]
        public Text BountiesText;

        [Header("Phase Timer")]
        [Tooltip("Top-of-screen phase timer text (0..60).")]
        public TextMeshProUGUI PhaseTimerText;

        public static SkateRunnerGUIManager SkateRunnerGUIManagerAccessor { get; private set; }

        [Header("Slam Meter (Shards)")]
        [Tooltip("Assign the 5 shard Image components here (left to right).")]
        [SerializeField] private Image[] SlamShardImages;

        [Tooltip("Shadowy / empty shard sprite (your dark transparent one).")]
        [SerializeField] private Sprite ShardEmptySprite;

        [Tooltip("Filled shard sprite (blue).")]
        [SerializeField] private Sprite ShardFilledBlueSprite;

        [Tooltip("Ready shard sprite (red).")]
        [SerializeField] private Sprite ShardFilledRedSprite;

        [Tooltip("Activated Downslam button")]
        [SerializeField] private Sprite DownslamSprite;

        [Tooltip("Downslam button Image component")]
        [SerializeField] private Image DownslamButtonImage;

        [Tooltip("Disabled Downslam button sprite (black & white)")]
        [SerializeField] private Sprite DownslamDisabledSprite;

        [SerializeField] private Button DownslamButton;

        [Tooltip("How many shards are required to become 'Ready'.")]
        [SerializeField] private int MaxSlamCharges = 5;

        [Header("Phase 2 SlamHUD Transition")]
        [SerializeField] private RectTransform SlamHUDRoot;          // SlamHUD parent (optional)
        [SerializeField] private CanvasGroup SlamButtonGroup;        // CanvasGroup on the SlamButton object (auto-added if missing)
        [SerializeField] private CanvasGroup PlatformGroup;          // CanvasGroup on Platform (auto-added if missing)
        [SerializeField] private CanvasGroup ShardsGroup;            // CanvasGroup on Shards (auto-added if missing)

        [SerializeField] private RectTransform SlamButtonRect;       // RectTransform of SlamButton
        [SerializeField] private Vector2 Phase2SlamButtonAnchoredPos = new Vector2(0f, -360f);
        [SerializeField] private float Phase2TransitionDuration = 0.35f;

        [Header("Phase 2 Slam Button -> Simulated Combo")]
        [SerializeField] private float phase2DoubleTapGap = 0.10f;      // small but noticeable
        [SerializeField] private float phase2AfterSecondTapDelay = 0.05f;
        [SerializeField] private float phase2WaitForAirTimeout = 0.35f; // fail-safe
        [SerializeField] private PowerMeter powerMeter;

        private bool _phase2SimInProgress;
        private SwipeDownDetector _swipeDownDetectorCached;
        private Jumper _jumperCached;

        private Vector2 _slamButtonOriginalAnchoredPos;
        private bool _cachedOriginalPos;
        private Coroutine _phase2HudRoutine;


        private int _currentSlamCharges;

        protected override void Awake()
        {
            base.Awake(); // VERY important for MM
            SkateRunnerGUIManagerAccessor = this;

            // Optional: initialize UI on load
            SetSlamCharge(0);
            CacheSlamHUDReferencesIfNeeded();
            if (DownslamButton != null)
            {
                DownslamButton.onClick.RemoveListener(OnDownslamButtonClicked);
                DownslamButton.onClick.AddListener(OnDownslamButtonClicked);
                if (powerMeter != null)
                {
                    // Safety: remove first in case of domain reload / prefab reuse
                    powerMeter.OnResult.RemoveListener(OnPowerMeterResult);
                    powerMeter.OnResult.AddListener(OnPowerMeterResult);
                }
            }
        }

        /// <summary>
        /// Sets the text to the game manager's points.
        /// </summary>
        public virtual void RefreshBounties()
        {
            var skateRunnerGameManager = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (BountiesText == null)
                return;

            BountiesText.text = skateRunnerGameManager.Bounties.ToString("000000");
        }

        /// <summary>
        /// Slam meter API — call this when enemies die.
        /// </summary>
        public void AddSlamCharge(int amount = 1)
        {
            SetSlamCharge(_currentSlamCharges + amount);
        }

        public void ResetSlamCharge()
        {
            SetSlamCharge(0);
        }
        private void CacheSlamHUDReferencesIfNeeded()
        {
            // If you assign in inspector, we use that.
            // If not, we try to find by names you told me exist: SlamHUD -> Platform, Shards, SlamButton.
            if (SlamHUDRoot == null)
            {
                var slamHudGo = GameObject.Find("SlamHUD");
                if (slamHudGo != null)
                    SlamHUDRoot = slamHudGo.GetComponent<RectTransform>();
            }

            if (SlamHUDRoot != null)
            {
                if (PlatformGroup == null)
                {
                    var t = SlamHUDRoot.Find("Platform");
                    if (t != null) PlatformGroup = GetOrAddCanvasGroup(t.gameObject);
                }

                if (ShardsGroup == null)
                {
                    var t = SlamHUDRoot.Find("Shards");
                    if (t != null) ShardsGroup = GetOrAddCanvasGroup(t.gameObject);
                }

                if (SlamButtonRect == null)
                {
                    var t = SlamHUDRoot.Find("SlamButton");
                    if (t != null) SlamButtonRect = t.GetComponent<RectTransform>();
                }

                if (SlamButtonRect != null && SlamButtonGroup == null)
                {
                    SlamButtonGroup = GetOrAddCanvasGroup(SlamButtonRect.gameObject);
                }
            }

            if (!_cachedOriginalPos && SlamButtonRect != null)
            {
                _slamButtonOriginalAnchoredPos = SlamButtonRect.anchoredPosition;
                _cachedOriginalPos = true;
            }
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        // ------------------------------------------------------
        // NEW: Phase 2 HUD override (only Slam button should work)
        // ------------------------------------------------------
        public void EnterPhase2BossHUD()
        {
            CacheSlamHUDReferencesIfNeeded();

            // Ensure slam button exists and is enabled
            if (DownslamButton != null)
            {
                DownslamButton.gameObject.SetActive(true);
                DownslamButton.interactable = true; // override: Phase 2 wants it clickable regardless of shard state
            }

            if (DownslamButtonImage != null && DownslamSprite != null)
            {
                DownslamButtonImage.sprite = DownslamSprite; // make it look "active"
            }

            // Start transition animation
            if (_phase2HudRoutine != null)
                StopCoroutine(_phase2HudRoutine);

            _phase2HudRoutine = StartCoroutine(Phase2HudTransitionCo());
            powerMeter.gameObject.SetActive(true);
            powerMeter.StartMeter();
        }
        private IEnumerator Phase2HudTransitionCo()
        {
            float dur = Mathf.Max(0.01f, Phase2TransitionDuration);

            // If references aren't found, fail gracefully (no crash)
            if (SlamButtonRect == null || SlamButtonGroup == null)
                yield break;

            if (!_cachedOriginalPos)
            {
                _slamButtonOriginalAnchoredPos = SlamButtonRect.anchoredPosition;
                _cachedOriginalPos = true;
            }

            // Start values
            float buttonA0 = SlamButtonGroup.alpha;
            float platA0 = PlatformGroup != null ? PlatformGroup.alpha : 1f;
            float shardsA0 = ShardsGroup != null ? ShardsGroup.alpha : 1f;

            Vector2 pos0 = SlamButtonRect.anchoredPosition;
            Vector2 pos1 = Phase2SlamButtonAnchoredPos;

            // Make sure button can receive clicks while fading
            SlamButtonGroup.interactable = true;
            SlamButtonGroup.blocksRaycasts = true;

            // Fade OUT platform/shards so they don’t eat clicks
            if (PlatformGroup != null) { PlatformGroup.interactable = false; PlatformGroup.blocksRaycasts = false; }
            if (ShardsGroup != null) { ShardsGroup.interactable = false; ShardsGroup.blocksRaycasts = false; }

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / dur);
                // smoothstep
                a = a * a * (3f - 2f * a);

                // Button fades in + moves to bottom-middle
                SlamButtonGroup.alpha = Mathf.Lerp(buttonA0, 1f, a);
                SlamButtonRect.anchoredPosition = Vector2.Lerp(pos0, pos1, a);

                // Platform + shards fade out
                if (PlatformGroup != null) PlatformGroup.alpha = Mathf.Lerp(platA0, 0f, a);
                if (ShardsGroup != null) ShardsGroup.alpha = Mathf.Lerp(shardsA0, 0f, a);

                yield return null;
            }

            // Snap end
            SlamButtonGroup.alpha = 1f;
            SlamButtonRect.anchoredPosition = pos1;

            if (PlatformGroup != null)
            {
                PlatformGroup.alpha = 0f;
                PlatformGroup.gameObject.SetActive(false); // fully hide
            }
            if (ShardsGroup != null)
            {
                ShardsGroup.alpha = 0f;
                ShardsGroup.gameObject.SetActive(false); // fully hide
            }
        }

        public void RestartPhase2PowerMeter()
        {
            if (powerMeter == null)
                return;

            powerMeter.gameObject.SetActive(true);
            powerMeter.ResetTickerTo(Random.value); // or 0.5f if you want consistency
            powerMeter.StartMeter();
        }

        private void OnPowerMeterResult(PowerMeter.ZoneResult result, float normalized)
        {
            switch (result)
            {
                case PowerMeter.ZoneResult.Red:
                {
                    var player = FindFirstObjectByType<PlayerPhase2Controller>();
                    if (player != null)
                    {
                        player.TriggerPhase2RedFail();
                    }
                    else
                    {
                        Debug.LogError("[Phase2] PlayerPhase2Controller not found.");
                    }
                    break;
                }

                case PowerMeter.ZoneResult.Yellow:
                case PowerMeter.ZoneResult.Green:
                case PowerMeter.ZoneResult.Cyan:
                {
                    StartCoroutine(SimulateDoubleJumpThenDownAttack());
                    // success path (already working / later work)
                    break;
                }
            }
        }


        public void RefreshPhase2Countdown(int secondsRemaining)
        {
            if (PhaseTimerText == null) return;
            secondsRemaining = Mathf.Clamp(secondsRemaining, 0, 99);
            PhaseTimerText.text = $"{secondsRemaining:00}";
        }

        private void OnDownslamButtonClicked()
        {
            // In Phase 2 you said all controls are locked, and ONLY this button works.
            // So we simulate the correct combo regardless of lock state.
            if (_phase2SimInProgress) return;
            powerMeter.StopMeterAndEvaluate();
        }

        private IEnumerator SimulateDoubleJumpThenDownAttack()
        {
            _phase2SimInProgress = true;

            // Cache player components once
            if (_swipeDownDetectorCached == null)
                _swipeDownDetectorCached = FindFirstObjectByType<SwipeDownDetector>();

            if (_jumperCached == null)
                _jumperCached = FindFirstObjectByType<Jumper>();

            // 1) First tap (jump)
            FireMainActionTap();

            // small gap so it feels like a real double-tap (not a robotic double-fire)
            yield return new WaitForSeconds(phase2DoubleTapGap);

            // 2) Second tap (double jump)
            FireMainActionTap();

            // tiny delay before slam
            yield return new WaitForSeconds(phase2AfterSecondTapDelay);

            // 3) Wait until actually airborne (important)
            float t = 0f;
            while (t < phase2WaitForAirTimeout)
            {
                if (_jumperCached != null && !_jumperCached.IsGrounded)
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            // 4) Trigger down attack (same logic as swipe-down in-air)
            if (_swipeDownDetectorCached != null && _jumperCached != null && !_jumperCached.IsGrounded)
            {
                // This calls your existing DownAttackRoutine without needing a real swipe
                // and it works even when GameplayInputsLocked is true.
                _swipeDownDetectorCached.TriggerDownAttackFromBuffer();
            }

            _phase2SimInProgress = false;
        }

        private void FireMainActionTap()
        {
            // This is the same pathway your TapOnlyMainActionZone uses
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SendMessage("MainActionButtonDown");
                InputManager.Instance.SendMessage("MainActionButtonUp");
            }
        }


        /// <summary>
        /// Sets slam charge count and refreshes shard sprites.
        /// 0..Max-1 => filled blue up to count, rest empty
        /// Max => all red
        /// </summary>
        public void SetSlamCharge(int newChargeCount)
        {
            if (MaxSlamCharges <= 0) MaxSlamCharges = 5;

            _currentSlamCharges = Mathf.Clamp(newChargeCount, 0, MaxSlamCharges);
            RefreshSlamShards();
        }

        public void RefreshPhaseTimer(int elapsedSeconds, int phase1DurationSeconds, bool phase2Active)
        {
            if (PhaseTimerText == null) return;

            if (phase2Active)
            {
                PhaseTimerText.text = "PHASE 2";
                return;
            }

            elapsedSeconds = Mathf.Clamp(elapsedSeconds, 0, phase1DurationSeconds);
            PhaseTimerText.text = $"{phase1DurationSeconds - elapsedSeconds:00}";
        }


        private void RefreshSlamShards()
        {
            if (SlamShardImages == null || SlamShardImages.Length == 0)
                return;

            bool isReady = (_currentSlamCharges >= MaxSlamCharges);

            // Handle shards
            for (int i = 0; i < SlamShardImages.Length; i++)
            {
                var img = SlamShardImages[i];
                if (img == null) continue;

                if (isReady)
                {
                    if (ShardFilledRedSprite != null)
                        img.sprite = ShardFilledRedSprite;
                }
                else
                {
                    bool shouldBeFilled = (i < _currentSlamCharges);

                    if (shouldBeFilled)
                    {
                        if (ShardFilledBlueSprite != null)
                            img.sprite = ShardFilledBlueSprite;
                    }
                    else
                    {
                        if (ShardEmptySprite != null)
                            img.sprite = ShardEmptySprite;
                    }
                }

                img.enabled = true;
            }

            // Handle slam button state
            if (DownslamButtonImage != null)
            {
                DownslamButtonImage.sprite = isReady
                    ? DownslamSprite
                    : DownslamDisabledSprite;
            }

            if (DownslamButton != null)
            {
                DownslamButton.interactable = isReady;
            }

        }
        public bool IsSlamReady()
        {
            return _currentSlamCharges >= MaxSlamCharges;
        }

        /// <summary>
        /// If slam is ready, consumes it (resets shards + button via SetSlamCharge(0))
        /// and returns true. Otherwise returns false.
        /// </summary>
        public bool ConsumeSlamIfReady()
        {
            if (!IsSlamReady())
                return false;

            ResetSlamCharge(); // this calls SetSlamCharge(0) -> RefreshSlamShards()
            return true;
        }


        /// <summary>
        /// Sets the game over screen on or off.
        /// </summary>
        public override void SetGameOverScreen(bool state)
        {
            GameOverScreen.SetActive(state);
            Text gameOverScreenTextObject = GameOverScreen.transform.Find("GameOverScreenText").GetComponent<Text>();
            if (gameOverScreenTextObject != null)
            {
                gameOverScreenTextObject.text =
                    "GAME OVER\nYOUR SCORE : " + Mathf.Round(GameManager.Instance.Points) +
                    "\nBOUNTIES EARNED : " + Mathf.Round(SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Bounties);
            }
        }

        protected override void OnEnable()
        {
            SkateRunnerDestructibleObjects.OnDestroyed += HandleDestructibleDestroyed;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            SkateRunnerDestructibleObjects.OnDestroyed -= HandleDestructibleDestroyed;
            base.OnDisable();
        }

        private void HandleDestructibleDestroyed(SkateRunnerDestructibleObjects obj)
        {
            Debug.Log($"Destroyed counted: {obj.name}");
            // This is the missing call you asked about:
            AddSlamCharge(1);
        }

    }
}
