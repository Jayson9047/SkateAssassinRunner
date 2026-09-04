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
using UnityEditor;

namespace FronkonGames.SpiceUp.Slash.Editor
{
  /// <summary> Slash Volume inspector. </summary>
  [CustomEditor(typeof(SlashVolume))]
  public class SlashVolumeInspector : Inspector
  {
    protected override void InspectorGUI()
    {
      /////////////////////////////////////////////////
      // Common.
      /////////////////////////////////////////////////
      DrawFloatSliderWithReset("intensity");

      /////////////////////////////////////////////////
      // Slash.
      /////////////////////////////////////////////////
      Separator();

      DrawFloatSliderWithReset("progress", "Slash");
      IndentLevel++;
      DrawFloatSliderWithReset("angle", "Angle");
      DrawFloatSliderWithReset("splitDist", "Split");
      DrawFloatSliderWithReset("distortPower", "Distort");
      DrawFloatSliderWithReset("slashFade", "Fade");
      DrawFloatSliderWithReset("coreWidth", "Width");
      IndentLevel--;

      DrawColorWithReset("glowColor", "Glow");
      IndentLevel++;
      DrawEnumDropdownWithReset("glowColorBlend", "Blend", ColorBlends.Additive);
      DrawFloatSliderWithReset("glowIntensity", "Intensity");
      DrawFloatSliderWithReset("glowSpread", "Spread");
      IndentLevel--;

      DrawFloatSliderWithReset("smokeSize1", "Smoke #1");
      IndentLevel++;
      DrawEnumDropdownWithReset("smokeColor1Blend", "Blend", ColorBlends.Additive);
      DrawColorWithReset("smokeColor1", "Color");
      DrawFloatSliderWithReset("smokeExpand", "Expand");
      DrawFloatSliderWithReset("smokeFade", "Fade");
      IndentLevel--;

      DrawFloatSliderWithReset("smokeSize2", "Smoke #2");
      IndentLevel++;
      DrawEnumDropdownWithReset("smokeColor2Blend", "Blend", ColorBlends.Darken);
      DrawColorWithReset("smokeColor2", "Color");
      DrawFloatSliderWithReset("smokeExpand", "Expand");
      DrawFloatSliderWithReset("smokeFade", "Fade");
      IndentLevel--;

      DrawColorWithReset("backgroundColor", "Background");
    }

    protected override void ResetValues() => ((SlashVolume)target).Reset();

    protected override void CheckForErrors()
    {
      if (Slash.IsInAnyRenderFeatures() == false)
      {
        Separator();
        EditorGUILayout.HelpBox($"Renderer Feature '{Constants.Asset.Name}' not found. You must add it as a Render Feature.", MessageType.Error);
      }
      else
      {
        Slash[] effects = Slash.Instances;
        bool anyEnabled = false;
        for (int i = 0; i < effects.Length; i++)
        {
          if (effects[i].isActive == true)
          {
            anyEnabled = true;
            break;
          }
        }

        if (anyEnabled == false)
        {
          Separator();
          EditorGUILayout.HelpBox($"No Renderer Feature '{Constants.Asset.Name}' is active. You must activate it in the Render Features.", MessageType.Warning);
        }
      }
    }
  }
}
