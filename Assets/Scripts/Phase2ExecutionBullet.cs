using UnityEngine;

public class Phase2ExecutionBullet : MonoBehaviour
{
    private Collider _target;
    private SimpleProjectile _projectile;
    private Vector3 _previousPosition;

    public void Initialize(Collider target, SimpleProjectile projectile)
    {
        _target = target;
        _projectile = projectile;
        _previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (_target == null || !_target.enabled || _projectile == null || _projectile.HasHit) return;

        // Transform-driven shots can cross the entire player between physics
        // ticks. Sweep the travelled segment against the real player collider.
        Vector3 position = transform.position;
        Vector3 travel = position - _previousPosition;
        RaycastHit hit;
        if (travel.sqrMagnitude > 0f &&
            _target.Raycast(new Ray(_previousPosition, travel.normalized), out hit, travel.magnitude))
        {
            transform.position = hit.point;
            if (_projectile.TryHit(_target, hit.point)) ResolvePlayerHit(_target);
        }
        _previousPosition = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        ResolvePlayerHit(other);
    }

    private static void ResolvePlayerHit(Collider other)
    {

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
