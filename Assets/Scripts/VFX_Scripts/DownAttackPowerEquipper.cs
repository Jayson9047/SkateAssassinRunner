using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using MoreMountains.InfiniteRunnerEngine;

public class DownAttackPowerEquipper : MonoBehaviour
{
    [Header("Where to attach AIR DownAttack FX (spawned on player)")]
    [SerializeField] private Transform airFxAnchor; // set to a child on player (spine/hips/root)

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    [Header("Registry (enum -> definition)")]
    [SerializeField] private DownAttackPowerEntry[] downAttackPowers;
    [SerializeField] private DownAttackPowerId EquippedPowerId = DownAttackPowerId.None;

    [Header("Pooling")]
    [SerializeField] private bool poolFx = true;
    [SerializeField] private int airFxPoolSize = 6;
    [SerializeField] private int groundFxPoolSize = 8;

    [Header("Grounded Source")]
    [SerializeField] private Jumper jumper;

    private GameObject _activeAirFxInstance;
    private GameObject _activeAirFxPrefab;
    private Coroutine _airFxReturnCoroutine;
    private Coroutine _airFxDelayCoroutine;
    private Coroutine _airFxGroundWatchCoroutine;

    private DownAttackPowerDefinition equippedPower;
    public DownAttackPowerDefinition EquippedPower => equippedPower;

    private Dictionary<DownAttackPowerId, DownAttackPowerDefinition> _powerMap;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    private void OnEnable()
    {
        var saved = DownAttackPowerSave.TryLoad(out var id) ? id : EquippedPowerId;
        EquipDownAttackPower(saved);
        if (jumper == null)
            jumper = GetComponentInParent<Jumper>();
    }
    private void Awake()
    {
        if (jumper == null)
            jumper = GetComponentInParent<Jumper>();
    }

    public void EquipDownAttackPower(DownAttackPowerId id)
    {
        BuildPowerMapIfNeeded();
        EquippedPowerId = id;

        if (_powerMap != null && _powerMap.TryGetValue(id, out var def))
            equippedPower = def;
        else
            equippedPower = null;

        // Optional: prewarm both pools for currently equipped prefabs
        PrewarmEquippedPools();
    }

    private void BuildPowerMapIfNeeded()
    {
        if (_powerMap != null) return;

        _powerMap = new Dictionary<DownAttackPowerId, DownAttackPowerDefinition>();
        if (downAttackPowers == null) return;

        for (int i = 0; i < downAttackPowers.Length; i++)
        {
            var e = downAttackPowers[i];
            if (e == null || e.definition == null) continue;
            _powerMap[e.id] = e.definition;
        }
    }

    private void PrewarmEquippedPools()
    {
        if (!poolFx || equippedPower == null) return;

        if (equippedPower.downAttackAirFxPrefab != null)
            EnsurePool(equippedPower.downAttackAirFxPrefab, airFxPoolSize);

        if (equippedPower.groundImpactAoeFxPrefab != null)
            EnsurePool(equippedPower.groundImpactAoeFxPrefab, groundFxPoolSize);
    }

    // --------- PUBLIC SPAWN METHODS ---------

    /// <summary>
    /// Spawns the "air" down-attack FX on the player when the down swipe triggers after double jump.
    /// </summary>
    public void SpawnAirDownAttackFx()
    {
        if (equippedPower == null || equippedPower.downAttackAirFxPrefab == null)
            return;

        if (airFxAnchor == null)
            airFxAnchor = transform;

        // If already grounded, never spawn air FX
        if (jumper != null && jumper.IsGrounded)
            return;

        // Cancel any pending delayed spawn
        if (_airFxDelayCoroutine != null)
        {
            StopCoroutine(_airFxDelayCoroutine);
            _airFxDelayCoroutine = null;
        }

        // Kill any existing air FX instance
        StopAirDownAttackFxImmediate();

        float delay = Mathf.Max(0f, equippedPower.airFxDelay);
        _airFxDelayCoroutine = StartCoroutine(SpawnAirFxAfterDelay(delay));
    }

