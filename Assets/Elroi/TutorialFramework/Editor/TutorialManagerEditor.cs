using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Elroi.Tutorials.Editor
{
    [CustomEditor(typeof(TutorialManager))]
    public sealed class TutorialManagerEditor : UnityEditor.Editor
    {
        private ReorderableList tutorialList;
        private ReorderableList sequencerList;
        private SerializedProperty tutorials;
        private SerializedProperty sequencers;

        private void OnEnable()
        {
            tutorials = serializedObject.FindProperty("tutorials");
            sequencers = serializedObject.FindProperty("tutorialSequencers");
            tutorialList = BuildList(tutorials, "TUTORIALS — WHAT", DrawTutorial, TutorialHeight);
            sequencerList = BuildList(sequencers, "TUTORIAL SEQUENCERS — ORDER / AUTOMATION", DrawSequencer, SequencerHeight);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("ELROI TUTORIAL FRAMEWORK v1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tutorial Definition = WHAT  •  Tutorial Sequencer = ORDER/AUTOMATION  •  Sequence Entry = WHEN", MessageType.Info);

            DrawSection("LIFECYCLE", "lifecyclePolicy", "whenAutomaticWorkCompletes");
            DrawSection("PROVIDERS / INTEGRATION", "presentationPrefab", "theme", "gameplayCamera", "gameplayAdapterComponent", "variableProviderComponent", "persistenceProviderComponent", "gestureSettings");
            EditorGUILayout.Space(8f);
            tutorialList.DoLayoutList();
            EditorGUILayout.Space(8f);
            sequencerList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
            DrawValidation();
        }

        private void DrawSection(string title, params string[] fields)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (string field in fields) EditorGUILayout.PropertyField(serializedObject.FindProperty(field), true);
        }

        private static ReorderableList BuildList(SerializedProperty property, string title,
            ReorderableList.ElementCallbackDelegate draw, ReorderableList.ElementHeightCallbackDelegate height)
        {
            ReorderableList list = new ReorderableList(property.serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
            list.drawElementCallback = draw;
            list.elementHeightCallback = height;
            return list;
        }

        private void DrawTutorial(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty item = tutorials.GetArrayElementAtIndex(index);
            rect.y += 2f;
            Rect line = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            string name = item.FindPropertyRelative("tutorialName").stringValue;
            item.isExpanded = EditorGUI.Foldout(line, item.isExpanded, $"Tutorial {index} — {(string.IsNullOrWhiteSpace(name) ? "Unnamed" : name)}", true);
            if (!item.isExpanded) return;
            line.y += EditorGUIUtility.singleLineHeight + 4f;

            DrawLabel(ref line, "GENERAL");
            Draw(ref line, item, "enabled"); Draw(ref line, item, "tutorialName");
            EditorGUI.BeginDisabledGroup(true); Draw(ref line, item, "stableId"); EditorGUI.EndDisabledGroup();
            Draw(ref line, item, "manualRunPolicy");
            DrawLabel(ref line, "TARGET");
            Draw(ref line, item, "targetType");
            TutorialTargetType type = (TutorialTargetType)item.FindPropertyRelative("targetType").enumValueIndex;
            if (type != TutorialTargetType.None)
            {
                Draw(ref line, item, "targetSource");
                TutorialTargetSource source = (TutorialTargetSource)item.FindPropertyRelative("targetSource").enumValueIndex;
                if (source == TutorialTargetSource.DirectObject) Draw(ref line, item, "spotlightTarget");
                if (source == TutorialTargetSource.RuntimeTargetId) Draw(ref line, item, "runtimeTargetId");
                Draw(ref line, item, "spotlightShape"); Draw(ref line, item, "spotlightPadding");
                if (type == TutorialTargetType.ThreeDimensional) { Draw(ref line, item, "fallbackTargetWidth"); Draw(ref line, item, "fallbackTargetHeight"); }
            }
            DrawLabel(ref line, "PRESENTATION");
            Draw(ref line, item, "introductionText", true); Draw(ref line, item, "bottomInstructionText", true); Draw(ref line, item, "gestureAnimation"); Draw(ref line, item, "freezeWorld");
            DrawLabel(ref line, "COMPLETION / INPUT");
            Draw(ref line, item, "completionType");
            TutorialCompletionType completion = (TutorialCompletionType)item.FindPropertyRelative("completionType").enumValueIndex;
            if (completion == TutorialCompletionType.Gesture) { Draw(ref line, item, "requiredGesture"); Draw(ref line, item, "gameplayActionId"); }
            if (completion == TutorialCompletionType.Event) Draw(ref line, item, "completionEventId");
            Draw(ref line, item, "markComplete");
        }

        private float TutorialHeight(int index)
        {
            SerializedProperty item = tutorials.GetArrayElementAtIndex(index);
            if (!item.isExpanded) return EditorGUIUtility.singleLineHeight + 6f;
            TutorialTargetType type = (TutorialTargetType)item.FindPropertyRelative("targetType").enumValueIndex;
            TutorialTargetSource source = (TutorialTargetSource)item.FindPropertyRelative("targetSource").enumValueIndex;
            TutorialCompletionType completion = (TutorialCompletionType)item.FindPropertyRelative("completionType").enumValueIndex;
            int lines = 18;
            if (type != TutorialTargetType.None) lines += 4 + (source == TutorialTargetSource.RuntimeContextTarget ? 0 : 1) + (type == TutorialTargetType.ThreeDimensional ? 2 : 0);
            if (completion == TutorialCompletionType.Gesture) lines += 2;
            if (completion == TutorialCompletionType.Event) lines += 1;
            return lines * (EditorGUIUtility.singleLineHeight + 3f) + 12f;
        }

        private void DrawSequencer(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty item = sequencers.GetArrayElementAtIndex(index);
            rect.y += 2f;
            Rect line = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            string name = item.FindPropertyRelative("sequenceName").stringValue;
            item.isExpanded = EditorGUI.Foldout(line, item.isExpanded, $"Sequencer {index} — {(string.IsNullOrWhiteSpace(name) ? "Unnamed" : name)}", true);
            if (!item.isExpanded) return;
            line.y += EditorGUIUtility.singleLineHeight + 4f;
            DrawLabel(ref line, "GENERAL");
            Draw(ref line, item, "enabled"); Draw(ref line, item, "sequenceName");
            EditorGUI.BeginDisabledGroup(true); Draw(ref line, item, "stableId"); EditorGUI.EndDisabledGroup();
            Draw(ref line, item, "runPolicy"); Draw(ref line, item, "lockGameplayBetweenTutorials");
            DrawLabel(ref line, "START CONDITIONS"); Draw(ref line, item, "startTrigger", true);
            DrawLabel(ref line, "SEQUENCE ENTRIES — WHEN"); Draw(ref line, item, "entries", true);
        }

        private float SequencerHeight(int index)
        {
            SerializedProperty item = sequencers.GetArrayElementAtIndex(index);
            if (!item.isExpanded) return EditorGUIUtility.singleLineHeight + 6f;
            return 10f * (EditorGUIUtility.singleLineHeight + 3f) +
                   EditorGUI.GetPropertyHeight(item.FindPropertyRelative("startTrigger"), true) +
                   EditorGUI.GetPropertyHeight(item.FindPropertyRelative("entries"), true) + 18f;
        }

        private static void DrawLabel(ref Rect line, string label)
        {
            EditorGUI.LabelField(line, label, EditorStyles.boldLabel);
            line.y += EditorGUIUtility.singleLineHeight + 3f;
        }

        private static void Draw(ref Rect line, SerializedProperty parent, string relative, bool includeChildren = false)
        {
            SerializedProperty property = parent.FindPropertyRelative(relative);
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            line.height = height;
            EditorGUI.PropertyField(line, property, includeChildren);
            line.y += height + 3f;
            line.height = EditorGUIUtility.singleLineHeight;
        }

        private void DrawValidation()
        {
            TutorialManager manager = (TutorialManager)target;
            foreach (string issue in manager.GetValidationIssues()) EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }
    }
}
