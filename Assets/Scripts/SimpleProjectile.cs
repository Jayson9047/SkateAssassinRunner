using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

[RequireComponent(typeof(Collider))]
public class SimpleProjectile : MonoBehaviour
{
    [Header("Shot Motion (relative to level speed)")]
    [SerializeField] private float shotSpeed = 12f; // tuned like MovingObject.Speed (it gets /10 * LevelManager.Speed)

    private Vector3 _shotDirection;
    private Transform _ownerRoot;

    private MovingObject _movingObject;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        _movingObject = GetComponent<MovingObject>();
    }

    public void Init(Vector3 direction, Transform ownerRoot)
    {
        _shotDirection = direction.normalized;
        _ownerRoot = ownerRoot;

        // If projectile doesn't have MovingObject, add it (so it scrolls like everything else)
        if (_movingObject == null)
        {
            _movingObject = gameObject.AddComponent<MovingObject>();
        }

        // Copy scroll settings from the shooter if it has MovingObject
        var ownerMoving = ownerRoot != null ? ownerRoot.GetComponentInChildren<MovingObject>() : null;
        if (ownerMoving != null)
        {
            _movingObject.Direction = ownerMoving.Direction;
            _movingObject.Speed = ownerMoving.Speed;
            _movingObject.MovementSpace = ownerMoving.MovementSpace;
            _movingObject.Acceleration = 0f;
            _movingObject.DirectionCanBeChangedBySpawner = false;
        }
        else
        {
            // fallback: still scroll left a bit if needed
            _movingObject.Direction = Vector3.left;
            _movingObject.Speed = 1f;
            _movingObject.Acceleration = 0f;
        }

        // Optional: keep capsule facing its fire direction
        if (_shotDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(_shotDirection);
        }
    }

    private void Update()
    {
        // Additional “shot” motion on top of MovingObject scroll
        float levelSpeed = (LevelManager.Instance != null) ? LevelManager.Instance.Speed : 1f;
        Vector3 shotMove = _shotDirection * (shotSpeed / 10f) * levelSpeed * Time.deltaTime;
        transform.Translate(shotMove, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore the shooter and its children (prevents instant self-hit)
        if (_ownerRoot != null && other.transform.root == _ownerRoot)
            return;

        // Hit anything => despawn (player death is handled by KillsPlayerOnTouch / SlideAware)
        Destroy(gameObject);
    }
}
