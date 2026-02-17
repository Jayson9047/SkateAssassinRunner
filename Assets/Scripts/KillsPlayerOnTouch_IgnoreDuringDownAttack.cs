using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

public class KillsPlayerOnTouch_IgnoreDuringDownAttack : KillsPlayerOnTouch
{
    protected override void TriggerEnter(GameObject collidingObject)
    {
        if (collidingObject != null && collidingObject.CompareTag("Player"))
        {
            var down = collidingObject.GetComponentInParent<SwipeDownDetector>();
            if (down != null && down.IsDownAttacking)
            {
                // Ignore Enemy weapon hits while player is in down attack
                return;
            }
        }

        base.TriggerEnter(collidingObject);
    }
}
