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
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace FronkonGames.SpiceUp.Slash
{
  ///------------------------------------------------------------------------------------------------------------------
  /// <summary> Render Pass. </summary>
  /// <remarks> Only available for Universal Render Pipeline. </remarks>
  ///------------------------------------------------------------------------------------------------------------------
  public sealed partial class Slash
  {
    [DisallowMultipleRendererFeature]
    private sealed class RenderPass : ScriptableRenderPass
    {
      // Internal use only.
      internal Material material { get; set; }

      private SlashVolume volume;

      private static class ShaderIDs
      {
        public static readonly int Intensity = Shader.PropertyToID("_Intensity");
        public static readonly int EffectTime = Shader.PropertyToID("_EffectTime");

        public static readonly int Progress = Shader.PropertyToID("_Progress");
        public static readonly int Angle = Shader.PropertyToID("_Angle");
        public static readonly int SplitDist = Shader.PropertyToID("_SplitDist");
        public static readonly int DistortPower = Shader.PropertyToID("_DistortPower");
        public static readonly int SlashFade = Shader.PropertyToID("_SlashFade");
        public static readonly int CoreWidth = Shader.PropertyToID("_CoreWidth");
        public static readonly int GlowSpread = Shader.PropertyToID("_GlowSpread");
        public static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        public static readonly int GlowColorBlend = Shader.PropertyToID("_GlowColorBlend");
        public static readonly int SmokeFade = Shader.PropertyToID("_SmokeFade");
        public static readonly int SmokeExpand = Shader.PropertyToID("_SmokeExpand");
        public static readonly int SmokeColor1 = Shader.PropertyToID("_SmokeColor1");
        public static readonly int SmokeSize1 = Shader.PropertyToID("_SmokeSize1");
        public static readonly int SmokeColor1Blend = Shader.PropertyToID("_SmokeColor1Blend");
        public static readonly int SmokeColor2 = Shader.PropertyToID("_SmokeColor2");
        public static readonly int SmokeSize2 = Shader.PropertyToID("_SmokeSize2");
        public static readonly int SmokeColor2Blend = Shader.PropertyToID("_SmokeColor2Blend");
        public static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");

        public static readonly int Brightness = Shader.PropertyToID("_Brightness");
        public static readonly int Contrast = Shader.PropertyToID("_Contrast");
        public static readonly int Gamma = Shader.PropertyToID("_Gamma");
        public static readonly int Hue = Shader.PropertyToID("_Hue");
        public static readonly int Saturation = Shader.PropertyToID("_Saturation");
      }

      /// <summary> Render pass constructor. </summary>
      public RenderPass() : base()
      {
        profilingSampler = new ProfilingSampler(Constants.Asset.AssemblyName);
      }

      /// <summary> Destroy the render pass. </summary>
      ~RenderPass() => material = null;

      private void UpdateMaterial()
      {
        material.shaderKeywords = null;
        material.SetFloat(ShaderIDs.Intensity, volume.intensity.value);

        float time = volume.useScaledTime.value == true ? Time.time : Time.unscaledTime;
        material.SetVector(ShaderIDs.EffectTime, new Vector4(time / 20.0f, time, time * 2.0f, time * 3.0f));

        material.SetFloat(ShaderIDs.Progress, volume.progress.value);
        material.SetFloat(ShaderIDs.Angle, volume.angle.value * Mathf.Deg2Rad);
        material.SetFloat(ShaderIDs.SplitDist, volume.splitDist.value);
        material.SetFloat(ShaderIDs.DistortPower, volume.distortPower.value);
        material.SetFloat(ShaderIDs.SlashFade, volume.slashFade.value);
        material.SetFloat(ShaderIDs.CoreWidth, volume.coreWidth.value);
        material.SetFloat(ShaderIDs.GlowSpread, volume.glowSpread.value);
        material.SetColor(ShaderIDs.GlowColor, volume.glowColor.value);
        material.SetInt(ShaderIDs.GlowColorBlend, (int)volume.glowColorBlend.value);
        material.SetFloat(ShaderIDs.SmokeFade, volume.smokeFade.value);
        material.SetFloat(ShaderIDs.SmokeExpand, volume.smokeExpand.value);
        material.SetColor(ShaderIDs.SmokeColor1, volume.smokeColor1.value);
        material.SetFloat(ShaderIDs.SmokeSize1, volume.smokeSize1.value);
        material.SetInt(ShaderIDs.SmokeColor1Blend, (int)volume.smokeColor1Blend.value);
        material.SetColor(ShaderIDs.SmokeColor2, volume.smokeColor2.value);
        material.SetFloat(ShaderIDs.SmokeSize2, volume.smokeSize2.value);
        material.SetInt(ShaderIDs.SmokeColor2Blend, (int)volume.smokeColor2Blend.value);
        material.SetColor(ShaderIDs.BackgroundColor, volume.backgroundColor.value);

        material.SetFloat(ShaderIDs.Brightness, volume.brightness.value);
        material.SetFloat(ShaderIDs.Contrast, volume.contrast.value);
        material.SetFloat(ShaderIDs.Gamma, 1.0f / volume.gamma.value);
        material.SetFloat(ShaderIDs.Hue, volume.hue.value);
        material.SetFloat(ShaderIDs.Saturation, volume.saturation.value);
      }

      /// <inheritdoc/>
      public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
      {
        volume = VolumeManager.instance.stack.GetComponent<SlashVolume>();
        if (material == null || volume == null || volume.IsActive() == false)
          return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer == true)
          return;

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.camera.cameraType == CameraType.SceneView && volume.affectSceneView.value == false || cameraData.postProcessEnabled == false)
          return;

        TextureHandle source = resourceData.activeColorTexture;
        TextureHandle destination = renderGraph.CreateTexture(source.GetDescriptor(renderGraph));

        UpdateMaterial();

        RenderGraphUtils.BlitMaterialParameters pass = new(source, destination, material, 0);
        renderGraph.AddBlitPass(pass, $"{Constants.Asset.AssemblyName}.Pass");

        resourceData.cameraColor = destination;
      }
    }
  }
}
