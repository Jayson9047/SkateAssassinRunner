using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Tools;
using IndieKit;

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

        private int _currentSlamCharges;

        protected override void Awake()
        {
            base.Awake(); // VERY important for MM
            SkateRunnerGUIManagerAccessor = this;

            // Optional: initialize UI on load
            SetSlamCharge(0);
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

        protected virtual void OnEnable()
        {
            SkateRunnerDestructibleObjects.OnDestroyed += HandleDestructibleDestroyed;
        }

        protected virtual void OnDisable()
        {
            SkateRunnerDestructibleObjects.OnDestroyed -= HandleDestructibleDestroyed;
        }

        private void HandleDestructibleDestroyed(SkateRunnerDestructibleObjects obj)
        {
            Debug.Log($"Destroyed counted: {obj.name}");
            // This is the missing call you asked about:
            AddSlamCharge(1);
        }

    }
}
