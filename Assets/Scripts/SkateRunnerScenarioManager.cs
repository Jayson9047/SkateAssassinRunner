using UnityEngine;
using System.Collections;
using MoreMountains.InfiniteRunnerEngine;
using System.Collections.Generic;
using System;
using MoreMountains.Tools;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Scenario manager
    /// This class is meant to be extended, and its Scenario() method overridden to describe your own level's scenario.
    /// </summary>
    public class SkateRunnerScenarioManager : ScenarioManager
    {
        /// <summary>
        /// Evaluates the scenario, triggering events every time the level's running time is higher than their start time
        /// </summary>
        protected override void EvaluateScenario()
        {
            float currentBounties = SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Bounties;

            base.EvaluateScenario();
        }
    }
}