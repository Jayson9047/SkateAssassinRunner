using UnityEngine;

/// <summary>
/// Enemy Type 1 behavior:
/// - When player enters reach trigger, enemy attacks once.
/// - Sword kills player on touch via MoreMountains KillsPlayerOnTouch (already on sword).
/// </summary>
public class EnemyType1 : EnemyBase
{
    protected override void OnPlayerInReach(Collider playerCollider)
    {
        // Attack immediately when player is in reach.
        TriggerAttack();
    }
}
