#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperPosition
{
    [CustomEditor(typeof(SuperPositionAlert))]
    public class SuperPositionAlertInspector : Editor
    {
        SerializedProperty SuperPositionAlert;

        void OnEnable()
        {
            SuperPositionAlert = serializedObject.FindProperty("SuperPositionAlert");
        }

        public override void OnInspectorGUI()
        {
            //GUILayout.BeginArea(new Rect(0, 0, EditorGUIUtility.currentViewWidth, 200f));
            //{
            EditorGUILayout.HelpBox("This Object was restored by SuperPosition and will exhibit most attributes previously saved, however since this is NOT the exact same object as the one that was deleted by yourself, if you had any references to this object you must relink all such connections into any relevant GameObjects that might rely upon this object.  Press the 'Understood' button below to confirm and this message will be removed!", MessageType.Warning);
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Understood!  Remove this Alert, Please!"))
                {
                    Component component = Selection.activeGameObject.GetComponent<SuperPositionAlert>();
                    if (component != null)
                    {
                        Object.DestroyImmediate(component as Object, true);

                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        //GUILayout.EndArea();
        //}
        //
    }
}
#endif