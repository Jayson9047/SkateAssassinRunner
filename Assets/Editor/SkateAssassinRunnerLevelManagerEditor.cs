using MoreMountains.InfiniteRunnerEngine;
using UnityEditor;
using UnityEngine;

// Editor-only presentation: keep the inherited runtime fields and serialized
// fallback data intact, but author campaign speeds through Level Speed Windows.
[CustomEditor(typeof(SkateAssassinRunnerLevelManager))]
[CanEditMultipleObjects]
public sealed class SkateAssassinRunnerLevelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        DrawPropertiesExcluding(serializedObject, "m_Script",
            nameof(LevelManager.InitialSpeed), nameof(LevelManager.MaximumSpeed));
        serializedObject.ApplyModifiedProperties();

        // Show only calculated values during a run, never editable speed inputs.
        if (Application.isPlaying && targets.Length == 1)
        {
            var manager = (SkateAssassinRunnerLevelManager)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Speed (Calculated)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Initial Speed", manager.InitialSpeed);
                EditorGUILayout.FloatField("Maximum Speed", manager.MaximumSpeed);
            }
        }
    }
}
