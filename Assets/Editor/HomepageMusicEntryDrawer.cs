using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HomepageMusicEntry))]
public sealed class HomepageMusicEntryDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => (property.FindPropertyRelative("loopEnabled").boolValue ? 5 : 4) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        position.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Draw(ref position, property, "clip");
        Draw(ref position, property, "volume");
        Draw(ref position, property, "loopEnabled");
        if (property.FindPropertyRelative("loopEnabled").boolValue) Draw(ref position, property, "loopCount");
        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    static void Draw(ref Rect rect, SerializedProperty parent, string name)
    {
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.PropertyField(rect, parent.FindPropertyRelative(name));
    }
}
