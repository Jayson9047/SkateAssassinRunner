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
using UnityEngine;
using System;

namespace FronkonGames.SpiceUp.Slash
{
  /// <summary> Int slider with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class IntSliderWithResetAttribute : PropertyAttribute
  {
    public readonly int min;
    public readonly int max;
    public readonly int value;
    public readonly string tooltip;

    public IntSliderWithResetAttribute(int value, int min, int max, string tooltip = "")
    {
      Debug.Assert(value >= min && value <= max, "Value is out of range");
      Debug.Assert(min <= max, "Min must be less than or equal to max");

      this.value = value;
      this.min = min;
      this.max = max;
      this.tooltip = tooltip;
    }
  }

  /// <summary> Float slider with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class FloatSliderWithResetAttribute : PropertyAttribute
  {
    public readonly float min;
    public readonly float max;
    public readonly float value;
    public readonly string tooltip;

    public FloatSliderWithResetAttribute(float value, float min, float max, string tooltip = "")
    {
      Debug.Assert(value >= min && value <= max, "Value is out of range");
      Debug.Assert(min <= max, "Min must be less than or equal to max");

      this.value = value;
      this.min = min;
      this.max = max;
      this.tooltip = tooltip;
    }
  }

  /// <summary> Toggle with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class ToggleWithResetAttribute : PropertyAttribute
  {
    public readonly bool value;
    public readonly string tooltip;

    public ToggleWithResetAttribute(bool value, string tooltip = "")
    {
      this.value = value;
      this.tooltip = tooltip;
    }
  }

  /// <summary> Enum dropdown attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class EnumDropdownAttribute : PropertyAttribute
  {
    public readonly string tooltip;
    public readonly int value;

    public EnumDropdownAttribute(int value, string tooltip = "")
    {
      this.tooltip = tooltip;
      this.value = value;
    }
  }

  /// <summary> Vector2 with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class Vector2WithResetAttribute : PropertyAttribute
  {
    public readonly Vector2 value;
    public readonly string tooltip;

    public Vector2WithResetAttribute(float x, float y, string tooltip = "")
    {
      this.value = new Vector2(x, y);
      this.tooltip = tooltip;
    }
  }

  /// <summary> Vector3 with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class Vector3WithResetAttribute : PropertyAttribute
  {
    public readonly Vector3 value;
    public readonly string tooltip;

    public Vector3WithResetAttribute(float x, float y, float z, string tooltip = "")
    {
      this.value = new Vector3(x, y, z);
      this.tooltip = tooltip;
    }
  }

  /// <summary> Color with reset attribute. </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class ColorWithResetAttribute : PropertyAttribute
  {
    public readonly Color color;
    public readonly string tooltip;

    public ColorWithResetAttribute(uint rgba, string tooltip = "")
    {
      this.color = new Color(
        ((rgba >> 24) & 0xFF) / 255.0f,
        ((rgba >> 16) & 0xFF) / 255.0f,
        ((rgba >> 8) & 0xFF) / 255.0f,
        (rgba & 0xFF) / 255.0f
      );
      this.tooltip = tooltip;
    }
  }
}
