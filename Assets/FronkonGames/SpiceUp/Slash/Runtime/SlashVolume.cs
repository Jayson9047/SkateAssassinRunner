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
using UnityEngine;
using UnityEngine.Rendering;

namespace FronkonGames.SpiceUp.Slash
{
  /// <summary> Slash Volume. </summary>
  [Serializable, VolumeComponentMenu("Fronkon Games/Spice Up/Slash"), HelpURL(Constants.Support.Documentation)]
  public sealed class SlashVolume : VolumeComponent, IPostProcessComponent
  {
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region Common settings.

    /// <summary> Controls the intensity of the effect [0, 1]. Default 1. </summary>
    [FloatSliderWithReset(1.0f, 0.0f, 1.0f, "Controls the intensity of the effect [0, 1]. Default 1.")]
    public FloatSliderParameterLinear intensity = new(1.0f, 0.0f, 1.0f);

    #endregion
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region Slash settings.

    /// <summary> Progress of the slash [0, 1]. Default 0. </summary>
    [FloatSliderWithReset(0.0f, 0.0f, 1.0f, "Progress of the slash [0, 1]. Default 0.")]
    public FloatSliderParameterNoInterpolation progress = new(0.0f, 0.0f, 1.0f);

    /// <summary> Angle of the slash in degrees [0, 360]. Default 149. </summary>
    [FloatSliderWithReset(149.0f, 0.0f, 360.0f, "Angle of the slash in degrees [0, 360]. Default 149.")]
    public FloatSliderParameterNoInterpolation angle = new(149.0f, 0.0f, 360.0f);

    /// <summary> Maximum split distance [0, 1]. Default 0.1. </summary>
    [FloatSliderWithReset(0.1f, 0.0f, 1.0f, "Maximum split distance [0, 1]. Default 0.1.")]
    public FloatSliderParameterNoInterpolation splitDist = new(0.1f, 0.0f, 1.0f);

    /// <summary> Distortion strength [0, 1]. Default 0.08. </summary>
    [FloatSliderWithReset(0.08f, 0.0f, 1.0f, "Distortion strength [0, 1]. Default 0.08.")]
    public FloatSliderParameterNoInterpolation distortPower = new(0.08f, 0.0f, 1.0f);

    /// <summary> Slash fade end timing [0, 1]. Default 0.8. </summary>
    [FloatSliderWithReset(0.8f, 0.0f, 1.0f, "Slash fade end timing [0, 1]. Default 0.8.")]
    public FloatSliderParameterNoInterpolation slashFade = new(0.8f, 0.0f, 1.0f);

    /// <summary> Core line thickness [0, 0.1]. Default 0.015. </summary>
    [FloatSliderWithReset(0.015f, 0.0f, 0.1f, "Core line thickness [0, 0.1]. Default 0.015.")]
    public FloatSliderParameterNoInterpolation coreWidth = new(0.015f, 0.0f, 0.1f);

    /// <summary> Glow spread [1, 100]. Default 40. </summary>
    [FloatSliderWithReset(40.0f, 1.0f, 100.0f, "Glow spread [1, 100]. Default 40.")]
    public FloatSliderParameterNoInterpolation glowSpread = new(40.0f, 1.0f, 100.0f);

    /// <summary> Glow color. Default white. </summary>
    [ColorWithReset(0xFFFFFFFF, "Glow color. Default white.")]
    public ColorParameterNoInterpolation glowColor = new(Color.white);

    /// <summary> Glow color blend. Default Additive. </summary>
    [EnumDropdown((int)ColorBlends.Additive, "Glow color blend. Default Additive.")]
    public EnumParameterNoInterpolation<ColorBlends> glowColorBlend = new(ColorBlends.Additive);

    /// <summary> Smoke fade end timing [0, 1]. Default 0.99. </summary>
    [FloatSliderWithReset(0.99f, 0.0f, 1.0f, "Smoke fade end timing [0, 1]. Default 0.99.")]
    public FloatSliderParameterNoInterpolation smokeFade = new(0.99f, 0.0f, 1.0f);

    /// <summary> Smoke expansion [0, 1]. Default 0.3. </summary>
    [FloatSliderWithReset(0.3f, 0.0f, 1.0f, "Smoke expansion [0, 1]. Default 0.3.")]
    public FloatSliderParameterNoInterpolation smokeExpand = new(0.3f, 0.0f, 1.0f);

    /// <summary> White smoke color. Default light gray. </summary>
    [ColorWithReset(0xE6E6F2FF, "White smoke color. Default light gray.")]
    public ColorParameterNoInterpolation smokeColor1 = new(new Color(0.9f, 0.9f, 0.95f, 1.0f));

    /// <summary> White smoke color blend. Default Additive. </summary>
    [EnumDropdown((int)ColorBlends.Additive, "White smoke color blend. Default Additive.")]
    public EnumParameterNoInterpolation<ColorBlends> smokeColor1Blend = new(ColorBlends.Additive);

