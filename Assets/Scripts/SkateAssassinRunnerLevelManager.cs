using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.Tools;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Spawns the player, and 
    /// </summary>
    public class SkateAssassinRunnerLevelManager : LevelManager
    {
        protected float _savedBounties;

        /// <summary>
        /// What happens when all characters are dead (or when the character is dead if you only have one)
        /// </summary>
        protected override void AllCharactersAreDead()
        {
            // if we've specified an effect for when a life is lost, we instantiate it at the camera's position
            if (LifeLostExplosion != null)
            {
                GameObject explosion = Instantiate(LifeLostExplosion);
                explosion.transform.position =
                    new Vector3(StartingPosition.transform.position.x, StartingPosition.transform.position.y, StartingPosition.transform.position.z);
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
