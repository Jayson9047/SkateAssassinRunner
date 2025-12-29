using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Handles all GUI effects and changes
    /// </summary>
    public class SkateRunnerGUIManager : GUIManager, MMEventListener<MMGameEvent>
    {
        /// the points counter
        [Tooltip("the bounties counter")]
        public Text BountiesText;
        public static SkateRunnerGUIManager SkateRunnerGUIManagerAccessor { get; private set; }

        protected override void Awake()
        {
            base.Awake(); // VERY important for MM

            SkateRunnerGUIManagerAccessor = this;
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
        /// Sets the game over screen on or off.
        /// </summary>
        /// <param name="state">If set to <c>true</c>, sets the game over screen on.</param>
        public override void SetGameOverScreen(bool state)
        {
            GameOverScreen.SetActive(state);
            Text gameOverScreenTextObject = GameOverScreen.transform.Find("GameOverScreenText").GetComponent<Text>();
            if (gameOverScreenTextObject != null)
            {
                gameOverScreenTextObject.text = "GAME OVER\nYOUR SCORE : " + Mathf.Round(GameManager.Instance.Points) + "\nBOUNTIES EARNED : " + Mathf.Round(SkateRunnerGameManager.SkateRunnerGameManagerAccessor.Bounties);
            }
        }
    }

}