    private IEnumerator SpawnAirFxAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        _airFxDelayCoroutine = null;

        // Became grounded during delay? don't spawn.
        if (jumper != null && jumper.IsGrounded)
            yield break;

        // ---- existing spawn code (same as you already have) ----
        var prefab = equippedPower.downAttackAirFxPrefab;
        var go = GetInstance(prefab);

        _activeAirFxPrefab = prefab;
        _activeAirFxInstance = go;

        go.transform.SetParent(airFxAnchor, false);
        go.transform.localPosition = equippedPower.airFxLocalPositionOffset;
        go.transform.localRotation = Quaternion.Euler(equippedPower.airFxLocalRotationOffset);
        go.transform.localScale = (equippedPower.airFxLocalScale == Vector3.zero) ? Vector3.one : equippedPower.airFxLocalScale;

        if (!go.activeSelf) go.SetActive(true);

        float lifetime = PlayOneShot(go);

        if (_airFxReturnCoroutine != null)
            StopCoroutine(_airFxReturnCoroutine);

        _airFxReturnCoroutine = StartCoroutine(ReturnAirAfter(lifetime));
        // --------------------------------------------------------

        // Start per-frame grounded watchdog (this is the important fix)
        if (_airFxGroundWatchCoroutine != null)
            StopCoroutine(_airFxGroundWatchCoroutine);

