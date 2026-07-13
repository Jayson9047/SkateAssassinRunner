using System.Collections;
using UnityEngine;

public class WeaponPowerEquipper : MonoBehaviour
{
    [Header("Where to attach the Aura")]
    [SerializeField] private Transform weaponAuraAnchor; // child on weapon

    [Header("Where slash FX should spawn from")]
    [SerializeField] private Transform slashFxSpawnAnchor; // child near blade tip or blade center

    [Header("Debug")]
    [SerializeField] private bool logWeaponPowerDebug = false;

    [Header("Weapon Power Registry (enum -> definition)")]
    [SerializeField] private WeaponPowerEntry[] weaponPowers;
    [SerializeField] private WeaponPowerId EquippedPowerId = WeaponPowerId.None;

    [Header("Pooling - Slash FX")]
    [SerializeField] private bool poolSlashFx = true;
    [SerializeField] private int slashPoolSize = 8;

    private readonly System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.Queue<GameObject>> _slashPools
        = new System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.Queue<GameObject>>();

    private GameObject _currentAuraInstance;
    private WeaponIdentity _weaponIdentityCached;
    private WeaponPowerDefinition equippedWeaponPower;
    public WeaponPowerDefinition EquippedWeaponPower => equippedWeaponPower;

    private void OnEnable()
    {
        CacheWeaponIdentity();
        var saved = WeaponPowerSave.TryLoad(out var id) ? id : EquippedPowerId;

        if (!WeaponPowerOwnershipSave.IsOwned(saved))
        {
            saved = WeaponPowerId.None;
            WeaponPowerSave.Save(saved);
        }

        EquipWeaponPower(saved);
    }

    private void CacheWeaponIdentity()
    {
        // Important: WeaponIdentity must be on a parent of the anchors (weapon root is best).
        _weaponIdentityCached = null;

        if (weaponAuraAnchor != null)
        {
            _weaponIdentityCached = weaponAuraAnchor.GetComponentInParent<WeaponIdentity>();
            if (_weaponIdentityCached == null)
            {
                // Try slash anchor as fallback
                if (slashFxSpawnAnchor != null)
                    _weaponIdentityCached = slashFxSpawnAnchor.GetComponentInParent<WeaponIdentity>();
            }
        }
        else if (slashFxSpawnAnchor != null)
        {
            _weaponIdentityCached = slashFxSpawnAnchor.GetComponentInParent<WeaponIdentity>();
        }
    }

    private string GetWeaponId()
    {
        if (_weaponIdentityCached == null) CacheWeaponIdentity();
        return _weaponIdentityCached != null ? _weaponIdentityCached.weaponId : null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Intentionally empty: avoid edit-mode instantiation warnings.
        // Use the context menu button below.
    }
#endif

    [ContextMenu("Apply Equipped Power")]
    public void ApplyEquippedPower()
    {
        CacheWeaponIdentity();

        // clear old aura
        if (_currentAuraInstance != null)
        {
            DestroyImmediateSafe(_currentAuraInstance);
            _currentAuraInstance = null;
        }

        if (equippedWeaponPower == null || equippedWeaponPower.weaponAuraPrefab == null || weaponAuraAnchor == null)
            return;

        string weaponId = GetWeaponId();

        // spawn aura (instantiate first, then parent with worldPositionStays=false)
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _currentAuraInstance = UnityEditor.PrefabUtility.InstantiatePrefab(equippedWeaponPower.weaponAuraPrefab) as GameObject;
        }
        else
        {
            _currentAuraInstance = Instantiate(equippedWeaponPower.weaponAuraPrefab);
        }
#else
        _currentAuraInstance = Instantiate(equippedWeaponPower.weaponAuraPrefab);
#endif

        if (_currentAuraInstance == null)
            return;

        _currentAuraInstance.transform.SetParent(weaponAuraAnchor, false);

        // USE PER-WEAPON OVERRIDES (falls back to defaults if no override exists)
        Vector3 pos = equippedWeaponPower.GetAuraPos(weaponId);
        Vector3 rot = equippedWeaponPower.GetAuraRot(weaponId);
        Vector3 scl = equippedWeaponPower.GetAuraScale(weaponId); // has zero-scale fallback

