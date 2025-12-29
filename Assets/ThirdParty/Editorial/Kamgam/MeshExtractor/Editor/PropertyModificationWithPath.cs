using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.MeshExtractor
{
    class PropertyModificationWithPath
    {
        public PropertyModification Modification;
        public string Path;
        public string ComponentPath;

        public PropertyModificationWithPath(Transform prefabRoot, PropertyModification modification)
        {
            Modification = modification;

            // Path
            if (modification.target != null)
            {
                Transform transform = null;

                var comp = modification.target as Component;
                if (comp != null)
                    transform = comp.transform;

                // Fallback, game object
                if (transform == null)
                {
                    var go = modification.target as GameObject;
                    if (go != null)
                        transform = go.transform;
                }

                Path = HierarchyPathUtils.GetRelativePath(prefabRoot, transform);
                // Component path will be null if it is a game object.
                ComponentPath = HierarchyPathUtils.GetComponentPath(modification.target as Component);
            }
        }

        public static PropertyModificationWithPath[] Convert(Transform prefabRoot, PropertyModification[] modifications)
        {
            var newMods = new PropertyModificationWithPath[modifications.Length];

            for (int i = 0; i < modifications.Length; i++)
            {
                newMods[i] = new PropertyModificationWithPath(prefabRoot, modifications[i]);
            }

            return newMods;
        }

        public static PropertyModificationWithPath[] GetModifications(Transform prefabInstanceRoot)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstanceRoot.gameObject))
                return null;

            var modifications = PrefabUtility.GetPropertyModifications(prefabInstanceRoot.gameObject);
            return Convert(prefabInstanceRoot, modifications);
        }

        // A dictionary to sort the modifications by Object(component or game object).
        static Dictionary<Object,List<PropertyModification>> s_tmpModifications = new Dictionary<Object, List<PropertyModification>>();

        public static void SetModifications(Transform prefabInstanceRoot, PropertyModificationWithPath[] modifications)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstanceRoot.gameObject))
                return;

            s_tmpModifications.Clear();

            // Sort modifications by Object
            foreach (var mod in modifications)
            {
                var transform = HierarchyPathUtils.ResolveRelativePath(prefabInstanceRoot, mod.Path);
                if (transform == null)
                    continue;

                // ResolveComponentPath returns null for game objects since those are not of type Component.
                var compOrGameObject = HierarchyPathUtils.ResolveComponentPath(transform, mod.ComponentPath) as Object;
                if (compOrGameObject == null)
                    compOrGameObject = transform.gameObject;

                // update the target (it's usually null because the original prefab was destroyed/replaced).
                mod.Modification.target = transform.gameObject;

                if (!s_tmpModifications.ContainsKey(compOrGameObject))
                {
                    s_tmpModifications.Add(compOrGameObject, new List<PropertyModification>());
                }

                var modsPerObject = s_tmpModifications[compOrGameObject];
                modsPerObject.Add(mod.Modification);
            }

            // Set modification on each object
            foreach (var kv in s_tmpModifications)
            {
                var compOrGameObject = kv.Key;
                var mods = kv.Value.ToArray();
                if (compOrGameObject != null && mods.Length > 0)
                {
                    PrefabUtility.SetPropertyModifications(compOrGameObject, mods);
                }
            }
            
            s_tmpModifications.Clear();
        }

        /// <summary>
        /// Only supports a subset of modified properties.
        /// </summary>
        /// <param name="prefabInstanceRoot"></param>
        /// <param name="modifications"></param>
        public static void SetPartialModificationsManually(Transform prefabInstanceRoot, PropertyModificationWithPath[] modifications)
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstanceRoot.gameObject))
                return;

            s_tmpModifications.Clear();

            // Sort modifications by Object
            foreach (var mod in modifications)
            {
                var transform = HierarchyPathUtils.ResolveRelativePath(prefabInstanceRoot, mod.Path);
                if (transform == null)
                    continue;

                // ResolveComponentPath returns null for game objects since those are not of type Component.
                var compOrGameObject = HierarchyPathUtils.ResolveComponentPath(transform, mod.ComponentPath) as Object;
                if (compOrGameObject == null)
                    compOrGameObject = transform.gameObject;

                // update the target (it's usually null because the original prefab was destroyed/replaced).
                mod.Modification.target = transform.gameObject;

                if (!s_tmpModifications.ContainsKey(compOrGameObject))
                {
                    s_tmpModifications.Add(compOrGameObject, new List<PropertyModification>());
                }

                var modsPerObject = s_tmpModifications[compOrGameObject];
                modsPerObject.Add(mod.Modification);
            }

            // Set modification on each object
            foreach (var kv in s_tmpModifications)
            {
                var compOrGameObject = kv.Key;
                var mods = kv.Value.ToArray();
                if (compOrGameObject != null && mods.Length > 0)
                {
                    var so = new SerializedObject(compOrGameObject);
                    foreach (var mod in mods)
                    {
                        var prop = so.FindProperty(mod.propertyPath);
                        
                        if (prop == null)
                            continue;

                        var p = compOrGameObject is GameObject ? (compOrGameObject as GameObject).transform.parent.name : (compOrGameObject as Component).gameObject.transform.parent.name;
                        // Debug.Log("MeshExtractor.PropertyModificationWithPath: Restoring " + mod.propertyPath + " on " + compOrGameObject.name + " parent: " + p);

                        switch (prop.propertyType)
                        {
                            //case SerializedPropertyType.Generic:
                            //    break;
                            case SerializedPropertyType.Integer:
                            case SerializedPropertyType.LayerMask:
                            case SerializedPropertyType.Enum:
                                prop.intValue = int.Parse(mod.value);
                                break;
                            case SerializedPropertyType.Boolean:
                                prop.boolValue = int.Parse(mod.value) != 0;
                                break;
                            case SerializedPropertyType.Float:
                                prop.floatValue = float.Parse(mod.value, System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case SerializedPropertyType.String:
                                prop.stringValue = mod.value;
                                break;
                            case SerializedPropertyType.ObjectReference:
                                prop.objectReferenceValue = mod.objectReference;
                                break;
                            // I do not know how these are serialized (nor how to easily deserialize them).
                            // Thus they are ignored.
                            //case SerializedPropertyType.Color:
                            //    break;
                            //case SerializedPropertyType.Vector2:
                            //    break;
                            case SerializedPropertyType.Vector3:
                                prop.vector3Value = prop.vector3Value; // < I don't recall why I am doing this, seems useless. Investigate?
                                break;
                            //case SerializedPropertyType.Vector4:
                            //    break;
                            //case SerializedPropertyType.Rect:
                            //    break;
                            //case SerializedPropertyType.ArraySize:
                            //    break;
                            //case SerializedPropertyType.Character:
                            //    break;
                            //case SerializedPropertyType.AnimationCurve:
                            //    break;
                            //case SerializedPropertyType.Bounds:
                            //    break;
                            //case SerializedPropertyType.Gradient:
                            //    break;
                            //case SerializedPropertyType.Quaternion:
                            //    prop.quaternionValue = prop.quaternionValue;
                            //    break;
                            //case SerializedPropertyType.ExposedReference:
                            //    break;
                            //case SerializedPropertyType.FixedBufferSize:
                            //    break;
                            //case SerializedPropertyType.Vector2Int:
                            //    break;
                            //case SerializedPropertyType.Vector3Int:
                            //    break;
                            //case SerializedPropertyType.RectInt:
                            //    break;
                            //case SerializedPropertyType.BoundsInt:
                            //    break;
                            //case SerializedPropertyType.ManagedReference:
                            //    break;
                            //case SerializedPropertyType.Hash128:
                            //    break;
                            default:
                                break;
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            s_tmpModifications.Clear();
        }
    }
}
