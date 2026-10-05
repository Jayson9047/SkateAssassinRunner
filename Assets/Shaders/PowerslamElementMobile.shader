Shader "ELROI/VFX/Powerslam Element Mobile"
{
    Properties
    {
        _MainTex ("Vendor flame or impact mask", 2D) = "white" {}
        _NoiseTex ("Flame motion", 2D) = "white" {}
        [HDR] _ElementColor ("Element", Color) = (1,0.12,0.01,1)
        [HDR] _HotColor ("Hot core", Color) = (1,0.9,0.7,1)
        _Intensity ("Intensity", Float) = 2
        _NoiseStrength ("Motion strength", Range(0,1)) = 0.2
        _Pan ("Texture motion", Vector) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _ElementColor;
                half4 _HotColor;
                float4 _Pan;
                half _Intensity;
                half _NoiseStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 noiseUV = input.uv * 2.0 + _Time.y * float2(0.17, -0.23);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float2 uv = input.uv + _Pan.xy * _Time.y + (noise - 0.5) * _NoiseStrength * 0.06;
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                mask = saturate(mask - noise * _NoiseStrength * 0.25);
                half core = smoothstep(0.68, 0.98, mask);
                half3 color = lerp(_ElementColor.rgb, _HotColor.rgb, core) * _Intensity;
                return half4(color * input.color.rgb, sqrt(mask) * input.color.a);
            }
            ENDHLSL
        }
    }
}
