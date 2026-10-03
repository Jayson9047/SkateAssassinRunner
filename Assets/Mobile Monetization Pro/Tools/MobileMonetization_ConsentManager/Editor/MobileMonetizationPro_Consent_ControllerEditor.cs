using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MobileMonetizationPro
{
    [CustomEditor(typeof(MobileMonetizationPro_Consent_Controller))]
    public class MobileMonetizationPro_Consent_ControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (MobileMonetizationPro_Consent_Controller)target;

            if (script.targetScript != null)
            {
                var methods = script.targetScript.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                    .Select(m => m.Name)
                    .ToList();

                if (methods.Count == 0)
                {
                    EditorGUILayout.HelpBox("No public void methods found in this script.", MessageType.Info);
                }
                else
                {
                    int currentIndex = Mathf.Max(0, methods.IndexOf(script.selectedMethodName));
                    int newIndex = EditorGUILayout.Popup("Select Method", currentIndex, methods.ToArray());
                    script.selectedMethodName = methods[newIndex];
                }
            }
        }
    }
}