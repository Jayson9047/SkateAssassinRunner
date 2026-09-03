using UnityEngine;

public class Phase2ExecutionBullet : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Only care about player
        if (!other.CompareTag("Player")) return;

        var p2 = other.GetComponentInParent<PlayerPhase2Controller>();
        if (p2 == null) return;

        // Only trigger Phase2 death if execution is pending
        if (p2.Phase2ExecutionPending)
        {
            SkateRunnerAudioManager.PlayPhase2SniperImpact();
            p2.OnHitByPhase2ExecutionBullet();
        }
    }
}
