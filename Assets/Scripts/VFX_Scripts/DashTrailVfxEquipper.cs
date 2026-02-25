using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

public class DashTrailVfxEquipper : MonoBehaviour
{
    [Header("DashVFX prefab (world object)")]
    [SerializeField] private GameObject dashVfxPrefab;

    [Header("Definitions (enum -> VisualEffectAsset)")]
    [SerializeField] private DashTrailVfxDefinition[] definitions;

    [Header("Find in player hierarchy")]
    [SerializeField] private string bodyName = "Body";
    [SerializeField] private string preferSmrNameContains = "Torso";

    [Header("VFX exposed property names (must match graph blackboard)")]
    [SerializeField] private string smrPropertyName = "SkinnedMeshRenderer";
    [SerializeField] private string canDrawBoolName = "CanDrawTrail";

    [Header("State")]
    [SerializeField] private DashTrailId equippedId = DashTrailId.None;

    [Header("Binder Target (player)")]
    [SerializeField] private string trailFxAnchorName = "TrailFxAnchor";
    [SerializeField] private string transformBinderTypeName = "VFXTransformBinder";
    [SerializeField] private string binderTargetMemberName = "Target";
    [SerializeField] private string binderPropertyMemberName = "Property";
    [SerializeField] private string binderSpaceMemberName = "Space";
    [SerializeField] private string binderPropertyValue = "Transform"; // exposed property in VFX graph

    private DashTrailVfxDefinition _equippedDef;
    private Transform _trailFxAnchor;
    // runtime
    private GameObject _dashVfxInstance;
    private VisualEffect _vfx;
    private Transform _body;
    private SkinnedMeshRenderer _smr;

    private System.Collections.Generic.Dictionary<DashTrailId, DashTrailVfxDefinition> _map;

    private IEnumerator Start()
    {
        yield return null; // let player finish spawn/rig init

        BuildMapIfNeeded();
        CachePlayerRefs();

        // Spawn once and bind once
        EnsureVfxInstance();

        // Equip default (or saved later)
        Equip(equippedId);
    }

    public void Equip(DashTrailId id)
    {
        BuildMapIfNeeded();
        equippedId = id;

        if (_vfx == null)
            EnsureVfxInstance();

        if (_vfx == null)
            return;

        if (!_map.TryGetValue(id, out var def) || def == null || def.vfxAsset == null)
        {
            // None / missing: just disable drawing
            SafeSetBool(_vfx, canDrawBoolName, false);
            return;
        }

        // Swap the graph
        _vfx.visualEffectAsset = def.vfxAsset;

        _equippedDef = def;

        // Swap graph
        _vfx.visualEffectAsset = def.vfxAsset;

        // Rebind using THIS graph's property names
        RebindGraphInputs(def);

        // Default draw state
        SafeSetBool(_vfx, def.canDrawTrailBool, def.setCanDrawTrailOnEquip);

        // Restart
        _vfx.Reinit();
        _vfx.Play();
    }

    // 1-liners for gameplay
    public void StartTrail()
    {
        if (_vfx == null) return;

        string boolName = _equippedDef != null ? _equippedDef.canDrawTrailBool : canDrawBoolName;
        SafeSetBool(_vfx, boolName, true);
        _vfx.Play();
    }

    public void StopTrail()
    {
        if (_vfx == null) return;

        string boolName = _equippedDef != null ? _equippedDef.canDrawTrailBool : canDrawBoolName;
        SafeSetBool(_vfx, boolName, false);
    }

    private void EnsureVfxInstance()
    {
        if (_dashVfxInstance != null && _vfx != null)
            return;

        if (dashVfxPrefab == null)
        {
            Debug.LogError("[DashTrailVfxEquipper] dashVfxPrefab not assigned.", this);
            return;
        }

        _dashVfxInstance = Instantiate(dashVfxPrefab);
        _dashVfxInstance.name = $"{dashVfxPrefab.name}__{name}";

        _vfx = _dashVfxInstance.GetComponentInChildren<VisualEffect>(true);
        if (_vfx == null)
        {
            Debug.LogError("[DashTrailVfxEquipper] DashVFX prefab has no VisualEffect component.", this);
            return;
        }
        // Bind VFXPropertyBinder's Transform target to TrailFxAnchor
        CachePlayerRefs();
        if (_trailFxAnchor == null)
        {
            Debug.LogError($"[DashTrailVfxEquipper] Could not find '{trailFxAnchorName}' under player '{name}'.", this);
        }
        else
        {
            bool ok = TryBindTransformBinderTarget(_dashVfxInstance, transformBinderTypeName, _trailFxAnchor);
            if (!ok)
                Debug.LogError("[DashTrailVfxEquipper] Could not bind TrailFxAnchor to VFXTransformBinder.Target. Check DashVFX prefab has the Transform Binder.", this);
        }
        // Don't bind here. Bind after Equip() assigns the correct visualEffectAsset.
    }


