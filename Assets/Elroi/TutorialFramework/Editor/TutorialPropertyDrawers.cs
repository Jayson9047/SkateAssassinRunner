using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elroi.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(TutorialTriggerDefinition))]
    public sealed class TutorialTriggerDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }
            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight + 2f;
            Draw(ref line, property.FindPropertyRelative("triggerType"));
            TutorialTriggerType type = (TutorialTriggerType)property.FindPropertyRelative("triggerType").enumValueIndex;
            switch (type)
            {
                case TutorialTriggerType.Time:
                    Draw(ref line, property.FindPropertyRelative("delaySeconds"));
                    Draw(ref line, property.FindPropertyRelative("timeOrigin"));
                    Draw(ref line, property.FindPropertyRelative("useUnscaledTime"));
                    break;
                case TutorialTriggerType.Distance:
                    Draw(ref line, property.FindPropertyRelative("referenceObject"));
                    Draw(ref line, property.FindPropertyRelative("referenceTargetId"));
                    Draw(ref line, property.FindPropertyRelative("targetObject"));
                    Draw(ref line, property.FindPropertyRelative("targetId"));
                    Draw(ref line, property.FindPropertyRelative("maximumAbsoluteXDistance"));
                    Draw(ref line, property.FindPropertyRelative("useYRange"));
                    if (property.FindPropertyRelative("useYRange").boolValue)
                    {
                        Draw(ref line, property.FindPropertyRelative("relativeYMinimum"));
                        Draw(ref line, property.FindPropertyRelative("relativeYMaximum"));
                    }
                    break;
                case TutorialTriggerType.VisibleOnScreen:
                    Draw(ref line, property.FindPropertyRelative("targetObject"));
                    Draw(ref line, property.FindPropertyRelative("targetId"));
                    Draw(ref line, property.FindPropertyRelative("visibilityDelay"));
                    Draw(ref line, property.FindPropertyRelative("visibilityCamera"));
                    break;
                case TutorialTriggerType.VariableCondition:
                    Draw(ref line, property.FindPropertyRelative("variableCondition"), true);
                    break;
                case TutorialTriggerType.Event:
                    Draw(ref line, property.FindPropertyRelative("eventId"));
                    break;
            }
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            int lines = 2;
            TutorialTriggerType type = (TutorialTriggerType)property.FindPropertyRelative("triggerType").enumValueIndex;
            switch (type)
            {
                case TutorialTriggerType.Time: lines += 3; break;
                case TutorialTriggerType.Distance: lines += 6 + (property.FindPropertyRelative("useYRange").boolValue ? 2 : 0); break;
                case TutorialTriggerType.VisibleOnScreen: lines += 4; break;
                case TutorialTriggerType.Event: lines += 1; break;
                case TutorialTriggerType.VariableCondition:
                    return lines * (EditorGUIUtility.singleLineHeight + 2f) + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("variableCondition"), true);
            }
            return lines * (EditorGUIUtility.singleLineHeight + 2f);
        }

        private static void Draw(ref Rect line, SerializedProperty property, bool children = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, children);
            line.height = height;
            EditorGUI.PropertyField(line, property, children);
            line.y += height + 2f;
            line.height = EditorGUIUtility.singleLineHeight;
        }
    }

    [CustomPropertyDrawer(typeof(TutorialSequenceEntry))]
    public sealed class TutorialSequenceEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }
            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight + 2f;
            Draw(ref line, property.FindPropertyRelative("enabled"));
            DrawTutorialPopup(ref line, property);
            Draw(ref line, property.FindPropertyRelative("trigger"), true);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            return (EditorGUIUtility.singleLineHeight + 2f) * 3f +
                   EditorGUI.GetPropertyHeight(property.FindPropertyRelative("trigger"), true);
        }

        private static void DrawTutorialPopup(ref Rect line, SerializedProperty entry)
        {
            SerializedProperty idProperty = entry.FindPropertyRelative("tutorialStableId");
            TutorialManager manager = entry.serializedObject.targetObject as TutorialManager;
            if (manager == null)
            {
                Draw(ref line, idProperty);
                return;
            }

            string[] names = new[] { "<Missing>" }.Concat(manager.Tutorials.Select(t => t == null ? "<Null>" : t.TutorialName)).ToArray();
            int current = 0;
            for (int i = 0; i < manager.Tutorials.Count; i++)
                if (manager.Tutorials[i] != null && string.Equals(manager.Tutorials[i].StableId, idProperty.stringValue, StringComparison.Ordinal)) current = i + 1;
            int selected = EditorGUI.Popup(line, "Tutorial Reference", current, names);
            idProperty.stringValue = selected <= 0 || manager.Tutorials[selected - 1] == null ? string.Empty : manager.Tutorials[selected - 1].StableId;
            line.y += EditorGUIUtility.singleLineHeight + 2f;
        }

        private static void Draw(ref Rect line, SerializedProperty property, bool children = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, children);
            line.height = height;
            EditorGUI.PropertyField(line, property, children);
            line.y += height + 2f;
            line.height = EditorGUIUtility.singleLineHeight;
        }
    }
}
