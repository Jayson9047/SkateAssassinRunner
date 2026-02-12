using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace MobileMonetizationPro
{
    [CustomEditor(typeof(InternetConnectivityCheck))]
    public class InternetConnectivityCheckEditor : Editor
    {
        private SerializedProperty scriptWithFunctionProp;
        private SerializedProperty methodNameProp;

        private string[] methodOptions;
        private int selectedIndex;

        void OnEnable()
        {
            scriptWithFunctionProp = serializedObject.FindProperty("scriptWithFunction");
            methodNameProp = serializedObject.FindProperty("methodName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "scriptWithFunction", "methodName");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Call Function When Connected To Internet", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(scriptWithFunctionProp);
            if (EditorGUI.EndChangeCheck())
            {
                methodNameProp.stringValue = "";
            }

            if (scriptWithFunctionProp.objectReferenceValue != null)
            {
                MonoBehaviour mono = scriptWithFunctionProp.objectReferenceValue as MonoBehaviour;
                var methods = GetValidMethods(mono);
                methodOptions = methods.ToArray();

                selectedIndex = Mathf.Max(0, System.Array.IndexOf(methodOptions, methodNameProp.stringValue));
                selectedIndex = EditorGUILayout.Popup("Method To Call", selectedIndex, methodOptions);

                if (methodOptions.Length > 0)
                    methodNameProp.stringValue = methodOptions[selectedIndex];
            }

            serializedObject.ApplyModifiedProperties();
        }

        private List<string> GetValidMethods(MonoBehaviour mono)
        {
            List<string> valid = new List<string>();
            MethodInfo[] methods = mono.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                {
                    valid.Add(method.Name);
                }
            }
            return valid;
        }
    }
}
