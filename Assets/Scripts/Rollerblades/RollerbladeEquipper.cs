using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns exactly one complete, saved and owned Rollerblade pair on the gameplay character.
/// The original static pair remains a safe fallback until both dynamic sides spawn successfully.
/// </summary>
[DisallowMultipleComponent]
public sealed class RollerbladeEquipper : MonoBehaviour
{
    [Header("Runtime Sockets")]
    [SerializeField] private Transform leftSocket;
    [SerializeField] private Transform rightSocket;

    [Header("Original Static Pair")]
    [SerializeField] private GameObject staticLeftRollerblade;
    [SerializeField] private GameObject staticRightRollerblade;

    [Header("Definitions")]
    [SerializeField] private RollerbladeDefinition[] rollerbladeDefinitions;

    [Header("Debug")]
    [SerializeField] private bool logRollerbladeDebug;

    private readonly Dictionary<RollerbladeId, RollerbladeDefinition> definitionsById =
        new Dictionary<RollerbladeId, RollerbladeDefinition>();

    private bool mappingBuilt;
    private RollerbladeId currentRollerbladeId = RollerbladeId.Default;
    private RollerbladeDefinition currentDefinition;
    private GameObject currentLeftRollerblade;
    private GameObject currentRightRollerblade;

    public RollerbladeId CurrentRollerbladeId => currentRollerbladeId;
    public RollerbladeDefinition CurrentDefinition => currentDefinition;
    public GameObject CurrentLeftRollerblade => currentLeftRollerblade;
    public GameObject CurrentRightRollerblade => currentRightRollerblade;

    private void OnEnable()
    {
        BuildDefinitionMapping();

        RollerbladeId savedId;
        bool hasSavedPair = RollerbladeSave.TryLoad(out savedId);
        RollerbladeId resolvedId = ResolveRollerbladeId(
            hasSavedPair ? savedId : RollerbladeId.Default);

        if (!hasSavedPair || resolvedId != savedId)
            RollerbladeSave.Save(resolvedId);

        EquipRollerblades(resolvedId);
    }

    private void OnDisable()
    {
        DestroyCurrentPair();
    }

    public void EquipRollerblades(RollerbladeId id)
    {
        BuildDefinitionMapping();
        RollerbladeId resolvedId = ResolveRollerbladeId(id);

        RollerbladeDefinition definition;
        if (!TryGetCompleteDefinition(resolvedId, out definition))
        {
            Debug.LogWarning("RollerbladeEquipper could not find a complete Default pair.", this);
            return;
        }

        GameObject newLeft;
        GameObject newRight;
        if (!TrySpawnPair(definition, out newLeft, out newRight))
        {
            if (resolvedId != RollerbladeId.Default &&
                TryGetCompleteDefinition(RollerbladeId.Default, out definition) &&
                TrySpawnPair(definition, out newLeft, out newRight))
            {
                resolvedId = RollerbladeId.Default;
                RollerbladeSave.Save(resolvedId);
            }
            else
            {
                Debug.LogWarning(
                    "RollerbladeEquipper kept the existing/static pair because a complete pair could not spawn.",
                    this);
                return;
            }
        }

        DestroyCurrentPair();

        currentLeftRollerblade = newLeft;
        currentRightRollerblade = newRight;
        currentRollerbladeId = resolvedId;
        currentDefinition = definition;

        if (staticLeftRollerblade != null)
            staticLeftRollerblade.SetActive(false);
        if (staticRightRollerblade != null)
            staticRightRollerblade.SetActive(false);

        if (logRollerbladeDebug)
        {
            Debug.Log(
                "[RollerbladeEquipper] Equipped pair='" + resolvedId + "'.",
                this);
        }
    }

    private bool TrySpawnPair(
        RollerbladeDefinition definition,
        out GameObject spawnedLeft,
        out GameObject spawnedRight)
    {
        spawnedLeft = null;
        spawnedRight = null;

        if (definition == null || definition.leftPrefab == null || definition.rightPrefab == null ||
            leftSocket == null || rightSocket == null)
        {
            return false;
        }

        try
        {
            spawnedLeft = Instantiate(definition.leftPrefab, leftSocket, false);
            spawnedRight = Instantiate(definition.rightPrefab, rightSocket, false);

            spawnedLeft.name = "P_Roller_Left";
            spawnedRight.name = "P_Roller_Right";

            ApplySideTransform(spawnedLeft.transform, definition.leftTransform);
            ApplySideTransform(spawnedRight.transform, definition.rightTransform);
            SetLayerRecursively(spawnedLeft, leftSocket.gameObject.layer);
            SetLayerRecursively(spawnedRight, rightSocket.gameObject.layer);
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "RollerbladeEquipper failed to spawn a complete pair: " + exception.Message,
                this);

            DestroySpawnedObject(spawnedLeft);
            DestroySpawnedObject(spawnedRight);
            spawnedLeft = null;
            spawnedRight = null;
            return false;
        }
    }

    private RollerbladeId ResolveRollerbladeId(RollerbladeId requestedId)
    {
        RollerbladeDefinition definition;
        bool valid = RollerbladeOwnershipSave.IsValidRollerbladeId(requestedId) &&
                     RollerbladeOwnershipSave.IsOwned(requestedId) &&
                     TryGetCompleteDefinition(requestedId, out definition);

        return valid ? requestedId : RollerbladeId.Default;
    }

    private bool TryGetCompleteDefinition(
        RollerbladeId id,
        out RollerbladeDefinition definition)
    {
        return definitionsById.TryGetValue(id, out definition) &&
               definition != null &&
               definition.leftPrefab != null &&
               definition.rightPrefab != null;
    }

    private void BuildDefinitionMapping()
    {
        if (mappingBuilt)
            return;

        mappingBuilt = true;
        definitionsById.Clear();

        if (rollerbladeDefinitions == null)
            return;

        for (int i = 0; i < rollerbladeDefinitions.Length; i++)
        {
            RollerbladeDefinition definition = rollerbladeDefinitions[i];
            if (definition == null)
                continue;

            if (definitionsById.ContainsKey(definition.rollerbladeId))
            {
                Debug.LogWarning(
                    "Duplicate Rollerblade definition mapping for " + definition.rollerbladeId + ".",
                    definition);
            }

            definitionsById[definition.rollerbladeId] = definition;
        }
    }

    private static void ApplySideTransform(
        Transform target,
        RollerbladeSideTransform configuredTransform)
    {
        target.localPosition = configuredTransform.localPosition;
        target.localRotation = Quaternion.Euler(configuredTransform.localEulerAngles);
        target.localScale = configuredTransform.localScale == Vector3.zero
            ? Vector3.one
            : configuredTransform.localScale;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
    }

    private void DestroyCurrentPair()
    {
        DestroySpawnedObject(currentLeftRollerblade);
        DestroySpawnedObject(currentRightRollerblade);
        currentLeftRollerblade = null;
        currentRightRollerblade = null;
        currentDefinition = null;
    }

    private static void DestroySpawnedObject(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(false);
        Destroy(instance);
    }
}
