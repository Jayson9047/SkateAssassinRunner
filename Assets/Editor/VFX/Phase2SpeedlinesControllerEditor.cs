using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Phase2SpeedlinesController))]
public sealed class Phase2SpeedlinesControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        var controller = (Phase2SpeedlinesController)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !controller.isActiveAndEnabled))
        {
            if (GUILayout.Button("TEST SHOW")) controller.ShowImmediateForTest();
            if (GUILayout.Button("TEST HIDE")) controller.HideImmediate();
            EditorGUILayout.Space();
            if (GUILayout.Button("TEST ROTATION -35°")) { controller.SetTestRotation(-35f); controller.ShowImmediateForTest(); }
            if (GUILayout.Button("TEST ROTATION 0°")) { controller.SetTestRotation(0f); controller.ShowImmediateForTest(); }
            if (GUILayout.Button("TEST ROTATION +35°")) { controller.SetTestRotation(35f); controller.ShowImmediateForTest(); }
        }
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to preview the animation. Phase 2 gameplay is not required.", MessageType.None);
    }
}
