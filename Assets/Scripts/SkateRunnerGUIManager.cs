using DamageNumbersPro;
using Elroi.Missions;
using IndieKit;
using MoreMountains.Tools;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Handles all GUI effects and changes
    /// </summary>
    public class SkateRunnerGUIManager : GUIManager, MMEventListener<MMGameEvent>
    {
        [Header("Bounties")]
        [Tooltip("the bounties counter")]
        public TextMeshProUGUI CashText;

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

        [SerializeField] private UnityEngine.UI.Button DownslamButton;

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

        [SerializeField] private Phase2PowerSlamFrameEvents phase2PowerSlamFrameEvents; // drag Body here

        [Header("Phase 2 Power Meter Fade Out")]
        [SerializeField] private float powerMeterFadeDuration = 0.20f;
        [SerializeField] private bool disablePowerMeterAfterFade = true;

        [Header("Level End Screen")]
        public GameObject LevelEndScreen;
        [SerializeField] private TextMeshProUGUI LevelEndTitleText;
        public TextMeshProUGUI LevelEndCashEarnedText;
        public TextMeshProUGUI LevelEndGemsEarnedText;

        [SerializeField] private Image reviveFillImage;   // assign ReviveFill
        [SerializeField] private RectTransform revivePulseTarget; // assign ReviveButton (or ReviveLogo)

        [Header("Damage Numbers Pro - Cash Popup")]
        [SerializeField] private DamageNumber cashPopupPrefab;
        [SerializeField] private Vector3 cashPopupWorldOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Vector2 cashPopupScatterXZ = new Vector2(0.3f, 0.3f);
        [SerializeField] private Vector2 cashPopupScatterY = new Vector2(0.0f, 0.4f);

        [Header("Level End Stars (Images disabled by default)")]
        [SerializeField] private UnityEngine.UI.Image _star1;
        [SerializeField] private UnityEngine.UI.Image _star2;
        [SerializeField] private UnityEngine.UI.Image _star3;

        // Phase 2 Timeout Guards
        private bool _phase2DownslamButtonPressed;   // latch: once true, never kill on timeout
        private bool _phase2BossResolved;            // latch: once resolved, ignore further timeout/result triggers

        public DamageNumber CashPopupPrefab => cashPopupPrefab;
        public Vector3 CashPopupWorldOffset => cashPopupWorldOffset;
        public static System.Action OnPowerSlamUsed;

        private Coroutine _reviveAnimCo;

        private bool _levelEndShown;

        private CanvasGroup _powerMeterGroup;
        private Coroutine _fadePowerMeterRoutine;
        

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

        private void Start()
        {
            // however you access the player in InfiniteRunnerEngine:
            // Usually LevelManager has a Player reference, or use GameObject.FindWithTag("Player") as fallback.
            CachePhase2EventsWhenReadyCo();
            BindPhase2FrameEvents();
            if (cashPopupPrefab != null)
            {
                cashPopupPrefab.PrewarmPool();
            }
        }

        private IEnumerator CachePhase2EventsWhenReadyCo()
        {
            // Wait until LevelManager + player exist (restart timing)
            while (LevelManager.Instance == null || LevelManager.Instance.PlayableCharacters == null || LevelManager.Instance.PlayableCharacters.Count <=0)
                yield return null;

            var player = LevelManager.Instance.PlayableCharacters[0].gameObject;

            phase2PowerSlamFrameEvents =
                player.GetComponentInChildren<Phase2PowerSlamFrameEvents>(true);

            // Optional: log if still missing (helps debugging prefab issues)
            if (phase2PowerSlamFrameEvents == null)
                Debug.LogWarning("Phase2PowerSlamFrameEvents not found under PlayerCharacter.");
        }

        private int GetGemRewardForStars(int stars)
        {
            return stars switch
            {
                1 => 1,
                2 => 3,
                3 => 5,
                _ => 0
            };
        }

        /// <summary>
        /// Sets the text to the game manager's points.
        /// </summary>
        public virtual void RefreshCash()
        {
            var skateRunnerGameManager = SkateRunnerGameManager.SkateRunnerGameManagerAccessor;
            if (CashText == null)
                return;

            CashText.text = skateRunnerGameManager.Cash.ToString("000000");
        }

        public void RefreshGems()
        {
            if (LevelEndGemsEarnedText == null) { return; }
            LevelEndGemsEarnedText.text =
                SkateRunnerGameManager.SkateRunnerGameManagerAccessor.GetGemsEarnedThisLevel().ToString("0");
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

        public void SpawnCashPopup(Vector3 worldPos, int amount, float scale = 1f)
        {
            if (cashPopupPrefab == null) return;

            Vector3 randomOffset = new Vector3(
                Random.Range(-cashPopupScatterXZ.x, cashPopupScatterXZ.x),
                Random.Range(cashPopupScatterY.x, cashPopupScatterY.y),
                Random.Range(-cashPopupScatterXZ.y, cashPopupScatterXZ.y)
            );

            var dn = cashPopupPrefab.Spawn(
                worldPos + cashPopupWorldOffset + randomOffset,
                amount
            );

            dn.transform.localScale *= scale;
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
            _phase2DownslamButtonPressed = false;
            _phase2BossResolved = false;

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

        public void FadeOutPowerMeterWhenPlayerGrounded()
        {
            if (_fadePowerMeterRoutine != null)
                StopCoroutine(_fadePowerMeterRoutine);

            _fadePowerMeterRoutine = StartCoroutine(FadeOutPowerMeterWhenGroundedCo());
        }

        private IEnumerator FadeOutPowerMeterWhenGroundedCo()
        {
            CachePowerMeterGroupIfNeeded();

            if (powerMeter == null || _powerMeterGroup == null)
                yield break;

            // make sure it doesn't block touch while we wait
            _powerMeterGroup.interactable = false;
            _powerMeterGroup.blocksRaycasts = false;

            // Wait until grounded
            if (_jumperCached == null)
                _jumperCached = FindFirstObjectByType<Jumper>();

            while (_jumperCached != null && !_jumperCached.IsGrounded)
                yield return null;

            // Fade out
            float start = _powerMeterGroup.alpha;
            float dur = Mathf.Max(0.01f, powerMeterFadeDuration);
            float t = 0f;

            while (t < dur)
            {
                t += Time.deltaTime;
                _powerMeterGroup.alpha = Mathf.Lerp(start, 0f, t / dur);
                yield return null;
            }

            _powerMeterGroup.alpha = 0f;

            if (disablePowerMeterAfterFade)
                powerMeter.gameObject.SetActive(false);

            _fadePowerMeterRoutine = null;
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
            // Once we have a result, phase2 is resolved; ignore timeout events.
            _phase2BossResolved = true;
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
                    //Sanity check
                    if (phase2PowerSlamFrameEvents == null) BindPhase2FrameEvents();
                    if (phase2PowerSlamFrameEvents == null) { Debug.LogError("Phase2PowerSlamFrameEvents missing"); return; }

                    // Trigger the arm flip + launch sequence
                    SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor.Phase2CarImpulse.ArmFlipOnce();
                    phase2PowerSlamFrameEvents.ResetExecutionAttempt();
                    phase2PowerSlamFrameEvents.ArmPhase2LaunchForNextSlam();
                    StartCoroutine(SimulateDoubleJumpThenDownAttack());
                    // success path (already working / later work)
                    FadeOutSlamHUD();
                    break;
                }
            }
        }

        public void OnPhase2BossCountdownFinished()
        {
            // If phase already resolved (meter result happened, or we already handled timeout), do nothing.
            if (_phase2BossResolved)
                return;

            _phase2BossResolved = true;

            // If player ever pressed the button, we NEVER kill on timeout (prevents mid-slam / post-slam kills).
            if (_phase2DownslamButtonPressed)
                return;

            // Otherwise: same deterministic Phase2 fail as RED
            var player = FindFirstObjectByType<PlayerPhase2Controller>();
            if (player != null)
            {
                player.TriggerPhase2RedFail();
            }
            else
            {
                Debug.LogError("[Phase2] PlayerPhase2Controller not found for timeout fail.");
            }
        }


        private void FadeOutSlamHUD(float duration = 0.15f)
        {
            CacheSlamHUDReferencesIfNeeded();

            // Button
            if (SlamButtonGroup != null)
            {
                SlamButtonGroup.interactable = false;
                SlamButtonGroup.blocksRaycasts = false;
                StartCoroutine(FadeCanvasGroup(SlamButtonGroup, duration));
            }
            SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor?.StopPhase2BossQTECountdown();
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float duration)
        {
            if (cg == null) yield break;

            float startAlpha = cg.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                yield return null;
            }

            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
        }

        private void CachePowerMeterGroupIfNeeded()
        {
            if (powerMeter == null) return;

            if (_powerMeterGroup == null)
            {
                _powerMeterGroup = powerMeter.GetComponent<CanvasGroup>();
                if (_powerMeterGroup == null)
                    _powerMeterGroup = powerMeter.gameObject.AddComponent<CanvasGroup>();
            }
        }


        public override void OnMMEvent(MMGameEvent gameEvent)
        {
            base.OnMMEvent(gameEvent);
            switch (gameEvent.EventName)
            {
                case "PlayableCharactersInstantiated":
                case "GameStart":
                case "LifeLost":
                    BindPhase2FrameEvents();
                    break;
            }
        }

        private void BindPhase2FrameEvents()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            phase2PowerSlamFrameEvents = player.GetComponentInChildren<Phase2PowerSlamFrameEvents>(true);

            if (phase2PowerSlamFrameEvents == null)
                Debug.LogWarning("[SkateRunnerGUIManager] Phase2PowerSlamFrameEvents not found under Player.");
        }

        public void RefreshPhase2Countdown(int secondsRemaining)
        {
            if (PhaseTimerText == null) return;
            secondsRemaining = Mathf.Clamp(secondsRemaining, 0, 99);
            PhaseTimerText.text = $"{secondsRemaining:00}";
        }

        private void OnDownslamButtonClicked()
        {
            if (_phase2SimInProgress) return;

            // Latch immediately. From this point on, we NEVER allow timeout-death.
            _phase2DownslamButtonPressed = true;

            // Optional but matches what you said: timer stops the moment button is clicked.
            SkateAssassinRunnerLevelManager.SkateRunnerLevelManagerAccessor?.StopPhase2BossQTECountdown();

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
                PhaseTimerText.text = "";
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

            OnPowerSlamUsed?.Invoke();   // mission hook: slam was actually spent
            return true;
        }



        /// <summary>
        /// Sets the game over screen on or off.
        /// </summary>
        public override void SetGameOverScreen(bool state)
        {
            GameOverScreen.SetActive(state);
            if(state)
            {
                StartReviveUrgencyVisuals();
            }
            else
            {
                if (_reviveAnimCo != null) { StopCoroutine(_reviveAnimCo); _reviveAnimCo = null; }
                if (revivePulseTarget != null) revivePulseTarget.localScale = Vector3.one;
                if (reviveFillImage != null) reviveFillImage.fillAmount = 1f;
            }
            var anim = GameOverScreen.GetComponent<Animator>();
            if (anim != null) anim.Play(0, 0, 0f);
            TextMeshProUGUI gameOverScreenTextObject = GameOverScreen.transform.Find("GameOverScreenText").GetComponent<TextMeshProUGUI>();
            if (gameOverScreenTextObject != null)
            {
                gameOverScreenTextObject.text = "YOU DIED!";
            }
        }

        private void StartReviveUrgencyVisuals()
        {
            if (_reviveAnimCo != null) StopCoroutine(_reviveAnimCo);
            _reviveAnimCo = StartCoroutine(ReviveUrgencyCo());
        }

        private IEnumerator ReviveUrgencyCo()
        {
            const float duration = 10f;

            // reset
            if (reviveFillImage != null)
                reviveFillImage.fillAmount = 1f;

            if (revivePulseTarget != null)
                revivePulseTarget.localScale = Vector3.one;

            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // IMPORTANT: unaffected by slow-mo
                float n = Mathf.Clamp01(t / duration);

                // drain top->bottom
                if (reviveFillImage != null)
                    reviveFillImage.fillAmount = 1f - n;

                // pulse (subtle)
                if (revivePulseTarget != null)
                {
                    // 2 pulses per second-ish
                    float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 12f);
                    revivePulseTarget.localScale = new Vector3(pulse, pulse, 1f);
                }

                yield return null;
            }

            // After timer ends: stop pulsing, keep button clickable forever
            if (revivePulseTarget != null)
                revivePulseTarget.localScale = Vector3.one;

            // Optional: keep fill at 0 to show time "ended"
            if (reviveFillImage != null)
                reviveFillImage.fillAmount = 0f;
        }


        protected override void OnEnable()
        {
            SkateRunnerDestructibleObject.OnDestroyed += HandleDestroyedForCash;
            SkateRunnerDestructibleObject.OnEnemyKilled += HandleEnemyKilledForSlam;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            SkateRunnerDestructibleObject.OnDestroyed -= HandleDestroyedForCash;
            SkateRunnerDestructibleObject.OnEnemyKilled -= HandleEnemyKilledForSlam;
            base.OnDisable();
        }

        private void HandleDestroyedForCash(SkateRunnerDestructibleObject obj)
        {
            var reward = obj.GetComponent<CashRewardOnDestroyed>();
            if (reward == null || !reward.EnabledReward) return;

            int amount = reward.GetRandomCash();
            if (amount <= 0) return;

            SkateRunnerGameManager.SkateRunnerGameManagerAccessor.AddCash(amount);
            if (cashPopupPrefab != null)
            {
                cashPopupPrefab.Spawn(obj.transform.position + cashPopupWorldOffset, amount);
            }
            RefreshCash();
        }

        private void HandleEnemyKilledForSlam(SkateRunnerDestructibleObject obj)
        {
            AddSlamCharge(1);
        }

        public void ShowLevelEndScreen(bool success)
        {
            if (_levelEndShown) return;
            _levelEndShown = true;
            MissionSystem.MissionSystemAccessor?.OnLevelEnd(success);
            // Lock gameplay input so player can't keep tapping
            LevelManager.Instance?.LockGameplayInputs();

            // 1) Compute stars
            int stars = 0;
            if (success && Elroi.Missions.MissionSystem.MissionSystemAccessor != null)
            {
                stars = Elroi.Missions.MissionSystem.MissionSystemAccessor.GetStarsEarned(success);
            }
            else if (success)
            {
                // if MissionSystem missing, still give the completion star
                stars = 1;
            }

            // 2) Enable star images
            SetStars(stars);

            // 3) Award gems based on stars (replaces your current defaulted gems)
            int gemsToAward = GetGemRewardForStars(stars);

            // Hide revive popup if it's still up
            if (GameOverScreen != null) GameOverScreen.SetActive(false);

            if (LevelEndScreen != null) LevelEndScreen.SetActive(true);

            // TODO (AD HOOK): Show "Commercial Break..." + interstitial here.
            // Save/level progression should only be committed AFTER the ad returns.

            if (LevelEndTitleText != null)
            {
                LevelEndTitleText.text = success ? "LEVEL COMPLETED" : "LEVEL FAILED";

                if (success && gemsToAward > 0)
                {
                    // add to session gems; SaveAfterLevelEnd will bank it
                    SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.AddGems(gemsToAward);
                }
                if (LevelEndCashEarnedText != null)
                {
                    LevelEndCashEarnedText.text =
                        SkateRunnerGameManager.SkateRunnerGameManagerAccessor.GetCashEarnedThisLevel().ToString("0");
                }

                if (LevelEndGemsEarnedText != null)
                {
                    LevelEndGemsEarnedText.text =
                        SkateRunnerGameManager.SkateRunnerGameManagerAccessor.GetGemsEarnedThisLevel().ToString("0");
                }
                // TEMP: saving immediately for now (replace with callback after ad returns)
                SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.SaveAfterLevelEnd(success);
            }
        }
        private void SetStars(int stars)
        {
            if (_star1 != null) _star1.gameObject.SetActive(stars >= 1);
            if (_star2 != null) _star2.gameObject.SetActive(stars >= 2);
            if (_star3 != null) _star3.gameObject.SetActive(stars >= 3);
        }

        public void ResetLevelEndUIState()
        {
            _levelEndShown = false;

            if (LevelEndScreen != null) LevelEndScreen.SetActive(false);
            if (GameOverScreen != null) GameOverScreen.SetActive(false);
        }

        public void OnNoThanksPressed()
        {
            // Player declined revive -> show end screen as failure
            ShowLevelEndScreen(false);
        }


    }
}
