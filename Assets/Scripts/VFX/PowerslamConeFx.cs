using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Bounded, prewarmed presentation for the charged Phase 1 Powerslam.</summary>
[DisallowMultipleComponent]
public sealed class PowerslamConeFx : MonoBehaviour
{
    [Tooltip("Disables charged slam air FX, ground FX, distortion and kill flash. Damage and normal Ground Slam are unaffected.")]
    [FormerlySerializedAs("useConeTrial")]
    [SerializeField] private bool enablePowerslamEffects = true;
    [SerializeField] private WeaponPowerEquipper weaponPowerEquipper;
    [FormerlySerializedAs("impactPrefab")]
    [SerializeField] private GameObject defaultImpactPrefab;
    [SerializeField] private AbilityImpact[] abilityImpacts;
    [SerializeField] private float defaultImpactScale = 1f;
    [System.Serializable]
    public struct AbilityImpact
    {
        public WeaponPowerId ability;
        public GameObject prefab;
    }
    public bool EffectsEnabled => isActiveAndEnabled && enablePowerslamEffects;
    [Tooltip("The authored cone points along local +Z. The runner advances along world +X.")]
    [SerializeField] private Vector3 forwardDirection = Vector3.right;
    [Tooltip("Scale at the seven-unit Powerslam radius; independent of the player transform.")]
    [SerializeField] private float scaleAtSevenUnitRadius = 0.6f;
    [SerializeField] private float groundLift = 0.035f;
    [SerializeField] private float returnAfterSeconds = 1.65f;

    private const int PoolSize = 2;
    private sealed class Slot
    {
        public GameObject Root;
        public ParticleSystem[] Particles;
        public float Remaining;
        public Transform Ground;
        public MovingObject GroundMotion;
        public Vector3 LastGroundPosition;
        public PowerslamEarthBurst EarthBurst;
    }

    private Slot[] _slots;
    private int _nextSlot;
    private bool _hasActiveFx;
    private GameObject _pooledPrefab;

    private void Start()
    {
        // OnEnable has finished binding the actual equipped sword power before prewarming.
        if (weaponPowerEquipper == null) weaponPowerEquipper = GetComponentInChildren<WeaponPowerEquipper>(true);
        if (EffectsEnabled) Prewarm(ResolvePrefab(out _));
    }

    public GameObject ResolvePrefab(out bool elemental)
    {
        if (weaponPowerEquipper == null) weaponPowerEquipper = GetComponentInChildren<WeaponPowerEquipper>(true);
        WeaponPowerId ability = weaponPowerEquipper != null
            ? weaponPowerEquipper.GetEquippedWeaponPowerId() : WeaponPowerId.None;
        elemental = false;
        if (ability != WeaponPowerId.None && abilityImpacts != null)
            for (int i = 0; i < abilityImpacts.Length; i++)
                if (abilityImpacts[i].ability == ability && abilityImpacts[i].prefab != null)
                {
                    elemental = true;
                    return abilityImpacts[i].prefab;
                }
        return defaultImpactPrefab;
    }

    private void Prewarm(GameObject prefab)
    {
        if (prefab == null || (_slots != null && _pooledPrefab == prefab)) return;
        // Keep one two-instance pool, rather than twelve copies of six expensive effects.
        DisposePool();
        _pooledPrefab = prefab;
        _slots = new Slot[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            // Inactive copies live with their owner. Active copies are detached at impact.
            GameObject root = Instantiate(prefab, transform);
            root.name = prefab.name + " (pooled)";
            root.SetActive(false);
            _slots[i] = new Slot
            {
                Root = root,
                Particles = root.GetComponentsInChildren<ParticleSystem>(true),
                EarthBurst = root.GetComponent<PowerslamEarthBurst>()
            };
        }
    }

