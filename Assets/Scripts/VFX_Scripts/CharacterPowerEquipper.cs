using UnityEngine;

public class CharacterPowerEquipper : MonoBehaviour
{
    [Header("Where to attach the Character Aura")]
    [SerializeField] private Transform characterAuraAnchor; // child on player

    [Header("Where to attach the Dash Trail")]
    [SerializeField] private Transform dashTrailAnchor; // child on player (behind feet / spine / etc.)

    [Header("Debug")]
    [SerializeField] private bool logCharacterPowerDebug = false;

    [Header("Character Power Registry (enum -> definition)")]
    [SerializeField] private CharacterPowerEntry[] characterPowers;
    [SerializeField] private CharacterPowerId EquippedPowerId = CharacterPowerId.None;

    private GameObject _currentAuraInstance;
    private GameObject _currentDashTrailInstance;

    private CharacterPowerDefinition equippedCharacterPower;
    public CharacterPowerDefinition EquippedCharacterPower => equippedCharacterPower;

    private System.Collections.Generic.Dictionary<CharacterPowerId, CharacterPowerDefinition> _powerMap;

    private void OnEnable()
    {
        var saved = CharacterPowerSave.TryLoad(out var id) ? id : EquippedPowerId;
        EquipCharacterPower(saved);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Intentionally empty: avoid edit-mode instantiation warnings.
        // Use context menu button below.
    }
#endif

    [ContextMenu("Apply Equipped Power")]
    public void ApplyEquippedPower()
    {
        // Clear old aura
        if (_currentAuraInstance != null)
        {
            DestroyImmediateSafe(_currentAuraInstance);
            _currentAuraInstance = null;
        }

        // Clear old dash trail
        if (_currentDashTrailInstance != null)
        {
            DestroyImmediateSafe(_currentDashTrailInstance);
            _currentDashTrailInstance = null;
        }

        if (equippedCharacterPower == null)
            return;

        // ---- Aura ----
        if (equippedCharacterPower.characterAuraPrefab != null && characterAuraAnchor != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                _currentAuraInstance = UnityEditor.PrefabUtility.InstantiatePrefab(equippedCharacterPower.characterAuraPrefab) as GameObject;
            else
                _currentAuraInstance = Instantiate(equippedCharacterPower.characterAuraPrefab);
#else
            _currentAuraInstance = Instantiate(equippedCharacterPower.characterAuraPrefab);
#endif

            if (_currentAuraInstance != null)
            {
                _currentAuraInstance.transform.SetParent(characterAuraAnchor, false);
                _currentAuraInstance.transform.localPosition = equippedCharacterPower.auraLocalPositionOffset;
                _currentAuraInstance.transform.localRotation = Quaternion.Euler(equippedCharacterPower.auraLocalRotationOffset);
                _currentAuraInstance.transform.localScale =
                    (equippedCharacterPower.auraLocalScale == Vector3.zero) ? Vector3.one : equippedCharacterPower.auraLocalScale;

                if (!_currentAuraInstance.activeSelf)
                    _currentAuraInstance.SetActive(true);

                ForcePlayAll(_currentAuraInstance);
            }
        }

        // ---- Dash Trail (prepared but OFF by default) ----
        if (equippedCharacterPower.dashTrailPrefab != null && dashTrailAnchor != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                _currentDashTrailInstance = UnityEditor.PrefabUtility.InstantiatePrefab(equippedCharacterPower.dashTrailPrefab) as GameObject;
            else
                _currentDashTrailInstance = Instantiate(equippedCharacterPower.dashTrailPrefab);
#else
            _currentDashTrailInstance = Instantiate(equippedCharacterPower.dashTrailPrefab);
#endif

            if (_currentDashTrailInstance != null)
            {
                _currentDashTrailInstance.transform.SetParent(dashTrailAnchor, false);
                _currentDashTrailInstance.transform.localPosition = equippedCharacterPower.dashTrailLocalPositionOffset;
                _currentDashTrailInstance.transform.localRotation = Quaternion.Euler(equippedCharacterPower.dashTrailLocalRotationOffset);
                _currentDashTrailInstance.transform.localScale =
                    (equippedCharacterPower.dashTrailLocalScale == Vector3.zero) ? Vector3.one : equippedCharacterPower.dashTrailLocalScale;

                // Keep it OFF until dash begins
                _currentDashTrailInstance.SetActive(false);
            }
        }

        if (logCharacterPowerDebug)
        {
            Debug.Log($"[CharacterPowerEquipper] Applied power='{(equippedCharacterPower != null ? equippedCharacterPower.powerId : "NULL")}' " +
                      $"Aura={(_currentAuraInstance != null)} Trail={(_currentDashTrailInstance != null)}", this);
        }
    }

    public void StartDashTrail()
    {
        if (_currentDashTrailInstance == null)
            return;

        if (!_currentDashTrailInstance.activeSelf)
            _currentDashTrailInstance.SetActive(true);

        ForcePlayAll(_currentDashTrailInstance);

        if (logCharacterPowerDebug)
            Debug.Log("[CharacterPowerEquipper] StartDashTrail()", this);
    }

    public void StopDashTrail()
    {
        if (_currentDashTrailInstance == null)
            return;

        // Stop particles/VFX cleanly, then disable
        StopAll(_currentDashTrailInstance);
        _currentDashTrailInstance.SetActive(false);

        if (logCharacterPowerDebug)
            Debug.Log("[CharacterPowerEquipper] StopDashTrail()", this);
    }

    public void EquipCharacterPower(CharacterPowerId id)
    {
        BuildPowerMapIfNeeded();

        EquippedPowerId = id;

        if (_powerMap != null && _powerMap.TryGetValue(id, out var def))
            equippedCharacterPower = def;
        else
            equippedCharacterPower = null;

        ApplyEquippedPower();
    }

    private void BuildPowerMapIfNeeded()
    {
        if (_powerMap != null) return;

        _powerMap = new System.Collections.Generic.Dictionary<CharacterPowerId, CharacterPowerDefinition>();
        if (characterPowers == null) return;

        for (int i = 0; i < characterPowers.Length; i++)
        {
            var e = characterPowers[i];
            if (e == null) continue;
            if (e.definition == null) continue;

            // last one wins if duplicates exist
            _powerMap[e.id] = e.definition;
        }
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

    private void StopAll(GameObject root)
    {
        var psList = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
            psList[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);

#if UNITY_2019_3_OR_NEWER
        var vfxList = root.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
        for (int i = 0; i < vfxList.Length; i++)
            vfxList[i].Stop();
#endif
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

    [System.Serializable]
    public class CharacterPowerEntry
    {
        public CharacterPowerId id;
        public CharacterPowerDefinition definition;
    }
}

public static class CharacterPowerSave
{
    private const string Key = "EquippedCharacterPowerId";

    public static void Save(CharacterPowerId id)
    {
        PlayerPrefs.SetInt(Key, (int)id);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out CharacterPowerId id)
    {
        if (!PlayerPrefs.HasKey(Key))
        {
            id = CharacterPowerId.None;
            return false;
        }

        id = (CharacterPowerId)PlayerPrefs.GetInt(Key, (int)CharacterPowerId.None);
        return true;
    }
}