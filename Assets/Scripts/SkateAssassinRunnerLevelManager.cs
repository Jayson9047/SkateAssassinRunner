using IndieKit;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Spawns the player, and 
    /// </summary>
    public class SkateAssassinRunnerLevelManager : LevelManager
    {
        protected float _savedBounties;
        private bool _hasLastDeathPosition;
        private Vector3 _lastDeathPosition;
        public static event Action<int, bool> OnSlamChanged;

        private const int SlamMax = 5;
        private int _slamKills;
        /// <summary>
        /// What happens when all characters are dead (or when the character is dead if you only have one)
        /// </summary>
        protected override void AllCharactersAreDead()
        {
            // if we've specified an effect for when a life is lost, we instantiate it at the camera's position
            if (LifeLostExplosion != null)
            {
                GameObject explosion = Instantiate(LifeLostExplosion);

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
            SkateRunnerGameManager.SkateRunnerGameManagerAccessor.SetBounties(_savedBounties);
            GameManager.Instance.LoseLives(1);

            if (GameManager.Instance.CurrentLives <= 0)
            {
                GUIManager.Instance.SetGameOverScreen(true);
                GameManager.Instance.SetStatus(GameManager.GameStatus.GameOver);
                MMGameEvent.Trigger("GameOver");
            }
        }

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void Start()
        {
            Speed = InitialSpeed;
            DistanceTraveled = 0;

            InstantiateCharacters();

            ManageControlScheme();

            // storage
            _savedPoints = GameManager.Instance.Points;
            _savedBounties = SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Bounties;
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

            PrepareStart();
            ResetSlam();
        }
        protected override void OnDisable()
        {
            SkateRunnerDestructibleObjects.OnDestroyed -= HandleDestroyed;
            base.OnDisable();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            SkateRunnerDestructibleObjects.OnDestroyed += HandleDestroyed;
        }
        private void HandleDestroyed(SkateRunnerDestructibleObjects obj)
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

        /// <summary>
        /// Every frame
        /// </summary>
        public override void Update()
        {
            _savedPoints = GameManager.Instance.Points;
            _savedBounties = SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Bounties;
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
        }
    }
}
