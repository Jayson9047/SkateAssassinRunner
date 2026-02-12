using IndieKit;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Elroi.Missions;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Spawns the player, and 
    /// </summary>
    public class SkateAssassinRunnerLevelManager : LevelManager
    {
        protected float _savedCash;
        private bool _hasLastDeathPosition;
        private Vector3 _lastDeathPosition;
        public static event Action<int, bool> OnSlamChanged;

        public static SkateAssassinRunnerLevelManager SkateRunnerLevelManagerAccessor { get; private set; }

        private const int SlamMax = 5;
        private int _slamKills;

        [Header("Phase 1 / Phase 2 Timing")]
        [SerializeField] private int Phase1DurationSeconds = 60;

        [Tooltip("At this time (seconds), we disable obstacle spawning so we hit a clean road by Phase 2.")]
        [SerializeField] private int DisableObstacleSpawningAtSeconds = 55;

        [Header("References")]
        [Tooltip("Assign the ObstacleSpawner's MMMultipleObjectPooler here. If left null, we'll try to find GameObject named 'ObstacleSpawner'.")]
        [SerializeField] private MMMultipleObjectPooler ObstacleSpawnerPooler;

        [Header("Phase 2 Car Spawn (Enemy Type 3)")]
        [SerializeField] private GameObject Phase2CarSpawner;   // DO NOT find by name
        [SerializeField] private float Phase2CarSpawnDelaySeconds = 0.75f;


        [Tooltip("Only objects with this tag are treated as enemies for Phase 2 gating.")]
        [SerializeField] private string EnemyTag = "Enemy";

        [Tooltip("Enemy counts as 'ahead' only if enemy.x > StartingPosition.x + this value.")]
        [SerializeField] private float EnemyAheadMinXOffset = 0.5f;

        [Tooltip("How often we re-check while waiting.")]
        [SerializeField] private float EnemyAheadCheckInterval = 0.1f;

        [Tooltip("Safety timeout so we never soft-lock Phase 2 if an enemy gets stuck.")]
        [SerializeField] private float EnemyClearMaxWaitSeconds = 2.0f;

        [Header("Phase 2 Boss QTE")]
        [SerializeField] private float Phase2BossQTEDurationSeconds = 30f;

        [Header("Phase 2 Markers (Enable when Phase 2 car spawns)")]
        [SerializeField] private GameObject StartDriftMarkerGO;
        [SerializeField] private GameObject Phase2CarSlotGO;

        private EnemyType3LaunchController _enemyLaunch;
        private CarImpulseTest _phase2CarImpulse;

        private Coroutine _phase2BossQTERoutine;
        private bool _phase2BossQTEActive;

        private bool _phase2CarSpawnerActivated;
        private Coroutine _phase2CarSpawnerRoutine;
        public bool IsPhase2BossActive => _phase2BossQTEActive;
        public CarImpulseTest Phase2CarImpulse => _phase2CarImpulse;
        public static System.Action OnPhase2Started;
        public static System.Action OnPhase2LifeLost;
        public static System.Action OnLevelWon;
        public float Phase1Duration => Phase1DurationSeconds;

        // If you already have a timer float, return it here instead of Time.time math.
        // Replace `_phase1ElapsedSeconds` with your real variable name.
        public float Phase1ElapsedSeconds
        {
            get
            {
                return _phaseElapsedSeconds;
            }
        }

        // runtime state
        private float _phaseElapsedSeconds;
        private bool _phase2Started;
        private bool _spawningDisabled;

        protected override void Awake()
        {
            base.Awake();
            SkateRunnerLevelManagerAccessor = this;
        }

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void Start()
        {
            Speed = InitialSpeed;
            DistanceTraveled = 0;

            InstantiateCharacters();

            if (StartDriftMarkerGO != null) StartDriftMarkerGO.SetActive(false);
            if (Phase2CarSlotGO != null) Phase2CarSlotGO.SetActive(false);

            ManageControlScheme();

            // storage
            _savedPoints = GameManager.Instance.Points;
            _savedCash = SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Cash;
            SkateRunnerGameManager.SkateRunnerGameManagerAccessor?.BeginLevelSession();
            MissionSystem.MissionSystemAccessor?.BeginLevel(SkateRunnerGameManager.SkateRunnerGameManagerAccessor.LevelNum);
            _started = DateTime.UtcNow;
            GameManager.Instance.SetStatus(GameManager.GameStatus.BeforeGameStart);
            GameManager.Instance.SetPointsPerSecond(PointsPerSecond);

            if (GUIManager.Instance != null)
            {
                // set the level name in the GUI
                GUIManager.Instance.SetLevelName(SceneManager.GetActiveScene().name);
                // fade in
                GUIManager.Instance.FaderOn(false, IntroFadeDuration);
            }

            // Phase timer state (new game / restart only)
            _phaseElapsedSeconds = 0f;
            _phase2Started = false;
            _spawningDisabled = false;

            _phase2CarSpawnerActivated = false;

            _phase2BossQTEActive = false;
            if (_phase2BossQTERoutine != null)
            {
                StopCoroutine(_phase2BossQTERoutine);
                _phase2BossQTERoutine = null;
            }

            if (_phase2CarSpawnerRoutine != null)
            {
                StopCoroutine(_phase2CarSpawnerRoutine);
                _phase2CarSpawnerRoutine = null;
            }


            // Auto-find obstacle pooler if not assigned
            if (ObstacleSpawnerPooler == null)
            {
                var spawnerGo = GameObject.Find("ObstacleSpawner");
                if (spawnerGo != null)
                {
                    ObstacleSpawnerPooler = spawnerGo.GetComponent<MMMultipleObjectPooler>();
                }
            }

            PrepareStart();
            ResetSlam();
        }

        protected override void InstantiateCharacters()
        {
            base.InstantiateCharacters();
            TryBindCinemachineToFirstPlayer();
        }

        protected virtual void TryBindCinemachineToFirstPlayer()
        {
            if (CurrentPlayableCharacters == null || CurrentPlayableCharacters.Count == 0) return;

            var player = CurrentPlayableCharacters[0];
            if (player == null) return;

            var binder = FindFirstObjectByType<CameraPlayerBinder>();
            if (binder == null) return;

            binder.BindTo(player.transform);
        }


        private void DisableObstacleSpawning()
        {
            if (_spawningDisabled) return;
            _spawningDisabled = true;

            if (ObstacleSpawnerPooler == null || ObstacleSpawnerPooler.Pool == null) return;

            for (int i = 0; i < ObstacleSpawnerPooler.Pool.Count; i++)
            {
                ObstacleSpawnerPooler.Pool[i].Enabled = false;
            }
        }
        /// <summary>
        /// What happens when all characters are dead (or when the character is dead if you only have one)
        /// </summary>
        protected override void AllCharactersAreDead()
        {
            // if we've specified an effect for when a life is lost, we instantiate it at the camera's position
            if (LifeLostExplosion != null)
            {
                GameObject explosion = Instantiate(LifeLostExplosion);

                LevelManager.Instance.FreezeSpeedAndCancelBoost();
                if (_hasLastDeathPosition)
                {
                    explosion.transform.position = _lastDeathPosition;
                    _hasLastDeathPosition = false; // clear after use
                }
                else
                {
                    explosion.transform.position = StartingPosition.transform.position; // fallback
                }
            }
            // we've just lost a life
            GameManager.Instance.SetStatus(GameManager.GameStatus.LifeLost);
            MMGameEvent.Trigger("LifeLost");
            _started = DateTime.UtcNow;
            GameManager.Instance.SetPoints(_savedPoints);
            SkateRunnerGameManager.SkateRunnerGameManagerAccessor.SetCash(_savedCash);
            GameManager.Instance.LoseLives(1);

            if (GameManager.Instance.CurrentLives <= 0)
            {
                GUIManager.Instance.SetGameOverScreen(true);
                GameManager.Instance.SetStatus(GameManager.GameStatus.GameOver);
                MMGameEvent.Trigger("GameOver");
            }
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.UnlockGameplayInputs();
            }
        }

        protected override void OnDisable()
        {
            SkateRunnerDestructibleObject.OnDestroyed -= HandleDestroyed;
            base.OnDisable();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            SkateRunnerDestructibleObject.OnDestroyed += HandleDestroyed;
        }

        private void HandleDestroyed(SkateRunnerDestructibleObject obj)
        {
            AddSlamKill();
        }

        public void AddSlamKill()
        {
            if (_slamKills >= SlamMax) return;

            _slamKills++;
            OnSlamChanged?.Invoke(_slamKills, _slamKills >= SlamMax);
        }


        public bool IsSlamReady() => _slamKills >= SlamMax;

        public void ConsumeSlam()
        {
            if (!IsSlamReady()) return;
            ResetSlam();
        }

        private void ResetSlam()
        {
            _slamKills = 0;
            OnSlamChanged?.Invoke(_slamKills, false);
        }
        protected override IEnumerator KillCharacterCo(PlayableCharacter player)
        {
            // Cache death position BEFORE the player gets removed/destroyed
            if (player != null)
            {
                _lastDeathPosition = player.transform.position;
                _hasLastDeathPosition = true;
            }

            // Keep original behavior
            LevelManager.Instance.CurrentPlayableCharacters.Remove(player);
            player.Die();
            yield return new WaitForSeconds(0f);

            // If last character died, trigger life-lost/gameover flow
            if (LevelManager.Instance.CurrentPlayableCharacters.Count == 0)
            {
                AllCharactersAreDead();
            }
        }
        public void OnPhase2SlamImpact()
        {
            if (_phase2CarImpulse == null)
            {
                Debug.LogError("[Phase2] phase2CarImpulse is not assigned (drag carBody with CarImpulseTest).");
                return;
            }

            _phase2CarImpulse.BlowRearUp();
            _enemyLaunch?.Launch();
        }

        public void RegisterPhase2Car(Transform pickupRoot)
        {
            _phase2CarImpulse =
                pickupRoot.GetComponentInChildren<CarImpulseTest>(true);
            _enemyLaunch = pickupRoot.GetComponentInChildren<EnemyType3LaunchController>(true);
            if (_phase2CarImpulse == null)
            {
                Debug.LogError(
                    "[Phase2] CarImpulseTest not found under pickup -> carBody"
                );
            }
            else
            {
                Debug.Log($"[Phase2] Registered Phase2 carBody: {_phase2CarImpulse.name}");
            }
        }

        
        public override void LifeLostAction()
        {
            if (_phase2BossQTEActive)
            {
                OnPhase2LifeLost?.Invoke();
            }
            // Restore whatever speed/accel we had before the cinematic stop
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ResumeSpeedAfterFreeze();
            }
            // Continue the normal respawn flow
            base.LifeLostAction();
            // We are still in Phase 2, just retrying the QTE
            if (_phase2BossQTEActive)
            {
                OnPhase2LifeLost?.Invoke();
                RestartPhase2BossQTE();
            }
        }

        public override void ResetLevel()
        {
            UnlockGameplayInputs();
            ResumeSpeedAfterFreeze();

            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.ResetLevelEndUIState();

            base.ResetLevel();
        }
        public override void GameOverAction()
        {
            // We're using the "GameOverScreen" as a Revive popup now.
            // The base engine restarts the level on ANY click during GameOver.
            // So while the popup is up, we must ignore global restart input.
            if (GUIManager.Instance != null &&
                GUIManager.Instance.GameOverScreen != null &&
                GUIManager.Instance.GameOverScreen.activeInHierarchy)
            {
                return;
            }

            base.GameOverAction();
        }


        // ------------------------------------------------------
        // NEW: Called by Phase2CarApproachController when the Jeep is fully in position (Speed = 0)
        // ------------------------------------------------------
        public void OnEnemyType3LockedInPosition()
        {
            if (_phase2BossQTEActive)
                return;

            _phase2BossQTEActive = true;
            OnPhase2Started?.Invoke();

            // 1) Stop all gameplay controls (swipes/tap zone, etc.)
            // Your detectors already early-return when GameplayInputsLocked is true.
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LockGameplayInputs();
            }
            _enemyLaunch.SetEnemyDisarmed(true);
            // 2) Tell GUI to transition: Slam button only, platform + shards fade out
            if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
            {
                SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.EnterPhase2BossHUD();
            }

            // 3) Start 30s timer
            if (_phase2BossQTERoutine != null)
            {
                StopCoroutine(_phase2BossQTERoutine);
            }
            _phase2BossQTERoutine = StartCoroutine(Phase2BossQTECountdownCo());
        }
        private IEnumerator Phase2BossQTECountdownCo()
        {
            float remaining = Mathf.Max(0f, Phase2BossQTEDurationSeconds);

            while (remaining > 0f)
            {
                // Only count down while actually playing (no countdown during LifeLost/GameOver/etc.)
                if (GameManager.Instance.Status == GameManager.GameStatus.GameInProgress)
                {
                    remaining -= Time.deltaTime;

                    // Optional: show countdown on the top timer text
                    if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
                    {
                        SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.RefreshPhase2Countdown(
                            Mathf.CeilToInt(remaining)
                        );
                    }
                }

                yield return null;
            }

            // Time’s up — ask GUI to resolve timeout safely
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor
                ?.OnPhase2BossCountdownFinished();

            MMGameEvent.Trigger("Phase2BossQTETimeout");

            _phase2BossQTERoutine = null;
        }

        public void StopPhase2BossQTECountdown()
        {
            if (_phase2BossQTERoutine != null)
            {
                StopCoroutine(_phase2BossQTERoutine);
                _phase2BossQTERoutine = null;
            }
        }


        public void RestartPhase2BossQTE()
        {
            // Lock inputs again (Phase 2 rule)
            LockGameplayInputs();

            // Reset and restart timer
            if (_phase2BossQTERoutine != null)
            {
                StopCoroutine(_phase2BossQTERoutine);
            }

            _phase2BossQTERoutine = StartCoroutine(Phase2BossQTECountdownCo());

            // Restart PowerMeter
            if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
            {
                SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.RestartPhase2PowerMeter();
            }
        }


        /// <summary>
        /// Waits for a short time and then loads the specified level
        /// </summary>
        /// <returns>The level co.</returns>
        /// <param name="levelName">Level name.</param>
        protected override IEnumerator GotoLevelCo(string levelName)
        {
            if (Time.timeScale > 0.0f)
            {
                yield return new WaitForSeconds(OutroFadeDuration);
            }

            GameManager.Instance.UnPause();

            if (string.IsNullOrEmpty(levelName))
            {
                MMSceneLoadingManager.LoadScene("SkateRunnerStartScreen");
            }
            else
            {
                MMSceneLoadingManager.LoadScene(levelName);
            }
        }

        public void NotifyLevelWon()
        {
            OnLevelWon?.Invoke();
        }

        /// <summary>
        /// Every frame
        /// </summary>
        public override void Update()
        {
            _savedPoints = GameManager.Instance.Points;
            _savedCash = SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Cash;
            _started = DateTime.UtcNow;

            // we increment the total distance traveled so far
            DistanceTraveled = DistanceTraveled + Speed * Time.fixedDeltaTime;

            // if we can still accelerate, we apply the level's speed acceleration
            if (Speed < MaximumSpeed)
            {
                Speed += SpeedAcceleration * Time.deltaTime;
            }

            HandleSpeedFactor();

            RunningTime += Time.deltaTime;

            // -----------------------------
            // PHASE TIMER LOGIC (NEW)
            // -----------------------------
            if (!_phase2Started && GameManager.Instance.Status == GameManager.GameStatus.GameInProgress)
            {
                _phaseElapsedSeconds += Time.deltaTime;

                int elapsedInt = Mathf.FloorToInt(_phaseElapsedSeconds);

                // UI refresh (top timer)
                if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
                {
                    SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.RefreshPhaseTimer(
                        elapsedInt,
                        Phase1DurationSeconds,
                        _phase2Started
                    );
                }

                // Stop obstacle spawning early so the road clears by 60
                if (!_spawningDisabled && elapsedInt >= DisableObstacleSpawningAtSeconds)
                {
                    DisableObstacleSpawning();
                }

                // Phase 2 start
                if (elapsedInt >= Phase1DurationSeconds)
                {
                    _phase2Started = true;

                    // Update UI one last time
                    if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
                    {
                        SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.RefreshPhaseTimer(
                            Phase1DurationSeconds,
                            Phase1DurationSeconds,
                            _phase2Started
                        );
                    }

                    // Hook for your upcoming phase 2 sequence (Jeep/sniper/etc.)
                    MMGameEvent.Trigger("Phase2Start");
                    if (!_phase2CarSpawnerActivated && _phase2CarSpawnerRoutine == null)
                    {
                        _phase2CarSpawnerRoutine = StartCoroutine(ActivatePhase2CarSpawnerAfterDelayCo());
                    }
                }
            }
            else
            {
                // If we're not in gameplay (LifeLost, BeforeGameStart, Paused, GameOver),
                // keep UI in sync but do NOT advance the timer.
                if (SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor != null)
                {
                    SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor.RefreshPhaseTimer(
                        Mathf.FloorToInt(_phaseElapsedSeconds),
                        Phase1DurationSeconds,
                        _phase2Started
                    );
                }
            }
        }

        private IEnumerator ActivatePhase2CarSpawnerAfterDelayCo()
        {
            // 1) Delay after Phase 1 ends (pauses when not in gameplay)
            float remaining = Mathf.Max(0f, Phase2CarSpawnDelaySeconds);
            while (remaining > 0f)
            {
                if (GameManager.Instance.Status == GameManager.GameStatus.GameInProgress)
                {
                    remaining -= Time.deltaTime;
                }
                yield return null;
            }

            // 2) Wait until no enemies ahead (TAG-based)
            float maxWaitRemaining = Mathf.Max(0f, EnemyClearMaxWaitSeconds);
            float checkTimer = 0f;

            while (true)
            {
                // pause waiting if not in active gameplay (respawn etc.)
                if (GameManager.Instance.Status != GameManager.GameStatus.GameInProgress)
                {
                    yield return null;
                    continue;
                }

                // timeout safety (prevents soft-lock if something gets stuck)
                if (EnemyClearMaxWaitSeconds > 0f)
                {
                    maxWaitRemaining -= Time.deltaTime;
                    if (maxWaitRemaining <= 0f)
                    {
                        break;
                    }
                }

                checkTimer -= Time.deltaTime;
                if (checkTimer > 0f)
                {
                    yield return null;
                    continue;
                }

                checkTimer = EnemyAheadCheckInterval;

                if (!AnyEnemiesAhead_Tag())
                {
                    break;
                }

                yield return null;
            }

            ActivatePhase2CarSpawnerNow();
            _phase2CarSpawnerRoutine = null;
        }



        private bool AnyEnemiesAhead_Tag()
        {
            if (StartingPosition == null)
            {
                Debug.LogWarning("[Phase2] StartingPosition not assigned - cannot check enemies ahead.");
                return false; // fail open (don't block forever)
            }

            float minX = StartingPosition.transform.position.x + EnemyAheadMinXOffset;

            // NOTE: FindGameObjectsWithTag allocates and can be expensive,
            // but we're calling it only during Phase 2 entry for a short time window.
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(EnemyTag);

            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject e = enemies[i];
                if (e == null) continue;

                if (e.transform.position.x > minX)
                {
                    return true;
                }
            }

            return false;
        }


        private void ActivatePhase2CarSpawnerNow()
        {
            if (_phase2CarSpawnerActivated) return;
            _phase2CarSpawnerActivated = true;

            if (Phase2CarSpawner == null)
            {
                Debug.LogWarning("[Phase2] Phase2CarSpawner is not assigned in the inspector.");
                return;
            }

            // 1) Enable all poolable objects in the spawner's MMMultipleObjectPooler
            MMMultipleObjectPooler pooler = Phase2CarSpawner.GetComponent<MMMultipleObjectPooler>();
            if (pooler == null)
            {
                Debug.LogWarning("[Phase2] Phase2CarSpawner has no MMMultipleObjectPooler component.");
            }
            else if (pooler.Pool != null)
            {
                for (int i = 0; i < pooler.Pool.Count; i++)
                {
                    pooler.Pool[i].Enabled = true;
                }
            }
            if (StartDriftMarkerGO != null) StartDriftMarkerGO.SetActive(true);
            if (Phase2CarSlotGO != null) Phase2CarSlotGO.SetActive(true);
        }


    }
}
