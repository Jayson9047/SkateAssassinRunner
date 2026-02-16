using UnityEngine;

public class EnemyTypeDrone : EnemyBase
{
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootInterval = 0.8f;
    [SerializeField] private float minXOffset = 2f;
    [SerializeField] private float maxXOffset = 2.5f;

    private bool _hasBeenTriggered;
    private float _timer;

    protected override void Awake()
    {
        base.Awake();

        if (firePoint == null)
        {
            var t = transform.Find("FirePoint");
            if (t != null) firePoint = t;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _hasBeenTriggered = false;
        _timer = 0f;
    }

    protected override void OnPlayerInReach(Collider playerCollider)
    {
        // Latch ON forever (until disabled/recycled)
        _hasBeenTriggered = true;

        // Optional: fire instantly on first trigger
        _timer = 0f;
    }

    private void Update()
    {
        if (!_hasBeenTriggered) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        Shoot();
        _timer = shootInterval;
    }

    private void Shoot()
    {
        if (firePoint == null || projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: Missing firePoint or projectilePrefab.");
            return;
        }

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        var projectile = go.GetComponent<SimpleProjectile>();
        if (projectile != null)
        {
            Vector3 dir = firePoint.forward;

            float xOffset = Random.Range(minXOffset, maxXOffset);

            // Force left + down in WORLD space for your 2.5D runner
            dir.x = -Mathf.Abs(dir.x + xOffset);
            dir.y = -Mathf.Abs(dir.y);
            dir.z = 0f;
            dir.Normalize();

            projectile.Init(dir, this.transform.root);
        }

    }
}

