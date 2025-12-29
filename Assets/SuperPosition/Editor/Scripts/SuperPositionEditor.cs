#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace SuperPosition
{
    [InitializeOnLoad]
    
    public class SuperPositionEditor : Editor
    {
        static public SuperPositionCore data;
        static public GameObject SP;
        static SuperPositionEditor()
        {

            EditorApplication.update += EditorUpdate;
            Selection.selectionChanged += OnSelectionChange;

            System.Reflection.FieldInfo info = typeof(EditorApplication).GetField("globalEventHandler", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            EditorApplication.CallbackFunction value = (EditorApplication.CallbackFunction)info.GetValue(null);


            info.SetValue(null, value);

#if UNITY_2018
            SceneView.onSceneGUIDelegate -= OnScene;
            SceneView.onSceneGUIDelegate += OnScene;
#endif

#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnScene;
            SceneView.duringSceneGui += OnScene;
#endif
        }



        [MenuItem("Window/ShrinkRay Entertainment/SuperPosition/Add SuperPosition to Scene")]
        static public void SetupSP()
        {
            AssetDatabase.Refresh();
            //Debug.Log("SetupSP");
            SP = GameObject.Find("SuperPosition");
            if (SP == null)
            {
                foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
                {
                    if (go.name.Contains("SuperPosition"))
                        DestroyImmediate(go);
                }

                SP = new GameObject();
                SP.AddComponent<SuperPositionCore>();

                //var r = Resources.Load("SuperPosition", typeof(GameObject));
                //AssetDatabase.ReleaseCachedFileHandles();

                //UnityEngine.Object r = Resources.Load("SuperPosition");
                //Debug.Log(r);

                //SP = (GameObject)PrefabUtility.InstantiatePrefab(r);
                ////Destroy(prefabClone, 0.25f);  // destroyed after 0.25 sec

                //SP = GameObject.Instantiate((GameObject) r) as GameObject;
                //Debug.Log(SP);
                //SP = GameObject.Instantiate(Resources.Load("SuperPosition")) as GameObject;
                SP.name = "SuperPosition";
                data = SP.GetComponent<SuperPositionCore>();
                //Debug.Log(data);
            }
        }


        [MenuItem("Window/ShrinkRay Entertainment/SuperPosition/Remove SuperPosition from Scene")]
        static private void RemoveSP()
        {
            Debug.Log("removeSP");
            if (EditorUtility.DisplayDialog("Remove SuperPosition", "This will remove 100% of all SuperPosition saved states and all helper objects from your Scene, it is highly recommended you Save As your scene before doing this to keep a backup of your states.  Proceed?", "Yes, I understand!", "No!  Abort!!!"))
            {
                //SP = GameObject.Find("SuperPosition");
                SuperPositionEditorWindow.CloseWindow();

                foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
                {
                    //Debug.Log(go.name);
                    if (go.name.Contains("SuperPosition"))
                        DestroyImmediate(go, true);
                }
                foreach (SuperPositionID go in Resources.FindObjectsOfTypeAll(typeof(SuperPositionID)))
                {
                    //Debug.Log(go.name);
                    DestroyImmediate(go.GetComponent<SuperPositionID>());
                }

                Resources.UnloadAsset(SP);

                data = null; SP = null;
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
        }

        static public GameObject selectedObject, prevObject;
        static public int selectedGUID = 0;
        static public Component[] components;
        //
        static void EditorUpdate()
        {
            if (SP == null)
            {
                //Debug.Log("Return");
                return;
            }//SetupSP();
            if (data == null) data = SP.GetComponent<SuperPositionCore>();

            selectedObject = Selection.activeGameObject;

            if (selectedObject != null)
            {
                selectedGUID = selectedObject.GetInstanceID();
                if (selectedObject != prevObject)
                {
                    //if (EditorWindow.HasOpenInstances<SuperPositionEditorWindow>())
                    //    SuperPositionEditorWindow.QueueRepaint();
                    prevObject = selectedObject;
                }
            }
            else
            {
                prevObject = null;
            }
        }

        static void EditorInit()
        {

        }

        static private void OnSelectionChange()
        {
            if (EditorWindow.HasOpenInstances<SuperPositionEditorWindow>())
                SuperPositionEditorWindow.QueueRepaint();
        }

        private static void OnScene(SceneView sceneview)
        {

        }
    }
}
#endif