////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Martin Bustos @FronkonGames <fronkongames@gmail.com>. All rights reserved.
//
// THIS FILE CAN NOT BE HOSTED IN PUBLIC REPOSITORIES.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace FronkonGames.SpiceUp.Slash.Editor
{
  /// <summary> Custom drawers. </summary>
  public abstract partial class Inspector : VolumeComponentEditor
  {
    /// <summary> Draws an IntSliderWithResetAttribute with slider and reset using attribute configuration. </summary>
    protected void DrawIntSliderWithReset(string name, string label = null)
    {
      SerializedDataParameter parameter = UnpackParameter(name);
      if (parameter == null)
        return;

      var attr = GetAttribute<IntSliderWithResetAttribute>(parameter);

      if (attr == null)
      {
        EditorGUILayout.PropertyField(parameter.value, new GUIContent(label ?? parameter.displayName));
        return;
      }

      GUIContent displayLabel = new(label ?? parameter.displayName, attr.tooltip);

      EditorGUILayout.BeginHorizontal();
      {
        EditorGUI.showMixedValue = parameter.overrideState.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = false;

        bool isOverridden = parameter.overrideState.boolValue;
        EditorGUI.BeginDisabledGroup(!isOverridden);

        EditorGUILayout.BeginHorizontal();
        {
          int value = parameter.value.intValue;

          value = EditorGUILayout.IntSlider(displayLabel, value, attr.min, attr.max);

          EditorGUI.EndDisabledGroup();
          int oldIndentLevel = EditorGUI.indentLevel;
          EditorGUI.indentLevel = 0;
          EditorGUILayout.PropertyField(parameter.overrideState, GUIContent.none, GUILayout.Width(16));
          EditorGUI.indentLevel = oldIndentLevel;
          EditorGUI.BeginDisabledGroup(!isOverridden);

          if (ResetButton(attr.value, value != attr.value) == true)
            value = attr.value;

          parameter.value.intValue = value;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
      }
      EditorGUILayout.EndHorizontal();
    }

    /// <summary> Draws an FloatSliderWithResetAttribute with slider and reset using attribute configuration. </summary>
    protected void DrawFloatSliderWithReset(string name, string label = null)
    {
      SerializedDataParameter parameter = UnpackParameter(name);
      if (parameter == null)
        return;

      var attr = GetAttribute<FloatSliderWithResetAttribute>(parameter);

      if (attr == null)
      {
        EditorGUILayout.PropertyField(parameter.value, new GUIContent(label ?? parameter.displayName));
        return;
      }

      GUIContent displayLabel = new(label ?? parameter.displayName, attr.tooltip);

      EditorGUILayout.BeginHorizontal();
      {
        EditorGUI.showMixedValue = parameter.overrideState.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = false;

        bool isOverridden = parameter.overrideState.boolValue;
        EditorGUI.BeginDisabledGroup(!isOverridden);

        EditorGUILayout.BeginHorizontal();
        {
          float value = parameter.value.floatValue;

          value = EditorGUILayout.Slider(displayLabel, value, attr.min, attr.max);

          EditorGUI.EndDisabledGroup();
          int oldIndentLevel = EditorGUI.indentLevel;
          EditorGUI.indentLevel = 0;
          EditorGUILayout.PropertyField(parameter.overrideState, GUIContent.none, GUILayout.Width(16));
          EditorGUI.indentLevel = oldIndentLevel;
          EditorGUI.BeginDisabledGroup(!isOverridden);

          if (ResetButton(attr.value, value != attr.value) == true)
            value = attr.value;

          parameter.value.floatValue = value;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
      }
      EditorGUILayout.EndHorizontal();
    }

    /// <summary> Draws an EnumParameter with dropdown and reset button using generics. </summary>
    /// <typeparam name="T">The enum type</typeparam>
    protected void DrawEnumDropdownWithReset<T>(string name, string label = null, T defaultValue = default) where T : Enum
    {
      SerializedDataParameter parameter = UnpackParameter(name);
      if (parameter == null)
        return;

      GUIContent displayLabel = new(label ?? parameter.displayName);

      EditorGUILayout.BeginHorizontal();
      {
        // Override checkbox
        EditorGUI.showMixedValue = parameter.overrideState.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = false;

        bool isOverridden = parameter.overrideState.boolValue;
        EditorGUI.BeginDisabledGroup(!isOverridden);

        EditorGUILayout.BeginHorizontal();
        {
          // Get current value (stored as int)
          int currentInt = parameter.value.intValue;

          // Validate bounds (safety check)
          Array enumValues = Enum.GetValues(typeof(T));
          if (currentInt < 0 || currentInt >= enumValues.Length)
            currentInt = 0;

          // Convert int to enum
          T currentValue = (T)enumValues.GetValue(currentInt);

          // Draw enum popup
          EditorGUI.BeginChangeCheck();
          T newValue = (T)EditorGUILayout.EnumPopup(displayLabel, currentValue);

          if (EditorGUI.EndChangeCheck() == true)
          {
            // Convert back to int for serialization
            parameter.value.intValue = Convert.ToInt32(newValue);
          }

          EditorGUI.EndDisabledGroup();
          int oldIndentLevel = EditorGUI.indentLevel;
          EditorGUI.indentLevel = 0;
          EditorGUILayout.PropertyField(parameter.overrideState, GUIContent.none, GUILayout.Width(16));
          EditorGUI.indentLevel = oldIndentLevel;
          EditorGUI.BeginDisabledGroup(!isOverridden);

          // Reset button
          bool isDefault = EqualityComparer<T>.Default.Equals(newValue, defaultValue);
          if (ResetButton(defaultValue, !isDefault) == true)
            parameter.value.intValue = Convert.ToInt32(defaultValue);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
      }
      EditorGUILayout.EndHorizontal();
    }

    /// <summary> Draws a ColorWithResetAttribute with color field and reset button. </summary>
    protected void DrawColorWithReset(string name, string label = null)
    {
      SerializedDataParameter parameter = UnpackParameter(name);
      if (parameter == null)
        return;

      var attr = GetAttribute<ColorWithResetAttribute>(parameter);

      Color defaultValue = attr != null ? attr.color : Color.white;
      GUIContent displayLabel = new(label ?? parameter.displayName, attr?.tooltip ?? "");

      EditorGUILayout.BeginHorizontal();
      {
        EditorGUI.showMixedValue = parameter.overrideState.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = false;

        bool isOverridden = parameter.overrideState.boolValue;
        EditorGUI.BeginDisabledGroup(!isOverridden);

        EditorGUILayout.BeginHorizontal();
        {
          Color value = parameter.value.colorValue;

          value = EditorGUILayout.ColorField(displayLabel, value);

          EditorGUI.EndDisabledGroup();
          int oldIndentLevel = EditorGUI.indentLevel;
          EditorGUI.indentLevel = 0;
          EditorGUILayout.PropertyField(parameter.overrideState, GUIContent.none, GUILayout.Width(16));
          EditorGUI.indentLevel = oldIndentLevel;
          EditorGUI.BeginDisabledGroup(!isOverridden);

          bool isDefault = value == defaultValue;
          if (ResetButton(defaultValue, !isDefault) == true)
            value = defaultValue;

          parameter.value.colorValue = value;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
      }
      EditorGUILayout.EndHorizontal();
    }

    protected void DrawToggleWithReset(string name, string label = null, bool defaultValue = default)
    {
      SerializedDataParameter parameter = UnpackParameter(name);
      if (parameter == null)
        return;

      GUIContent displayLabel = new(label ?? parameter.displayName);

      EditorGUILayout.BeginHorizontal();
      {
        EditorGUI.showMixedValue = parameter.overrideState.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = false;

        bool isOverridden = parameter.overrideState.boolValue;
        EditorGUI.BeginDisabledGroup(!isOverridden);

        EditorGUILayout.BeginHorizontal();
        {
          bool value = parameter.value.boolValue;

          EditorGUI.BeginChangeCheck();
          value = EditorGUILayout.Toggle(displayLabel, value);

          EditorGUI.EndDisabledGroup();
          int oldIndentLevel = EditorGUI.indentLevel;
          EditorGUI.indentLevel = 0;
          EditorGUILayout.PropertyField(parameter.overrideState, GUIContent.none, GUILayout.Width(16));
          EditorGUI.indentLevel = oldIndentLevel;
          EditorGUI.BeginDisabledGroup(!isOverridden);

          bool isDefault = EqualityComparer<bool>.Default.Equals(value, defaultValue);
          if (ResetButton(defaultValue, !isDefault) == true)
            value = defaultValue;

          parameter.value.boolValue = value;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
      }
      EditorGUILayout.EndHorizontal();
    }

    /// <summary> Helper to extract attributes from SerializedDataParameter. </summary>
    protected T GetAttribute<T>(SerializedDataParameter param) where T : Attribute
    {
      if (param.attributes == null)
        return null;

      return param.attributes.OfType<T>().FirstOrDefault();
    }
  }
}
