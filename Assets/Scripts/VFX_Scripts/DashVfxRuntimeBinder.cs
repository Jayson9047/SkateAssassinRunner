using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

public class DashVfxRuntimeBinder : MonoBehaviour
{
    [Header("DashVFX prefab (the one with Dash1 VisualEffect + VFXPropertyBinder + VFXTransformBinder)")]
    [SerializeField] private GameObject dashVfxPrefab;

    [Header("Player hierarchy names")]
    [SerializeField] private string bodyName = "Body";
    [SerializeField] private string preferSmrNameContains = "Torso";

    [Header("VFX exposed property names (must match VFX Graph Blackboard)")]
    [SerializeField] private string smrPropertyName = "SkinnedMeshRenderer";
    [SerializeField] private string canDrawBoolName = "CanDrawTrail";

    [Header("VFX Transform Binder config (matches your inspector screenshot)")]
    [SerializeField] private string transformBinderTypeName = "VFXTransformBinder";
    [SerializeField] private string binderTargetMemberName = "Target";     // field or property on binder
    [SerializeField] private string binderPropertyMemberName = "Property"; // string
    [SerializeField] private string binderSpaceMemberName = "Space";       // enum
    [SerializeField] private string binderPropertyValue = "Transform";     // exposed property in VFX

    [Header("Temp: for testing")]
    [SerializeField] private bool forceCanDrawTrailTrueOnStart = true;

    private GameObject _dashVfxInstance;
    private VisualEffect _vfx;

    private IEnumerator Start()
    {
        // let player finish spawning/rig init
        yield return null;

        if (dashVfxPrefab == null)
        {
            Debug.LogError("[DashVfxRuntimeBinder] dashVfxPrefab not assigned.", this);
            yield break;
        }

        // 1) Find Body transform
        Transform body = FindDeepChild(transform, bodyName);
        if (body == null)
        {
            Debug.LogError($"[DashVfxRuntimeBinder] Could not find '{bodyName}' under '{name}'.", this);
            yield break;
        }

        // 2) Find Torso SMR (or fallback to biggest)
        SkinnedMeshRenderer smr = FindPreferredSmr(body, preferSmrNameContains);
        if (smr == null)
        {
            Debug.LogError("[DashVfxRuntimeBinder] No SkinnedMeshRenderer found under Body.", this);
            yield break;
        }

        // 3) Spawn DashVFX as WORLD object (do NOT parent to body)
        _dashVfxInstance = Instantiate(dashVfxPrefab);
        _dashVfxInstance.name = $"{dashVfxPrefab.name}__{name}";

        // Find VisualEffect inside spawned DashVFX
        _vfx = _dashVfxInstance.GetComponentInChildren<VisualEffect>(true);
        if (_vfx == null)
        {
            Debug.LogError("[DashVfxRuntimeBinder] Spawned DashVFX has no VisualEffect component.", this);
            yield break;
        }

        // Bind SkinnedMeshRenderer into VFX exposed property
        _vfx.SetSkinnedMeshRenderer(smrPropertyName, smr);

        // Force CanDrawTrail true for now
        if (forceCanDrawTrailTrueOnStart)
        {
            if (_vfx.HasBool(canDrawBoolName))
                _vfx.SetBool(canDrawBoolName, true);
            else
                Debug.LogWarning($"[DashVfxRuntimeBinder] Bool '{canDrawBoolName}' not found on '{_vfx.visualEffectAsset?.name}'.", this);
        }

        // Configure the VFXTransformBinder via reflection (since you can't reference its type)
        bool bound = TryBindTransformBinderTarget(_dashVfxInstance, transformBinderTypeName, body);
        if (!bound)
        {
            Debug.LogError("[DashVfxRuntimeBinder] Could not bind Body to VFXTransformBinder.Target. Check binder exists on prefab.", this);
            yield break;
        }

        _vfx.Reinit();
        _vfx.Play();

        Debug.Log($"[DashVfxRuntimeBinder] Bound dash trail. SMR='{smr.name}', Body='{body.name}', VFX='{_vfx.visualEffectAsset?.name}'", this);
    }

    // Call these later from your right-swipe dash start/end
    public void StartTrail()
    {
        if (_vfx == null) return;
        if (_vfx.HasBool(canDrawBoolName)) _vfx.SetBool(canDrawBoolName, true);
        _vfx.Play();
    }

    public void StopTrail()
    {
        if (_vfx == null) return;
        if (_vfx.HasBool(canDrawBoolName)) _vfx.SetBool(canDrawBoolName, false);
    }

    private bool TryBindTransformBinderTarget(GameObject root, string binderType, Transform target)
    {
        // Find any component whose type name == "VFXTransformBinder"
        Component binder = FindComponentByTypeName(root, binderType);
        if (binder == null) return false;

        var t = binder.GetType();

        // Ensure Property = "Transform" (matches your inspector)
        SetMemberIfExists(binder, t, binderPropertyMemberName, binderPropertyValue);

        // Ensure Space = Local if available (matches your inspector)
        // The enum type differs across packages, so we set by name if we can.
        SetEnumMemberIfExists(binder, t, binderSpaceMemberName, "Local");

        // Set Target = Body transform
        return SetMemberIfExists(binder, t, binderTargetMemberName, target);
    }

    private Component FindComponentByTypeName(GameObject root, string typeName)
    {
        // Includes inactive
        var comps = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null) continue;
            if (c.GetType().Name == typeName)
                return c;
        }
        return null;
    }

    private bool SetMemberIfExists(object instance, System.Type type, string memberName, object value)
    {
        // property first
        var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, value);
            return true;
        }

        // then field
        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(instance, value);
            return true;
        }

        return false;
    }

    private void SetEnumMemberIfExists(object instance, System.Type type, string memberName, string enumValueName)
    {
        var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite && prop.PropertyType.IsEnum)
        {
            try
            {
                object v = System.Enum.Parse(prop.PropertyType, enumValueName, true);
                prop.SetValue(instance, v);
            }
            catch { }
            return;
        }

        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType.IsEnum)
        {
            try
            {
                object v = System.Enum.Parse(field.FieldType, enumValueName, true);
                field.SetValue(instance, v);
            }
            catch { }
        }
    }

    private SkinnedMeshRenderer FindPreferredSmr(Transform body, string contains)
    {
        var smrs = body.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (smrs == null || smrs.Length == 0) return null;

        if (!string.IsNullOrWhiteSpace(contains))
        {
            foreach (var r in smrs)
                if (r != null && r.name.Contains(contains))
                    return r;
        }

        // fallback: biggest bounds
        SkinnedMeshRenderer best = smrs[0];
        float bestScore = 0f;
        foreach (var r in smrs)
        {
            if (r == null) continue;
            var b = r.bounds;
            float score = b.size.x * b.size.y * b.size.z;
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }
        return best;
    }

    private Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (_dashVfxInstance != null)
            Destroy(_dashVfxInstance);
    }
}