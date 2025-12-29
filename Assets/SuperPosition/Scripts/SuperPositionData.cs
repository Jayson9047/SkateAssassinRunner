#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;


namespace SuperPosition
{
    [ExecuteInEditMode]
    public class SuperPositionData : MonoBehaviour
    {
#if UNITY_EDITOR
        static public List<GameObject> restoredGameObjects = new List<GameObject>();

        static public List<long> IDLedger = new List<long>();

        static public int totalObjects, currentObject;

        static long CalculateNewID()
        {
            long ID = DateTime.Now.Ticks;
            if (IDLedger.Contains(ID)) ID = IDLedger.Max() + 1;
            IDLedger.Add(ID);
            return ID;
        }

        static SuperPositionID CreateIDObj(GameObject go, long ID = 0)
        {
            SuperPositionID spID = go.GetComponent<SuperPositionID>();
            if (spID == null)
            {
                go.AddComponent<SuperPositionID>();
                spID = go.GetComponent<SuperPositionID>();
            }
            spID.instanceID = go.GetInstanceID();

            if (ID == 0)
                spID.ID = CalculateNewID();
            else
                spID.ID = ID;

            return spID;
        }

        private static void GetAllChildren(Transform parent, ref List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.hideFlags == HideFlags.DontSaveInEditor || child.gameObject.hideFlags == HideFlags.DontSave || child.gameObject.hideFlags == HideFlags.HideAndDontSave) continue;
                list.Add(child.gameObject);
                //Debug.Log("Child found: " + child.gameObject.name + " With Flags: " + child.gameObject.hideFlags.ToString());
                //child.gameObject.hideFlags = HideFlags.None;
                GetAllChildren(child, ref list);
            }
        }


        [Serializable]
        public class GOSnapshot
        {
            public Component[] components;
            public string name, oldName;
            public long ID, timestamp;
            public GameObject originalObject, copyObject;
            [SerializeReference]
            public List<GOSnapshot> children = new List<GOSnapshot>();
            public int totalChildCount = 0;
            public bool active, single = false;
            public StaticEditorFlags staticEditorFlags;
            public string tag;
            public LayerMask layerMask;

            public byte[] terrainData;
            public int terrainDataLength;

            public GOSnapshot(GameObject go, GameObject parent = null, int childIndex = -1, bool isSingle = false)
            {
                string text;
                if (parent == null)
                {
                    text = go.name;
                }
                else
                {
                    text = parent.name + " >> " + go.name;
                }
                EditorUtility.DisplayProgressBar("SuperPosition Capturing Snapshot(s)", go.name, 1.0f);

                //if (parent != null)
                //{
                //    Debug.Log("New Snapshot for " + go.name + " with Parent of " + parent.name + "  Child Index: " + childIndex);
                //}
                //else
                //{
                //    Debug.Log("New Snapshot for " + go.name + " with Parent of NULL  Child Index: " + childIndex);
                //}
                timestamp = DateTime.Now.Ticks;
                SuperPositionID originalID = go.GetComponent<SuperPositionID>();
                SuperPositionID copyID = null;

                if (copyObject != null)
                    copyID = copyObject.GetComponent<SuperPositionID>();

                if (originalID == null)
                {
                    ID = GetID(go);
                }
                if (originalID != null || ID == 0)
                {
                    ID = timestamp;
                }

                if (IDLedger.Contains(ID)) ID = IDLedger.Max() + 1;
                IDLedger.Add(ID);
                originalObject = go;
                name = oldName = go.name + " | " + System.DateTime.Now.ToLongTimeString();
                if (isSingle)
                {
                    totalChildCount = 0;
                }
                else
                {
                    List<GameObject> x = new List<GameObject>();
                    GetAllChildren(go.transform, ref x);
                    totalChildCount = x.Count;
                }
                active = go.activeInHierarchy;
                staticEditorFlags = GameObjectUtility.GetStaticEditorFlags(go);
                tag = go.tag;
                layerMask = go.layer;
                single = isSingle;




                if (childIndex < 0)
                {
                    //if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    //{
                    //    Debug.Log("prefab");
                    //    copyObject = (GameObject)PrefabUtility.InstantiatePrefab(go);
                    //    Debug.Log(copyObject);
                    //}
                    //else
                    //{
                    copyObject = Instantiate(go);
                    //}

                    copyObject.name = copyObject.name.Replace("(Clone)", "") + " (SuperPosition)";
                }
                else
                {
                    copyObject = parent.transform.GetChild(childIndex).gameObject;
                    copyObject.name = copyObject.name.Replace("(Clone)", "");
                    copyObject.name = copyObject.name.Replace("(SuperPosition)", "") + " (SuperPosition)";
                }
                CopyComponents(copyObject, go);
                if (originalID == null) originalID = CreateIDObj(go, ID);
                if (copyID == null) copyID = CreateIDObj(copyObject, originalID.ID);
                copyObject.SetActive(false);
                copyObject.hideFlags = HideFlags.HideInHierarchy;
                copyObject.tag = "EditorOnly";
                //

                components = copyObject.GetComponents(typeof(Component));

                if (go.GetComponent<Terrain>() != null)
                {
                    string terrainDataPath = AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(go.GetComponent<Terrain>().terrainData.GetInstanceID()));
                    terrainData = File.ReadAllBytes(terrainDataPath);
                    terrainDataLength = terrainData.Length;
                }

                if (!single)
                    for (int i = 0; i < go.transform.childCount; i++)
                    {
                        if (go.transform.GetChild(i).gameObject.hideFlags == HideFlags.DontSaveInEditor || go.transform.GetChild(i).gameObject.hideFlags == HideFlags.DontSave || go.transform.GetChild(i).gameObject.hideFlags == HideFlags.HideAndDontSave) continue;
                        children.Add(new GOSnapshot(go.transform.GetChild(i).gameObject, copyObject, i));
                    }


            }

            private void counter(GameObject currentObj, ref int count)
            {
                for (int i = 0; i < currentObj.transform.childCount; i++)
                {
                    count++;
                    counter(currentObj.transform.GetChild(i).gameObject, ref count);
                }
            }


            public void RebuildAll(GameObject go)
            {
                if (single) return;
                Dictionary<GOSnapshot, GameObject> restoreGOs = new Dictionary<GOSnapshot, GameObject>();

                List<GameObject> goChildren = new List<GameObject>();
                GetAllChildren(copyObject.transform, ref goChildren);

                List<long> goIDs = new List<long>();

                for (int i = 0; i < goChildren.Count; i++)
                {
                    long id = GetID(goChildren[i].gameObject);
                    if (id > 0)
                        goIDs.Add(id);
                }

                for (int i = 0; i < go.transform.childCount; i++)
                {
                    int index = goIDs.IndexOf(GetID(go.transform.GetChild(i).gameObject));
                    if (index > -1)
                        goIDs.RemoveAt(index);
                }

                for (int i = 0; i < goIDs.Count; i++)
                {
                    for (int ii = 0; ii < children.Count; ii++)
                    {
                        long thisID, childID;
                        thisID = goIDs[i]; childID = GetID(children[ii].copyObject);

                        if (thisID == childID)
                            if (!restoreGOs.ContainsKey(children[ii]))
                                restoreGOs.Add(children[ii], go);
                    }
                }

                foreach (KeyValuePair<GOSnapshot, GameObject> p in restoreGOs)
                {
                    GameObject restoredGO = GameObject.Instantiate(p.Key.copyObject);

                    restoredGO.transform.SetParent(p.Value.transform);
                    restoredGO.name = restoredGO.name.Replace("(Clone)", "").Trim();
                    restoredGO.name = restoredGO.name.Replace("(SuperPosition)", "").Trim();
                    restoredGO.name = restoredGO.name + " (Restored)";
                    restoredGO.GetComponent<SuperPositionID>().instanceID = restoredGO.GetComponent<SuperPositionID>().GetInstanceID();
                    p.Key.originalObject = restoredGO;
                    restoredGameObjects.Add(restoredGO);
                }
            }


            static long GetID(GameObject go)
            {
                if (go.GetComponent<SuperPositionID>() != null)
                {
                    return go.GetComponent<SuperPositionID>().ID;
                }

                return 0;
            }

            public string RestoreAll(GameObject go, GO goSettings, bool isChild = false)
            {
                string specialAssets = "";
                specialAssets += Restore(go, goSettings);
                if (single)
                    return specialAssets;

                EditorUtility.DisplayProgressBar("SuperPosition Restoring", go.name, 1.0f);
                bool found;

                Dictionary<GOSnapshot, GameObject> restoreGOs = new Dictionary<GOSnapshot, GameObject>();

                for (int i = 0; i < go.transform.childCount; i++)
                {
                    GameObject thisGO = go.transform.GetChild(i).gameObject;
                    found = false;
                    for (int ii = 0; ii < children.Count; ii++)
                    {
                        long thisID = GetID(thisGO), childID = GetID(children[ii].copyObject);
                        if (thisID + childID > 0 && thisID == childID)
                        {
                            specialAssets += children[ii].RestoreAll(thisGO, goSettings, true);
                            found = children[ii].active;
                        }
                    }
                    if (thisGO.hideFlags == HideFlags.DontSaveInEditor || thisGO.hideFlags == HideFlags.DontSave || thisGO.hideFlags == HideFlags.HideAndDontSave) found = thisGO.activeInHierarchy;
                    thisGO.SetActive(found);
                }
                if (!isChild) EditorUtility.ClearProgressBar();

                return (specialAssets);
            }

            public string Restore(GameObject go, GO goSettings)
            {
                Component[] GOcomponents = go.GetComponents(typeof(Component));
                string specialAssets = "";

                //List<Component> retryComponents = new List<Component>();

                go.SetActive(active);
                GameObjectUtility.SetStaticEditorFlags(go, staticEditorFlags);
                go.layer = layerMask;
                go.tag = tag;

                //for (int i = 1; i < GOcomponents.Length; i++)
                //{
                //    if (CanDestroy(go, GOcomponents[i].GetType()))
                //    {
                //        DestroyImmediate(GOcomponents[i]);
                //    }
                //    else
                //    {
                //        retryComponents.Add(GOcomponents[i]);
                //    }
                //}

                //for (int i = 0; i < retryComponents.Count; i++)
                //{
                //    DestroyImmediate(retryComponents[i]);
                //}

                components = copyObject.GetComponents(typeof(Component));
                List<Component> gc = GOcomponents.ToList();

                for (int i = 0; i < components.Length; i++)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(components[i]);
                    if (gc.Count == 0)
                    {
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
                    }
                    for (int ii = 0; ii < gc.Count; ii++)
                    {
                        if (components[i].GetType() == gc[ii].GetType())
                        {
                            //Debug.Log(gc[ii].GetType().ToString());
                            bool forceOnEnable = false, forceAwake = false, forceStart = false;
                            if (gc[ii].GetType().ToString().Contains("PlayMaker")) specialAssets += "PLAYMAKER//";
                            if (gc[ii].GetType().ToString() == "UnityEngine.Terrain") specialAssets += "TERRAIN//";
                            if (gc[ii].GetType().ToString().Contains("AwesomeTechnologies.Veg")) { specialAssets += "VEGETATIONSTUDIO//"; forceOnEnable = true; }
                            if (gc[ii].GetType().ToString() == "UnityEngine.Terrain")
                            {
                                string terrainDataPath = AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(go.GetComponent<Terrain>().terrainData.GetInstanceID()));

                                if (terrainData == null)
                                {

                                    Debug.Log("NULL TERRAIN DATA");
                                }
                                else
                                {
                                    File.WriteAllBytes(terrainDataPath, terrainData);
                                }
                            }

                            if (gc[ii].GetType().ToString() != "SuperPosition.SuperPositionID")
                            {
                                UnityEditorInternal.ComponentUtility.PasteComponentValues(gc[ii]);
                                MethodInfo dynMethod = gc[ii].GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                                if (dynMethod != null && (goSettings.forceAwake || forceAwake)) { dynMethod.Invoke(gc[ii], new object[] { }); }
                                dynMethod = gc[ii].GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                                if (dynMethod != null && (goSettings.forceOnEnable || forceOnEnable)) { dynMethod.Invoke(gc[ii], new object[] { }); }
                                dynMethod = gc[ii].GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                                if (dynMethod != null && (goSettings.forceStart || forceStart)) { dynMethod.Invoke(gc[ii], new object[] { }); }
                            }

                            gc.RemoveAt(ii);
                            break;
                        }
                    }
                }
                for (int i = 0; i < gc.Count; i++)
                {
                    DestroyImmediate(gc[i]);
                }




                return (specialAssets);
            }

            public void CopyComponents(GameObject copy, GameObject original)
            {
                Component[] copyComponents = copy.GetComponents(typeof(Component));
                Component[] originalComponents = original.GetComponents(typeof(Component));
                List<Component> retryComponents = new List<Component>();

                copy.SetActive(active);
                GameObjectUtility.SetStaticEditorFlags(copy, staticEditorFlags);
                copy.layer = layerMask;
                copy.tag = tag;

                for (int i = 1; i < copyComponents.Length; i++)
                {
                    if (CanDestroy(copy, copyComponents[i].GetType()))
                    {
                        DestroyImmediate(copyComponents[i]);

                    }
                    else
                    {
                        retryComponents.Add(copyComponents[i]);
                    }
                }

                for (int i = 0; i < retryComponents.Count; i++)
                {
                    DestroyImmediate(retryComponents[i]);
                }

                for (int i = 0; i < originalComponents.Length; i++)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(originalComponents[i]);
                    if (i == 0)
                    {
                        UnityEditorInternal.ComponentUtility.PasteComponentValues(copyComponents[0]);
                    }
                    else
                    {
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(copy);
                    }
                }
            }

            public void Delete(GameObject go)
            {
                DestroyImmediate(copyObject);
            }

        }

        [Serializable]
        public class GO
        {
            public string name;
            public GameObject go;
            public List<GOSnapshot> goSnapshot;
            public int selected = 0, selectedLocked = 0;
            public bool single = false, forceCompile = false, isOptions = false, forceAwake = false, forceOnEnable = false, forceStart = false;

            public GO(GameObject g, bool s)
            {
                go = g;
                name = go.name;
                single = s;
                goSnapshot = new List<GOSnapshot>();
                AssetDatabase.SaveAssets();

                NewState(s);


            }

            public void NewState(bool s)
            {
                goSnapshot.Add(new GOSnapshot(go, null, -1, s));
                EditorUtility.ClearProgressBar();
            }

            public void SetSelected(int s)
            {
                selected = s;
            }
        }

        [Serializable]
        public class State
        {
            [SerializeField]
            public List<GO> GOs = new List<GO>();

            public static int CountObjects(Transform transform)
            {
                int childCount = transform.childCount;
                foreach (Transform child in transform)
                {
                    childCount += CountObjects(child);
                }
                return childCount;
            }

            public void NewState(GameObject go, bool single)
            {
                bool found = false;
                int i;

                for (i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go)
                    {
                        //Debug.Log(GOs[i].go.name + " // " + go.name);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    //Debug.Log(go.name + " not found");
                    GOs.Add(new GO(go, single));
                    i = GOs.Count - 1;
                }
                else
                {
                    //Debug.Log("Making New State");
                    GOs[i].NewState(single);
                }
            }

            public List<GOSnapshot> GetGOSnapshots(GameObject go)
            {
                for (int i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go) return GOs[i].goSnapshot;
                }
                return null;
            }

            public GO GetGO(GameObject go)
            {
                for (int i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go) return GOs[i];
                }
                return null;
            }

            public void DeleteGO(GameObject go)
            {
                for (int i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go) GOs.RemoveAt(i);
                }
            }

            public void SetSelected(GameObject go, int s)
            {
                bool found = false;
                int i;
                for (i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    GOs[i].selected = s;
                }
            }

            public bool IsSelected(GameObject go, int s)
            {
                bool found = false;
                int i;
                for (i = 0; i < GOs.Count; i++)
                {
                    if (GOs[i].go == go)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    return GOs[i].selected == s;
                }
                return false;
            }
        }

        private static bool Requires(Type obj, Type requirement)
        {
            return Attribute.IsDefined(obj, typeof(RequireComponent)) &&
                   Attribute.GetCustomAttributes(obj, typeof(RequireComponent)).OfType<RequireComponent>()
                   .Any(rc => rc.m_Type0.IsAssignableFrom(requirement));
        }

        private static bool CanDestroy(GameObject go, Type t)
        {
            try
            {
                return !go.GetComponents<Component>().Any(c => Requires(c.GetType(), t));
            }
            catch
            {
                Debug.LogWarning($"GameObject {go.name} failed CanDestroy() check, please contact support at shrinkrayentertainment@gmail.com");
                return false;
            }
        }

    }

#endif
}
#endif