        _currentAuraInstance.transform.localPosition = pos;
        _currentAuraInstance.transform.localRotation = Quaternion.Euler(rot);
        _currentAuraInstance.transform.localScale = scl;

        // Make sure it’s active
        if (!_currentAuraInstance.activeSelf)
            _currentAuraInstance.SetActive(true);

        // Force play in case prefab has PlayOnAwake off
        ForcePlayAll(_currentAuraInstance);

        if (logWeaponPowerDebug)
        {
            Debug.Log($"[WeaponPowerEquipper] Applied power='{equippedWeaponPower.powerId}' weaponId='{weaponId}' " +
                      $"AuraPos={pos} AuraRot={rot} AuraScale={scl}", this);
        }
    }

    public void SetEquippedPower(WeaponPowerDefinition power)
    {
        equippedWeaponPower = power;
        ApplyEquippedPower();
    }

    public void SpawnSlashFx()
    {
        if (equippedWeaponPower == null)
        {
            if (logWeaponPowerDebug) Debug.Log("[WeaponPowerEquipper] SpawnSlashFx aborted: equippedWeaponPower is NULL", this);
            return;
        }

        if (equippedWeaponPower.slashFxPrefab == null)
        {
            if (logWeaponPowerDebug) Debug.Log("[WeaponPowerEquipper] SpawnSlashFx aborted: slashFxPrefab is NULL on power '" + equippedWeaponPower.powerId + "'", this);
            return;
        }

        if (slashFxSpawnAnchor == null)
        {
            if (logWeaponPowerDebug) Debug.Log("[WeaponPowerEquipper] SpawnSlashFx aborted: slashFxSpawnAnchor is NULL", this);
            return;
        }

        string weaponId = GetWeaponId();

        // Use per-weapon overrides if present (fallback to defaults)
        Vector3 slashPosOffset = equippedWeaponPower.GetSlashPos(weaponId);
        Vector3 slashRotOffset = equippedWeaponPower.GetSlashRot(weaponId);
        Vector3 slashScale = equippedWeaponPower.GetSlashScale(weaponId);

        Vector3 pos = slashFxSpawnAnchor.position + slashFxSpawnAnchor.TransformVector(slashPosOffset);
        Quaternion rot = slashFxSpawnAnchor.rotation * Quaternion.Euler(slashRotOffset);

        // Parent it so it doesn't drift and you can see it under VFXSlashSpawn
        var prefab = equippedWeaponPower.slashFxPrefab;
        var go = GetSlashInstance(prefab);

        // parent + set transform (don’t rely on Instantiate overload anymore)
        go.transform.SetParent(slashFxSpawnAnchor, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = slashScale;

        if (!go.activeSelf)
            go.SetActive(true);

        // Force non-looping + play (prevents “why is it looping??” forever)
        float lifetime = 1.5f;

        var psList = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            var main = ps.main;
            main.loop = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);

            float est = main.duration + main.startLifetime.constantMax;
            if (est > lifetime) lifetime = est;
        }

#if UNITY_2019_3_OR_NEWER
        var vfxList = go.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        for (int i = 0; i < vfxList.Length; i++)
            vfxList[i].Play();
#endif

        if (logWeaponPowerDebug)
        {
            Debug.Log($"[WeaponPowerEquipper] SpawnSlashFx OK power='{equippedWeaponPower.powerId}' weaponId='{weaponId}' " +
                      $"pos={pos} rot={rot.eulerAngles} scale={slashScale}", this);
        }

        StartCoroutine(ReturnSlashAfter(prefab, go, 0.4f));
    }

    // ---- Helpers ----

    private void ForcePlayAll(GameObject root)
    {
        var psList = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
            psList[i].Play(true);

#if UNITY_2019_3_OR_NEWER
        var vfxList = root.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        for (int i = 0; i < vfxList.Length; i++)
            vfxList[i].Play();
#endif
    }

    // Returns estimated lifetime for cleanup
    private float MakeOneShotAndPlay(GameObject root)
    {
        float maxLifetime = 1.5f;

        // Particle Systems
        var psList = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            var main = ps.main;

            // force non-looping
            main.loop = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);

            float est = main.duration + main.startLifetime.constantMax;
            if (est > maxLifetime) maxLifetime = est;
        }

