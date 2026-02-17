using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

[RequireComponent(typeof(Collider))]
public class SimpleProjectile : MonoBehaviour
{
    [Header("Shot Motion (relative to level speed)")]
    [SerializeField] private float shotSpeed = 12f;

    [Header("Toon Projectile VFX")]
    [SerializeField] private float destroyDelayAfterHit = 1.0f;
    [SerializeField] private bool autoFindToonVFX = true;

    private Vector3 _shotDirection;
    private Transform _ownerRoot;

    private MovingObject _movingObject;

    // Toon Projectile hierarchy refs
    private Transform _visuals;
    private GameObject _travelVisual; // Visuals/Projectile
    private GameObject _flash;        // Visuals/Flash1
    private GameObject _hit;          // Visuals/Hit1

    private bool _hasHit;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        _movingObject = GetComponent<MovingObject>();

        if (autoFindToonVFX)
        {
            CacheToonVFXRefs();
        }
    }

    private void CacheToonVFXRefs()
    {
        _visuals = transform.Find("Visuals");
        if (_visuals == null) return;

        var t = _visuals.Find("Projectile");
        if (t != null) _travelVisual = t.gameObject;

        t = _visuals.Find("Flash1");
        if (t != null) _flash = t.gameObject;

        t = _visuals.Find("Hit1");
        if (t != null) _hit = t.gameObject;
    }

    public void Init(Vector3 direction, Transform ownerRoot, float? overrideShotSpeed = null)
    {
        _shotDirection = direction.normalized;
        _ownerRoot = ownerRoot;

        if (overrideShotSpeed.HasValue)
            shotSpeed = overrideShotSpeed.Value;

        _hasHit = false;

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

        // Face fire direction (helps if the visual uses forward)
        if (_shotDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(_shotDirection);
        }

        // Play muzzle flash (detach so it doesn't ride the projectile)
        PlayDetachedFX(_flash, transform.position, transform.rotation);

        // Ensure travel visual is visible
        if (_travelVisual != null) _travelVisual.SetActive(true);
    }
    public void SetShotSpeed(float newSpeed)
    {
        shotSpeed = newSpeed;
    }

    private void Update()
    {
        if (_hasHit) return;

        // Additional “shot” motion on top of MovingObject scroll
        float levelSpeed = (LevelManager.Instance != null) ? LevelManager.Instance.Speed : 1f;
        Vector3 shotMove = _shotDirection * (shotSpeed / 10f) * levelSpeed * Time.deltaTime;
        transform.Translate(shotMove, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        // Ignore the shooter and its children (prevents instant self-hit)
        if (_ownerRoot != null && other.transform.root == _ownerRoot)
            return;

        _hasHit = true;

        // Spawn hit FX at impact point
        Vector3 hitPos = other.ClosestPoint(transform.position);
        Quaternion hitRot = transform.rotation;

        PlayDetachedFX(_hit, hitPos, hitRot);

        // Hide travel visual immediately so it "impacts"
        if (_travelVisual != null) _travelVisual.SetActive(false);

        // Stop further collisions & movement
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (_movingObject != null) _movingObject.enabled = false;

        // Destroy after a short delay (lets hit FX finish nicely)
        Destroy(gameObject, destroyDelayAfterHit);
    }

    private void PlayDetachedFX(GameObject fxObj, Vector3 pos, Quaternion rot)
    {
        if (fxObj == null) return;

        // Detach from projectile so it stays in world space
        fxObj.transform.SetParent(null, true);
        fxObj.transform.position = pos;
        fxObj.transform.rotation = rot;
        fxObj.SetActive(true);

        // Play all particle systems and compute lifetime
        var pss = fxObj.GetComponentsInChildren<ParticleSystem>(true);
        float maxDuration = 0.25f;

        foreach (var ps in pss)
        {
            ps.Play(true);
            var main = ps.main;

            // duration + max lifetime (rough but good)
            float dur = main.duration + main.startLifetime.constantMax;
            if (dur > maxDuration) maxDuration = dur;
        }

        Destroy(fxObj, maxDuration + 0.25f);
    }
}