    /// <summary> White smoke size [0, 1]. Default 0.4. </summary>
    [FloatSliderWithReset(0.4f, 0.0f, 1.0f, "White smoke size [0, 1]. Default 0.4.")]
    public FloatSliderParameterNoInterpolation smokeSize1 = new(0.4f, 0.0f, 1.0f);

    /// <summary> Black smoke color. Default dark gray. </summary>
    [ColorWithReset(0x050505FF, "Black smoke color. Default dark gray.")]
    public ColorParameterNoInterpolation smokeColor2 = new(new Color(0.02f, 0.02f, 0.02f, 1.0f));

    /// <summary> Black smoke size [0, 1]. Default 0.6. </summary>
    [FloatSliderWithReset(0.6f, 0.0f, 1.0f, "Black smoke size [0, 1]. Default 0.6.")]
    public FloatSliderParameterNoInterpolation smokeSize2 = new(0.6f, 0.0f, 1.0f);

    /// <summary> Black smoke color blend. Default Darken. </summary>
    [EnumDropdown((int)ColorBlends.Darken, "Black smoke color blend. Default Darken.")]
    public EnumParameterNoInterpolation<ColorBlends> smokeColor2Blend = new(ColorBlends.Darken);

    /// <summary> Background color used to hide screen-edge clamping when the image splits. Default black with alpha 1. </summary>
    [ColorWithReset(0x000000FF, "Background color used to hide screen-edge clamping when the image splits. Default black.")]
    public ColorParameterNoInterpolation backgroundColor = new(new Color(0.0f, 0.0f, 0.0f, 1.0f));

    #endregion
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region Color settings.

    /// <summary> Brightness [-1, 1]. Default 0. </summary>
    [FloatSliderWithReset(0.0f, -1.0f, 1.0f, "Brightness [-1, 1]. Default 0.")]
    public FloatSliderParameterNoInterpolation brightness = new(0.0f, -1.0f, 1.0f);

    /// <summary> Contrast [0, 10]. Default 1. </summary>
    [FloatSliderWithReset(1.0f, 0.0f, 10.0f, "Contrast [0, 10]. Default 1.")]
    public FloatSliderParameterNoInterpolation contrast = new(1.0f, 0.0f, 10.0f);

    /// <summary> Gamma [0.1, 10]. Default 1. </summary>
    [FloatSliderWithReset(1.0f, 0.1f, 10.0f, "Gamma [0.1, 10]. Default 1.")]
    public FloatSliderParameterNoInterpolation gamma = new(1.0f, 0.1f, 10.0f);

    /// <summary> The color wheel [0, 1]. Default 0. </summary>
    [FloatSliderWithReset(0.0f, 0.0f, 1.0f, "The color wheel [0, 1]. Default 0.")]
    public FloatSliderParameterNoInterpolation hue = new(0.0f, 0.0f, 1.0f);

    /// <summary> Intensity of colors [0, 2]. Default 1. </summary>
    [FloatSliderWithReset(1.0f, 0.0f, 2.0f, "Intensity of colors [0, 2]. Default 1.")]
    public FloatSliderParameterNoInterpolation saturation = new(1.0f, 0.0f, 2.0f);

    #endregion
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region Advanced settings.

    /// <summary> Does it affect the Scene View? </summary>
    [ToggleWithReset(false, "Does it affect the Scene View?")]
    public BoolParameterNoInterpolation affectSceneView = new(false);

    /// <summary> Use scaled time. </summary>
    [ToggleWithReset(true, "Use scaled time.")]
    public BoolParameterNoInterpolation useScaledTime = new(true);

    #endregion
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary> Reset to default values. </summary>
    public void Reset()
    {
      intensity.value = 1.0f;

      progress.value = 0.0f;
      angle.value = 149.0f;
      splitDist.value = 0.1f;
      distortPower.value = 0.08f;
      slashFade.value = 0.8f;
      coreWidth.value = 0.015f;
      glowSpread.value = 40.0f;
      glowColorBlend.value = ColorBlends.Additive;
      glowColor.value = Color.white;
      smokeFade.value = 0.99f;
      smokeExpand.value = 0.3f;
      smokeColor1.value = new Color(0.9f, 0.9f, 0.95f, 1.0f);
      smokeColor1Blend.value = ColorBlends.Additive;
      smokeSize1.value = 0.4f;
      smokeColor2.value = new Color(0.02f, 0.02f, 0.02f, 1.0f);
      smokeSize2.value = 0.6f;
      smokeColor2Blend.value = ColorBlends.Darken;
      backgroundColor.value = new Color(0.0f, 0.0f, 0.0f, 1.0f);

      brightness.value = 0.0f;
      contrast.value = 1.0f;
      gamma.value = 1.0f;
      hue.value = 0.0f;
      saturation.value = 1.0f;

      affectSceneView.value = false;
      useScaledTime.value = true;
    }

    /// <summary> Is the effect active? </summary>
    public bool IsActive() => intensity.overrideState && intensity.value > 0.0f;

    /// <summary> Is the effect tile compatible? </summary>
    public bool IsTileCompatible() => false;
  }
}
