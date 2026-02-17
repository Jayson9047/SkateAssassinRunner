using UnityEngine;

public class EnemyTypeDrone : EnemyBase
{
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootInterval = 0.8f;

    [Header("Direction Tuning")]
    [SerializeField] private float minXOffset = 2f;
    [SerializeField] private float maxXOffset = 2.5f;

    [Header("Shooting Duration")]
    [SerializeField] private float shootDuration = 5f; // <-- stop after this many seconds

    [Header("Recoil")]
    [SerializeField] private Transform recoilVisual; // set to your mesh/visual root (same place as DroneHoverBob)
    [SerializeField] private float recoilKickDistance = 0.08f; // small: 0.04–0.12
    [SerializeField] private float recoilKickTime = 0.06f;
    [SerializeField] private float recoilReturnTime = 0.10f;

    [SerializeField] private float recoilPitchDegrees = 4f; // tiny
    [SerializeField] private float recoilRotTime = 0.05f;
    [SerializeField] private float recoilRotReturnTime = 0.10f;

    private bool _hasBeenTriggered;
    private float _timer;

    private Vector3 _recoilBaseLocalPos;
    private Quaternion _recoilBaseLocalRot;
    private Coroutine _recoilCo;

    private float _stopShootingTime;
    private bool _isShooting;

    protected override void Awake()
    {
        base.Awake();

        if (firePoint == null)
        {
            var t = transform.Find("FirePoint");
            if (t != null) firePoint = t;
        }
        if (recoilVisual == null)
        {
            // default to first child named "Visual" if you have one
            var t = transform.Find("Visual");
            if (t != null) recoilVisual = t;
        }
        if (recoilVisual != null)
        {
            _recoilBaseLocalPos = recoilVisual.localPosition;
            _recoilBaseLocalRot = recoilVisual.localRotation;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _hasBeenTriggered = false;
        _isShooting = false;
        _timer = 0f;
        _stopShootingTime = 0f;
        if (recoilVisual != null)
        {
            recoilVisual.localPosition = _recoilBaseLocalPos;
            recoilVisual.localRotation = _recoilBaseLocalRot;
        }
    }

    protected override void OnPlayerInReach(Collider playerCollider)
    {
        // Trigger only once
        if (_hasBeenTriggered) return;

        _hasBeenTriggered = true;
        _isShooting = true;

        _timer = 0f; // fire instantly on first trigger (optional)
        _stopShootingTime = Time.time + shootDuration;
    }

    private void Update()
    {
        if (!_isShooting) return;

        // Stop after duration
        if (Time.time >= _stopShootingTime)
        {
            _isShooting = false;
            return;
        }

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

            projectile.Init(dir, transform.root);
        }
        DoRecoil();
    }
    private void DoRecoil()
    {
        if (recoilVisual == null || firePoint == null) return;

        if (_recoilCo != null) StopCoroutine(_recoilCo);

        // Kick opposite of shot direction (use the same direction you fire with)
        Vector3 shotDir = firePoint.forward;
        shotDir.z = 0f;
        shotDir.Normalize();

        // We want recoil opposite of shot direction
        Vector3 kickWorld = -shotDir * recoilKickDistance;

        // Convert world kick into recoilVisual local space so it plays nice with parent motion
        Vector3 kickLocal = recoilVisual.InverseTransformVector(kickWorld);

        _recoilCo = StartCoroutine(RecoilRoutine(kickLocal));
    }

    private System.Collections.IEnumerator RecoilRoutine(Vector3 kickLocal)
    {
        // Position kick
        Vector3 startPos = _recoilBaseLocalPos;
        Vector3 kickPos = _recoilBaseLocalPos + kickLocal;

        // Rotation kick (pitch up a bit)
        Quaternion startRot = _recoilBaseLocalRot;
        Quaternion kickRot = _recoilBaseLocalRot * Quaternion.Euler(-recoilPitchDegrees, 0f, 0f);

        // Kick phase
        float t = 0f;
        while (t < recoilKickTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / recoilKickTime);

            recoilVisual.localPosition = Vector3.Lerp(startPos, kickPos, EaseOut(a));
            recoilVisual.localRotation = Quaternion.Slerp(startRot, kickRot, EaseOut(a));
            yield return null;
        }

        // Return phase
        t = 0f;
        while (t < recoilReturnTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / recoilReturnTime);

            recoilVisual.localPosition = Vector3.Lerp(kickPos, startPos, EaseInOut(a));
            recoilVisual.localRotation = Quaternion.Slerp(kickRot, startRot, EaseInOut(a));
            yield return null;
        }

        recoilVisual.localPosition = _recoilBaseLocalPos;
        recoilVisual.localRotation = _recoilBaseLocalRot;
        _recoilCo = null;
    }

    private float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private float EaseInOut(float x) => x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;

}
