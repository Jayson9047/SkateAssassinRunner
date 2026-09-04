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
Shader "Hidden/Fronkon Games/Spice Up/Slash URP"
{
  Properties
  {
    _MainTex("Main Texture", 2D) = "white" {}
  }

  SubShader
  {
    Tags
    {
      "RenderType" = "Opaque"
      "RenderPipeline" = "UniversalPipeline"
    }
    LOD 100
    ZTest Always ZWrite Off Cull Off

    Pass
    {
      Name "Fronkon Games Spice Up Slash"

      HLSLPROGRAM
      #include "SpiceUp.hlsl"
      #include "ColorBlend.hlsl"

      #pragma vertex SpiceUpVert
      #pragma fragment SpiceUpFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      float _Progress;
      float _Angle;
      float _SplitDist;
      float _DistortPower;
      float _SlashFade;
      float _CoreWidth;
      float _GlowSpread;
      half4 _GlowColor;
      int _GlowColorBlend;
      float _SmokeFade;
      float _SmokeExpand;
      half4 _SmokeColor1;
      float _SmokeSize1;
      int _SmokeColor1Blend;
      half4 _SmokeColor2;
      float _SmokeSize2;
      int _SmokeColor2Blend;
      half4 _BackgroundColor;

      inline float hash(float2 p)
      {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
      }

      inline float noise(float2 p)
      {
        float2 i = floor(p);
        float2 f = frac(p);
        f = f * f * (3.0 - 2.0 * f);

        float a = hash(i);
        float b = hash(i + float2(1.0, 0.0));
        float c = hash(i + float2(0.0, 1.0));
        float d = hash(i + float2(1.0, 1.0));
        return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
      }

      inline float fbm(float2 p)
      {
        float v = 0.0;
        float a = 0.5;
        float2x2 rot = float2x2(0.866, 0.5, -0.5, 0.866);
        for (int i = 0; i < 5; ++i)
        {
          v += a * noise(p);
          p = mul(rot, p) * 2.0 + float2(100.0, 100.0);
          a *= 0.5;
        }
        return v;
      }

      half4 SpiceUpFrag(SpiceUpVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);

        float aspect = _ScreenParams.x / _ScreenParams.y;
        float2 p = uv;
        p.x *= aspect;

        float2 center = float2(0.5 * aspect, 0.5);
        float2 dir = float2(cos(_Angle), sin(_Angle));
        float2 norm = float2(-sin(_Angle), cos(_Angle));

        float t = _Progress;

        float slashAnim = smoothstep(0.0, 0.03, t) * (1.0 - smoothstep(0.03, _SlashFade, t));
        float smokeAnim = smoothstep(0.0, 0.03, t) * (1.0 - smoothstep(0.2, _SmokeFade, t));
        float smokeSpread = 1.0 + smoothstep(0.0, _SmokeFade, t) * _SmokeExpand;

        float dist = dot(p - center, norm);
        float absDist = abs(dist);
        float l = dot(p - center, dir);
        float lengthFade = smoothstep(0.9, 0.0, abs(l));

        float2 distort = norm * sign(dist) * (_DistortPower / (absDist * 20.0 + 1.0)) * slashAnim;
        float side = sign(dist);
        float2 slideOffset = dir * side * (_SplitDist * slashAnim);
        slideOffset.x /= aspect;

        float2 movingUV = uv - slideOffset + distort * 0.5;

        half3 pixel = SAMPLE_MAIN(movingUV).rgb;

        // Hide screen-edge clamping using background color alpha.
        float2 outOfBounds = max(-movingUV, movingUV - 1.0);
        float outOfBoundsMask = smoothstep(0.0, 0.01, max(outOfBounds.x, outOfBounds.y));
        pixel = lerp(color.rgb, _BackgroundColor.rgb, _BackgroundColor.a * outOfBoundsMask);

        // White smoke.
        float smokeMask1 = smoothstep(_SmokeSize1 * lengthFade * smokeSpread, 0.0, absDist);
        float2 smokeUv1 = p * 4.0;
        smokeUv1 -= dir * _EffectTime.y * 0.3;
        smokeUv1 += fbm(p * 3.0 - _EffectTime.y * 0.2) * 1.5;
        float smokeDensity1 = fbm(smokeUv1);
        float smokeAlpha1 = smoothstep(0.3, 0.7, smokeDensity1 * smokeMask1) * smokeAnim;
        float smokeAlpha1Blend = clamp(smokeAlpha1 * 1.5, 0.0, 1.0) * _SmokeColor1.a;
        pixel = lerp(pixel, ColorBlend(_SmokeColor1Blend, pixel, _SmokeColor1.rgb * smokeAlpha1Blend), smokeAlpha1Blend);

        // Black smoke.
        float smokeMask2 = smoothstep(_SmokeSize2 * lengthFade * smokeSpread, 0.0, absDist);
        float2 smokeUv2 = p * 6.0;
        smokeUv2 += dir * _EffectTime.y * 0.2;
        smokeUv2 += fbm(p * 5.0 + _EffectTime.y * 0.4) * 2.0;
        float smokeDensity2 = fbm(smokeUv2);
        float smokeAlpha2 = smoothstep(0.4, 0.8, smokeDensity2 * smokeMask2) * smokeAnim;
        float smokeAlpha2Blend = clamp(smokeAlpha2 * 1.5, 0.0, 1.0) * _SmokeColor2.a;
        pixel = lerp(pixel, ColorBlend(_SmokeColor2Blend, pixel, _SmokeColor2.rgb * smokeAlpha2Blend), smokeAlpha2Blend);

        // Slash glow and core.
        float whiteAura = exp(-absDist * _GlowSpread);
        float coreLine = smoothstep(_CoreWidth, 0.0, absDist);

        pixel += lerp(half3(0.0, 0.0, 0.0), _GlowColor.rgb * _GlowColor.a * 2.0, ColorBlend(_GlowColorBlend, pixel, _GlowColor.rgb * _GlowColor.a)) * whiteAura * slashAnim * lengthFade;
        pixel = lerp(pixel, half3(0.0, 0.0, 0.0), coreLine * slashAnim * lengthFade);

        // Color adjust.
        pixel = ColorAdjust(pixel, _Contrast, _Brightness, _Hue, _Gamma, _Saturation);

        return lerp(color, half4(pixel, 1.0), _Intensity);
      }

      ENDHLSL
    }
  }

  FallBack "Diffuse"
}
