using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

namespace MoreMountains.InfiniteRunnerEngine
{
    /// <summary>
    /// Same as KillsPlayerOnTouch, except for obstacles tagged "SlideObstacle":
    /// - If player is sliding, we ignore the hit.
    /// - If player is not sliding, we kill like normal.
    /// 
    /// Put this ONLY on obstacles that should be "slide-under" obstacles.
    /// </summary>
    public class KillsPlayerOnTouch_SlideAware : KillsPlayerOnTouch
    {
        [Tooltip("Only applies slide immunity when THIS obstacle has this tag.")]
        [SerializeField] private string slideObstacleTag = "SlideObstacle";

        protected override void TriggerEnter(GameObject collidingObject)
        {
            if (collidingObject != null && collidingObject.CompareTag("Player"))
            {
                var swipeDown =
                    collidingObject.GetComponent<SwipeDownDetector>() ??
                    collidingObject.GetComponentInChildren<SwipeDownDetector>(true) ??
                    collidingObject.GetComponentInParent<SwipeDownDetector>();

                if (swipeDown != null && swipeDown.IsSliding)
                {
                    return;
                }
            }

            base.TriggerEnter(collidingObject);
        }

    }
}
