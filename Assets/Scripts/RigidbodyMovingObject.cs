using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>MovingObject scrolling for dynamic debris, integrated on the physics clock.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
public sealed class RigidbodyMovingObject : MovingObject
{
    private Rigidbody body;
    private Vector3 previousScrollVelocity;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        previousScrollVelocity = Vector3.zero;
    }

    // The inherited Update translates Transform, which fights dynamic interpolation.
    protected override void Update() { }

    private void FixedUpdate() => Move();

    public override void Move()
    {
        if (body == null || body.isKinematic) return;
        float levelSpeed = LevelManager.Instance != null ? LevelManager.Instance.Speed : 1f;
        Vector3 direction = MovementSpace == Space.Self ? transform.TransformDirection(Direction) : Direction;
        Vector3 scrollVelocity = direction * (Speed / 10f) * levelSpeed;
        // Only replace our scrolling contribution. Gravity, separation impulses and
        // angular motion stay with PhysX. Frictionless corpse colliders prevent the
        // static road collider from braking the world-scroll contribution to zero.
        body.linearVelocity += scrollVelocity - previousScrollVelocity;
        previousScrollVelocity = scrollVelocity;
        _movement = scrollVelocity * Time.fixedDeltaTime;
        Speed += Acceleration * Time.fixedDeltaTime;
    }

    private void OnDisable()
    {
        if (body != null && !body.isKinematic) body.linearVelocity -= previousScrollVelocity;
        previousScrollVelocity = Vector3.zero;
        _movement = Vector3.zero;
    }
}