#if UNITY_2019_3_OR_NEWER
        // VFX Graph: we can't reliably read duration, so we just play it.
        var vfxList = root.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        for (int i = 0; i < vfxList.Length; i++)
            vfxList[i].Play();
#endif

        return maxLifetime + 0.1f;
    }

    private void DestroyImmediateSafe(GameObject go)
    {
        if (go == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(go);
        else Destroy(go);
#else
        Destroy(go);
#endif
    }


    private System.Collections.Generic.Dictionary<WeaponPowerId, WeaponPowerDefinition> _powerMap;

    private void BuildPowerMapIfNeeded()
    {
        if (_powerMap != null) return;

        _powerMap = new System.Collections.Generic.Dictionary<WeaponPowerId, WeaponPowerDefinition>();
        if (weaponPowers == null) return;

        for (int i = 0; i < weaponPowers.Length; i++)
        {
            var e = weaponPowers[i];
            if (e == null) continue;
            if (e.definition == null) continue;

            // last one wins if duplicates exist
            _powerMap[e.id] = e.definition;
        }
    }

    public void EquipWeaponPower(WeaponPowerId id)
    {
        BuildPowerMapIfNeeded();

        EquippedPowerId = id;

        if (_powerMap != null && _powerMap.TryGetValue(id, out var def))
            equippedWeaponPower = def;
        else
            equippedWeaponPower = null;

        ApplyEquippedPower();
    }
    private GameObject GetSlashInstance(GameObject prefab)
    {
        if (!poolSlashFx || prefab == null)
            return Instantiate(prefab);

        if (!_slashPools.TryGetValue(prefab, out var q) || q == null)
        {
            q = new System.Collections.Generic.Queue<GameObject>();
            _slashPools[prefab] = q;

            for (int i = 0; i < slashPoolSize; i++)
            {
                var go = Instantiate(prefab);
                go.SetActive(false);
                q.Enqueue(go);
            }
        }

        if (q.Count > 0)
            return q.Dequeue();

        // Pool exhausted (spam). Expand by 1.
        var extra = Instantiate(prefab);
        extra.SetActive(false);
        return extra;
    }

    private void ReturnSlashInstance(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;

        instance.SetActive(false);

        if (!poolSlashFx || prefab == null)
        {
            Destroy(instance);
            return;
        }

        if (!_slashPools.TryGetValue(prefab, out var q) || q == null)
        {
            q = new System.Collections.Generic.Queue<GameObject>();
            _slashPools[prefab] = q;
        }

        q.Enqueue(instance);
    }

    private IEnumerator ReturnSlashAfter(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnSlashInstance(prefab, instance);
    }
    public WeaponPowerId GetEquippedWeaponPowerId()
    {
        // If you keep equippedWeaponPower.weaponPowerId accurate, return it:
        if (equippedWeaponPower != null) return equippedWeaponPower.weaponPowerId;
        return WeaponPowerId.None;
    }
    [System.Serializable]
    public class WeaponPowerEntry
    {
        public WeaponPowerId id;
        public WeaponPowerDefinition definition;
    }
}

public static class WeaponPowerSave
{
    private const string Key = "EquippedWeaponPowerId";

    public static void Save(WeaponPowerId id)
    {
        PlayerPrefs.SetInt(Key, (int)id);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out WeaponPowerId id)
    {
        if (!PlayerPrefs.HasKey(Key))
        {
            id = WeaponPowerId.None;
            return false;
        }

        id = (WeaponPowerId)PlayerPrefs.GetInt(Key, (int)WeaponPowerId.None);
        return true;
    }
}

//TODO: for saving from inventory equipment, just do the following and you're good:
//WeaponPowerSave.Save(id);
//weaponPowerEquipper.EquipWeaponPower(id);