    private bool TryBindTransformBinderTarget(GameObject root, string binderType, Transform target)
    {
        Component binder = FindComponentByTypeName(root, binderType);
        if (binder == null) return false;

        var t = binder.GetType();

        // Make sure it's set the way your inspector expects
        SetMemberIfExists(binder, t, binderPropertyMemberName, binderPropertyValue);
        SetEnumMemberIfExists(binder, t, binderSpaceMemberName, "Local");

        // Set target = TrailFxAnchor
        return SetMemberIfExists(binder, t, binderTargetMemberName, target);
    }

    private Component FindComponentByTypeName(GameObject root, string typeName)
    {
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
        var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, value);
            return true;
        }

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

    private void RebindGraphInputs(DashTrailVfxDefinition def)
    {
        CachePlayerRefs();
        if (_vfx == null || _smr == null || def == null) return;

        // Bind SMR safely (never throw)
        SafeSetSkinnedMeshRenderer(_vfx, def.skinnedMeshRendererProperty, _smr);

        // NOTE: Transform binding is still handled by your VFXPropertyBinder setup (and/or the reflection binder you already have working).
    }

    private void SafeSetSkinnedMeshRenderer(VisualEffect vfx, string name, SkinnedMeshRenderer smr)
    {
        if (vfx == null || smr == null || string.IsNullOrWhiteSpace(name))
            return;

        // KEY: only call SetSkinnedMeshRenderer if the property exists on THIS asset.
        if (!VfxPropertyUtil.HasExposedProperty(vfx, name))
            return;

        vfx.SetSkinnedMeshRenderer(name, smr);
    }

    private void CachePlayerRefs()
    {
        if (_body == null)
            _body = FindDeepChild(transform, bodyName);

        if (_trailFxAnchor == null)
            _trailFxAnchor = FindDeepChild(transform, trailFxAnchorName);

        if (_smr == null && _body != null)
            _smr = FindPreferredSmr(_body, preferSmrNameContains);
    }

    private void BuildMapIfNeeded()
    {
        if (_map != null) return;

        _map = new System.Collections.Generic.Dictionary<DashTrailId, DashTrailVfxDefinition>();
        if (definitions == null) return;

        foreach (var def in definitions)
        {
            if (def == null) continue;
            _map[def.id] = def;
        }
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

    private void SafeSetBool(VisualEffect vfx, string name, bool value)
    {
        if (vfx == null) return;
        try { vfx.SetBool(name, value); }
        catch (System.ArgumentException) { }
    }

    private void SafeSetFloat(VisualEffect vfx, string name, float value)
    {
        if (vfx == null) return;
        try { vfx.SetFloat(name, value); }
        catch (System.ArgumentException) { }
    }

    private void OnDestroy()
    {
        if (_dashVfxInstance != null)
            Destroy(_dashVfxInstance);
    }
}

public static class VfxPropertyUtil
{
    // Cache per VisualEffectAsset to avoid reflection every equip
    private static readonly Dictionary<VisualEffectAsset, HashSet<string>> _cache = new();

    public static bool HasExposedProperty(VisualEffect vfx, string propertyName)
    {
        if (vfx == null || vfx.visualEffectAsset == null || string.IsNullOrWhiteSpace(propertyName))
            return false;

        var asset = vfx.visualEffectAsset;

        if (!_cache.TryGetValue(asset, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _cache[asset] = set;

            // VisualEffectAsset has an internal list of exposed properties.
            // We grab it via reflection in a version-tolerant way.
            try
            {
                // Try method: GetExposedProperties() (exists in some versions)
                var m = typeof(VisualEffectAsset).GetMethod("GetExposedProperties", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null)
                {
                    var arr = m.Invoke(asset, null);
                    if (arr is Array a)
                    {
                        foreach (var item in a)
                        {
                            if (item == null) continue;
                            var nameProp = item.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            var n = nameProp?.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(n))
                                set.Add(n);
                        }
                    }
                    // done
                }
                else
                {
                    // Try field: m_ExposedProperties or exposedProperties (varies)
                    var f = typeof(VisualEffectAsset).GetField("m_ExposedProperties", BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? typeof(VisualEffectAsset).GetField("exposedProperties", BindingFlags.Instance | BindingFlags.NonPublic);

                    var listObj = f?.GetValue(asset);
                    if (listObj is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item == null) continue;
                            var nameField = item.GetType().GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            var n = nameField?.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(n))
                                set.Add(n);
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, we can't safely check. Return false to avoid logs.
            }
        }

        return set.Contains(propertyName);
    }
}