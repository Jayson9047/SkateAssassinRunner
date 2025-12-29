#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperPosition
{
    [CustomEditor(typeof(SuperPositionCore))]
    public class SuperPositionCoreInspector : Editor
    {
        SerializedProperty superPositionCore;

        void OnEnable()
        {
            superPositionCore = serializedObject.FindProperty("SuperPositionCore");

        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open SuperPosition Window"))
            {
                EditorApplication.ExecuteMenuItem("Window/ShrinkRay Entertainment/SuperPosition/Open SuperPosition Window");
            }
            EditorGUILayout.HelpBox("DO NOT DELETE THIS OBJECT DIRECTLY", MessageType.Warning);
            EditorGUILayout.HelpBox("This Object contains the database to all helper objects and other data required to save and restore states.  Deleting this will have unexpected effects on your scene!", MessageType.Info);
            EditorGUILayout.HelpBox("If you want to remove SuperPosition from your scene (good to do before going into production) it is recommended that you SAVE AS your scene, and give it a title that lets you know that version has your SuperPosition saves, then use the menu item Window > ShrinkRay Entertainment > SuperPosition > Remove SuperPosition from Scene!", MessageType.Info);
            EditorGUILayout.HelpBox("The advantage to this is it will also 100% clean your scene from any SuperPosition helper data.", MessageType.Info);
            EditorGUILayout.HelpBox("Think of this as 'merge all layers' in Photoshop -- once you do this, all your layers are GONE FOREVER.  This is why I really, really, REALLY think you should save a copy of your scene before doing this action.", MessageType.Info);
            EditorGUILayout.HelpBox("BY THE WAY!  Any helper objects created by SuperPosition are 1. Hidden and 2. Tagged as Editor Only, so they _shouldn't_ compile with any builds anyway.  So you might not need to worry about doing this at all.", MessageType.Info);
        }
    }

}
#endif