        _airFxGroundWatchCoroutine = StartCoroutine(KillAirFxWhenGrounded());
    }

    private IEnumerator KillAirFxWhenGrounded()
    {
        while (_activeAirFxInstance != null)
        {
            if (jumper != null && jumper.IsGrounded)
            {
                StopAirDownAttackFxImmediate();
                yield break;
            }
            yield return null;
        }
    }

    public void StopAirDownAttackFxImmediate()
    {
        // cancel delayed spawn
        if (_airFxDelayCoroutine != null)
        {
            StopCoroutine(_airFxDelayCoroutine);
            _airFxDelayCoroutine = null;
        }

        // stop watchdog
        if (_airFxGroundWatchCoroutine != null)
        {
            StopCoroutine(_airFxGroundWatchCoroutine);
            _airFxGroundWatchCoroutine = null;
        }

        if (_airFxReturnCoroutine != null)
        {
            StopCoroutine(_airFxReturnCoroutine);
            _airFxReturnCoroutine = null;
        }

        if (_activeAirFxInstance == null || _activeAirFxPrefab == null)
            return;

        // HARD stop all particle systems so nothing keeps emitting
        var ps = _activeAirFxInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i] == null) continue;
            ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Return to pool instantly
        ReturnInstance(_activeAirFxPrefab, _activeAirFxInstance);

        _activeAirFxInstance = null;
        _activeAirFxPrefab = null;
    }

    private IEnumerator ReturnAirAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        // If we haven’t already stopped it due to ground impact:
        StopAirDownAttackFxImmediate();
    }

    /// <summary>
    /// Spawns the "AOE" ground impact FX at hitPoint when slam hits ground.
    /// </summary>
    public void SpawnGroundImpactAoeFx(Vector3 hitPoint, float gameplayRadius, Transform yawReference = null)
    {
        if (equippedPower == null || equippedPower.groundImpactAoeFxPrefab == null)
            return;

        var prefab = equippedPower.groundImpactAoeFxPrefab;
        var go = GetInstance(prefab);

        // world-space spawn (NOT parented)
        go.transform.SetParent(null, true);

        Vector3 pos = hitPoint;
        pos.y += equippedPower.groundFxYLift;
        go.transform.position = pos;
        // Rotation
        if (equippedPower.useFixedGroundRotation)
        {
            go.transform.rotation = Quaternion.Euler(equippedPower.groundFxFixedEulerRotation);
        }
        else if (equippedPower.matchPlayerYawIfNotFixed && yawReference != null)
        {
            // Keep it "ground-flat" but match yaw
            var e = equippedPower.groundFxFixedEulerRotation;
            e.y = yawReference.eulerAngles.y;
            go.transform.rotation = Quaternion.Euler(e);
        }
        else
        {
            go.transform.rotation = Quaternion.identity;
        }

        // optional radius-based scaling for rings/shockwaves
        if (equippedPower.scaleGroundFxByRadius)
        {
            float diameter = gameplayRadius * equippedPower.groundFxRadiusToScaleMultiplier;
            float s = diameter * equippedPower.groundFxScaleMultiplier;
            go.transform.localScale = new Vector3(s, s, s);
        }

        if (!go.activeSelf) go.SetActive(true);

        float lifetime = PlayOneShot(go);

        if (logDebug)
            Debug.Log($"[DownAttackPowerEquipper] SpawnGroundImpactAoeFx prefab='{prefab.name}' radius={gameplayRadius} lifetime={lifetime}", this);

        StartCoroutine(ReturnAfter(prefab, go, lifetime));
    }

    // --------- POOLING + PLAY HELPERS ---------

    private void EnsurePool(GameObject prefab, int size)
    {
        if (prefab == null) return;

        if (!_pools.TryGetValue(prefab, out var q) || q == null)
        {
            q = new Queue<GameObject>();
            _pools[prefab] = q;
        }

        while (q.Count < size)
        {
            var inst = Instantiate(prefab);
            inst.SetActive(false);

            // Add cache component so we don’t scan children every time
            if (inst.GetComponent<PooledParticleFx>() == null)
                inst.AddComponent<PooledParticleFx>();

            q.Enqueue(inst);
        }
    }

    private GameObject GetInstance(GameObject prefab)
    {
        if (!poolFx || prefab == null)
            return Instantiate(prefab);

        if (!_pools.TryGetValue(prefab, out var q) || q == null)
        {
            q = new Queue<GameObject>();
            _pools[prefab] = q;
            EnsurePool(prefab, 1); // create at least one
        }

        if (q.Count > 0)
            return q.Dequeue();

        // pool exhausted -> expand by 1
        var extra = Instantiate(prefab);
        extra.SetActive(false);

        if (extra.GetComponent<PooledParticleFx>() == null)
            extra.AddComponent<PooledParticleFx>();

        return extra;
    }

    private void ReturnInstance(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(null, true);

        if (!poolFx || prefab == null)
        {
            Destroy(instance);
            return;
        }

        if (!_pools.TryGetValue(prefab, out var q) || q == null)
        {
            q = new Queue<GameObject>();
            _pools[prefab] = q;
        }

        q.Enqueue(instance);
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnInstance(prefab, instance);
    }

    private float PlayOneShot(GameObject go)
    {
        // Fast path: cached particle list
        var cached = go.GetComponent<PooledParticleFx>();
        if (cached != null)
            return cached.PlayOneShot(true);

        // Fallback: scan once (still works if user forgets to add component)
        float maxLifetime = 0.4f;
        var psList = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            if (ps == null) continue;

            var main = ps.main;
            main.loop = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);

            float est = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            if (est > maxLifetime) maxLifetime = est;
        }

        return maxLifetime + 0.05f;
    }

    [System.Serializable]
    public class DownAttackPowerEntry
    {
        public DownAttackPowerId id;
        public DownAttackPowerDefinition definition;
    }
}

public static class DownAttackPowerSave
{
    private const string Key = "EquippedDownAttackPowerId";

    public static void Save(DownAttackPowerId id)
    {
        PlayerPrefs.SetInt(Key, (int)id);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out DownAttackPowerId id)
    {
        if (!PlayerPrefs.HasKey(Key))
        {
            id = DownAttackPowerId.None;
            return false;
        }

        id = (DownAttackPowerId)PlayerPrefs.GetInt(Key, (int)DownAttackPowerId.None);
        return true;
    }
}

// Note: saving/loading is currently unused since we don't have a proper "inventory" or
// "power selection" screen, but this is here for easy future integration if we add those
// features. For now, the equipped power will just be determined by the default value in the
// inspector or whatever is set by other gameplay scripts at runtime.
//DownAttackPowerSave.Save(id);
//downAttackPowerEquipper.EquipDownAttackPower(id);