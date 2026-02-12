using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace MobileMonetizationPro
{
    public class FixScriptsForAndroidWindow : EditorWindow
    {
        private List<MonoScript> scriptsToFix = new List<MonoScript>();

        [MenuItem("Tools/Mobile Monetization Pro/Solutions/Fix Specific Scripts For Android")]
        public static void ShowWindow()
        {
            GetWindow<FixScriptsForAndroidWindow>("Fix Scripts For Android");
        }

        private void OnGUI()
        {
            GUILayout.Label("Drag and Drop MonoBehaviour Scripts Below", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw Drag and Drop area
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Scripts Here", EditorStyles.helpBox);

            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space();

            // Show the list of scripts
            if (scriptsToFix.Count > 0)
            {
                GUILayout.Label("Scripts to fix:", EditorStyles.boldLabel);
                foreach (var script in scriptsToFix)
                {
                    if (script != null)
                    {
                        GUILayout.Label($"- {script.name}");
                    }
                }
            }

            EditorGUILayout.Space();

            GUI.enabled = scriptsToFix.Count > 0;
            if (GUILayout.Button("Convert Them Specific To Android"))
            {
                ConvertScripts();
            }
            GUI.enabled = true;
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is MonoScript script)
                            {
                                if (!scriptsToFix.Contains(script))
                                {
                                    scriptsToFix.Add(script);
                                }
                            }
                        }

                    }
                    Event.current.Use();
                    break;
            }
        }

        private void ConvertScripts()
        {
            foreach (var script in scriptsToFix)
            {
                if (script == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(script);

                if (!path.EndsWith(".cs"))
                {
                    Debug.LogWarning($"Skipped {path}: Not a C# script.");
                    continue;
                }

                string originalContent = File.ReadAllText(path);

                // Avoid adding if already wrapped
                if (originalContent.StartsWith("#if UNITY_ANDROID"))
                {
                    Debug.Log($"Skipped {path}: Already wrapped with #if UNITY_ANDROID.");
                    continue;
                }

                string newContent = "#if UNITY_ANDROID\n" + originalContent + "\n#endif";

                File.WriteAllText(path, newContent);

                Debug.Log($"Modified {path} for Android only.");
            }

            AssetDatabase.Refresh();
            Debug.Log("Finished modifying selected scripts!");
        }
    }
}