    /// <returns>True when a charged Phase 1 impact was presented.</returns>
    public bool TryPlayImpact(Vector3 groundPoint, float gameplayRadius, bool isPowerSlam,
        Transform groundSurface = null)
    {
        if (!isPowerSlam || !EffectsEnabled)
            return false;

        // The Phase 2 cinematic down attack has its own presentation, even if charge remains.
        var level = LevelManager.Instance as SkateAssassinRunnerLevelManager;
        if (level != null && level.IsPhase2BossActive) return false;

        GameObject prefab = ResolvePrefab(out bool elemental);
        if (prefab == null) return false;
        Prewarm(prefab);
        Slot slot = _slots[_nextSlot];
        _nextSlot = (_nextSlot + 1) % PoolSize;
        Clear(slot);

        Vector3 forward = Vector3.ProjectOnPlane(forwardDirection, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.right;
        Transform root = slot.Root.transform;
        root.SetParent(null, false);
        root.SetPositionAndRotation(groundPoint + Vector3.up * groundLift,
            Quaternion.LookRotation(forward.normalized, Vector3.up));
        float scale = (elemental ? scaleAtSevenUnitRadius : defaultImpactScale) * Mathf.Max(0.1f, gameplayRadius) / 7f;
        root.localScale = Vector3.one * scale;
        slot.GroundMotion = groundSurface != null ? groundSurface.GetComponentInParent<MovingObject>() : null;
        slot.Ground = slot.GroundMotion != null ? slot.GroundMotion.transform : groundSurface;
        slot.LastGroundPosition = slot.Ground != null ? slot.Ground.position : Vector3.zero;
        slot.Root.SetActive(true);
        if (slot.EarthBurst != null) slot.EarthBurst.Play();
        // Play each cached emitter once. Recursive Play per child can restart shared descendants.
        for (int i = 0; i < slot.Particles.Length; i++) slot.Particles[i].Play(false);
        slot.Remaining = returnAfterSeconds;
        _hasActiveFx = true;
        return true;
    }

    private void LateUpdate()
    {
        if (_slots == null || !_hasActiveFx) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            Slot slot = _slots[i];
            if (slot.Remaining <= 0f || slot.Ground == null) continue;
            // Snapshot the real ground translation after its Update. This avoids a frame of
            // drift when level speed changes between different scripts' Update callbacks.
            Vector3 current = slot.Ground.position;
            Vector3 movement = current - slot.LastGroundPosition;
            if (slot.GroundMotion != null)
            {
                Vector3 expected = slot.GroundMotion.isActiveAndEnabled
                    ? slot.GroundMotion.Movement : Vector3.zero;
                // Pooled road chunks can teleport when recycled; flames stay at the impact site.
                if (movement.magnitude > Mathf.Max(1f, expected.magnitude * 2f)) movement = expected;
            }
            slot.Root.transform.position += movement;
            slot.LastGroundPosition = current;
        }
    }

    private void Update()
    {
        if (_slots == null || !_hasActiveFx) return;
        if (!EffectsEnabled) { OnDisable(); return; }
        float delta = Time.deltaTime;
        bool anyActive = false;
        for (int i = 0; i < _slots.Length; i++)
        {
            Slot slot = _slots[i];
            if (slot.Remaining <= 0f) continue;
            slot.Remaining -= delta;
            if (slot.Remaining <= 0f) Clear(slot);
            else anyActive = true;
        }
        _hasActiveFx = anyActive;
    }

    private void Clear(Slot slot)
    {
        if (slot.EarthBurst != null) slot.EarthBurst.Clear();
        for (int i = 0; i < slot.Particles.Length; i++)
            slot.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        slot.Root.SetActive(false);
        slot.Root.transform.SetParent(transform, false);
        slot.Remaining = 0f;
        slot.Ground = null;
        slot.GroundMotion = null;
    }

    private void OnDisable()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++) Clear(_slots[i]);
        _hasActiveFx = false;
    }

    private void OnDestroy()
    {
        DisposePool();
    }

    private void DisposePool()
    {
        if (_slots == null) return;
        // Detached instances must not survive a character or scene being destroyed.
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i].Root != null)
            {
                _slots[i].Root.SetActive(false);
                Destroy(_slots[i].Root);
            }
        _slots = null;
        _pooledPrefab = null;
        _nextSlot = 0;
        _hasActiveFx = false;
    }
}
