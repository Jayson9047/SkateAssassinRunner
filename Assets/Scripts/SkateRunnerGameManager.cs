using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// The game manager is a persistent singleton that handles bounties and time
    /// </summary>
    public class SkateRunnerGameManager : GameManager
    {
        /// the current number of game bounties
        public float Cash { get; protected set; }

        [Header("Gems")]
        public float DefaultGemsPerLevelComplete = 2f;
        public float Gems { get; protected set; }

        // snapshots for "earned this level" on LevelEndScreen
        private float _cashAtLevelStart;
        private float _gemsAtLevelStart;

        public static SkateRunnerGameManager SkateRunnerGameManagerAccessor { get; private set; }

        protected override void Awake()
        {
            base.Awake(); // VERY important for MM

            SkateRunnerGameManagerAccessor = this;
            // Make CurrentLives correct before any GUI init
            CurrentLives = TotalLives;
        }

        /// <summary>
        /// Adds the bounties in parameters to the current game bounties.
        /// </summary>
        /// <param name="bountiesToAdd">bounties to add.</param>
        public virtual void AddCash(float cashToAdd)
        {
            Cash += cashToAdd;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshCash();
            
        }

        /// <summary>
        /// this method resets the whole game manager
        /// </summary>
        public override void Reset()
        {
            Cash = 0;
            Gems = 0;
            base.Reset();
        }

        /// <summary>
        /// use this to set the current bounties to the one you pass as a parameter
        /// </summary>
        /// <param name="bounties">bounties.</param>
        public virtual void SetCash(float bounties)
        {
            Cash = bounties;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshCash();
            
        }


        public void BeginLevelSession()
        {
            _cashAtLevelStart = Cash;
            _gemsAtLevelStart = Gems;
        }

        public float GetCashEarnedThisLevel()
        {
            return Mathf.Max(0f, Cash - _cashAtLevelStart);
        }

        public float GetGemsEarnedThisLevel()
        {
            return Mathf.Max(0f, Gems - _gemsAtLevelStart);
        }

        private void AddGems(float amount)
        {
            Gems += amount;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshGems();
        }

        public void AddGems()
        {
            AddGems(DefaultGemsPerLevelComplete);
        }

        public void SetGems(float value)
        {
            Gems = value;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshGems();
        }

    }
}