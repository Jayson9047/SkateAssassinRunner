using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns exactly one saved, owned Sword beneath the gameplay character's Sword socket.
/// </summary>
[DisallowMultipleComponent]
public sealed class SwordEquipper : MonoBehaviour
{
    [Header("Runtime Sword")]
    [SerializeField] private Transform swordSocket;
    [SerializeField] private GameObject staticSwordToDisable;
    [SerializeField] private SwordDefinition[] swordDefinitions;
    [SerializeField] private WeaponPowerEquipper weaponPowerEquipper;

    [Header("Debug")]
    [SerializeField] private bool logSwordDebug;

    private readonly Dictionary<SwordId, SwordDefinition> definitionsById =
        new Dictionary<SwordId, SwordDefinition>();

    private bool mappingBuilt;
    private SwordId currentSwordId = SwordId.Katana;
    private SwordDefinition currentDefinition;
    private GameObject currentSword;
    private WeaponIdentity currentWeaponIdentity;

    public SwordId CurrentSwordId => currentSwordId;
    public SwordDefinition CurrentDefinition => currentDefinition;
    public GameObject CurrentSword => currentSword;
    public WeaponIdentity CurrentWeaponIdentity => currentWeaponIdentity;

    private void OnEnable()
    {
        BuildDefinitionMapping();

        SwordId savedId;
        bool hasSavedSword = SwordSave.TryLoad(out savedId);
        SwordId resolvedId = ResolveSwordId(hasSavedSword ? savedId : SwordId.Katana);

        if (!hasSavedSword || resolvedId != savedId)
            SwordSave.Save(resolvedId);

        EquipSword(resolvedId);
    }

    private void OnDisable()
    {
        DestroyCurrentSword();
    }

    public void EquipSword(SwordId id)
    {
        BuildDefinitionMapping();
        SwordId resolvedId = ResolveSwordId(id);

        SwordDefinition definition;
        if (!definitionsById.TryGetValue(resolvedId, out definition) ||
            definition == null || definition.swordPrefab == null || swordSocket == null)
        {
            Debug.LogWarning("SwordEquipper could not equip a configured fallback Katana.", this);
            return;
        }

        DestroyCurrentSword();

        if (staticSwordToDisable != null)
            staticSwordToDisable.SetActive(false);

        currentSword = Instantiate(definition.swordPrefab);
        currentSword.name = definition.swordPrefab.name;
        currentSword.transform.SetParent(swordSocket, false);
        currentSword.transform.localPosition = definition.localPosition;
        currentSword.transform.localRotation = Quaternion.Euler(definition.localEulerAngles);
        currentSword.transform.localScale = definition.localScale == Vector3.zero
            ? Vector3.one
            : definition.localScale;

        currentSwordId = resolvedId;
        currentDefinition = definition;
        currentWeaponIdentity = currentSword.GetComponentInChildren<WeaponIdentity>(true);

        Transform auraAnchor = currentWeaponIdentity != null
            ? currentWeaponIdentity.AuraAnchor
            : null;

        if (currentWeaponIdentity == null || auraAnchor == null)
        {
            Debug.LogWarning(
                "Spawned Sword '" + resolvedId + "' is missing WeaponIdentity or its serialized aura anchor.",
                currentSword);
        }

        if (weaponPowerEquipper != null)
            weaponPowerEquipper.BindActiveWeapon(currentWeaponIdentity, auraAnchor);

        if (logSwordDebug)
        {
            Debug.Log(
                "[SwordEquipper] Equipped sword='" + resolvedId +
                "' weaponId='" + (currentWeaponIdentity != null ? currentWeaponIdentity.weaponId : "<missing>") + "'.",
                this);
        }
    }

    private SwordId ResolveSwordId(SwordId requestedId)
    {
        SwordDefinition definition;
        bool valid = SwordOwnershipSave.IsValidSwordId(requestedId) &&
                     SwordOwnershipSave.IsOwned(requestedId) &&
                     definitionsById.TryGetValue(requestedId, out definition) &&
                     definition != null &&
                     definition.swordPrefab != null;

        return valid ? requestedId : SwordId.Katana;
    }

    private void BuildDefinitionMapping()
    {
        if (mappingBuilt)
            return;

        mappingBuilt = true;
        definitionsById.Clear();

        if (swordDefinitions == null)
            return;

        for (int i = 0; i < swordDefinitions.Length; i++)
        {
            SwordDefinition definition = swordDefinitions[i];
            if (definition == null)
                continue;

            if (definitionsById.ContainsKey(definition.swordId))
                Debug.LogWarning("Duplicate Sword definition mapping for " + definition.swordId + ".", definition);

            definitionsById[definition.swordId] = definition;
        }
    }

    private void DestroyCurrentSword()
    {
        if (currentSword != null)
        {
            currentSword.SetActive(false);
            Destroy(currentSword);
        }

        currentSword = null;
        currentDefinition = null;
        currentWeaponIdentity = null;
    }
}
