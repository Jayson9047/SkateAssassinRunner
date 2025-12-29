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
        public float Bounties { get; protected set; }

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
        public virtual void AddBounties(float bountiesToAdd)
        {
            Bounties += bountiesToAdd;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshBounties();
            
        }

        /// <summary>
        /// this method resets the whole game manager
        /// </summary>
        public override void Reset()
        {
            Bounties = 0;
            base.Reset();
        }

        /// <summary>
        /// use this to set the current bounties to the one you pass as a parameter
        /// </summary>
        /// <param name="bounties">bounties.</param>
        public virtual void SetBounties(float bounties)
        {
            Bounties = bounties;
            SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor?.RefreshBounties();
            
        }
    }
}