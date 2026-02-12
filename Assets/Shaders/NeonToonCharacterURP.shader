Shader "Elroi/NeonToonCharacterURP"
{
    Properties
    {
        _BaseMap ("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        // Night vibe controls
        _AmbientColor ("Ambient Night Tint", Color) = (0.20, 0.25, 0.40, 1)
        _AmbientIntensity ("Ambient Intensity", Range(0, 3)) = 1.1

        // Toon lighting controls (uses Main Light direction, but heavily stylized)
        _KeyLightIntensity ("Key Light Intensity", Range(0, 3)) = 0.65
        _ToonThreshold ("Toon Threshold", Range(0, 1)) = 0.45
        _ToonSoftness ("Toon Softness", Range(0.001, 0.5)) = 0.08
        _ShadowTint ("Shadow Tint", Color) = (0.55, 0.60, 0.80, 1)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.55

        // Neon rim bounce
        _RimColor ("Rim Color (Neon)", Color) = (0.25, 0.95, 1.0, 1)
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1.2
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.8

        // Emission lift (keeps characters visible without glowing like a lamp)
        _EmissionColor ("Emission Color", Color) = (0.10, 0.15, 0.25, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 0.35

        // Simple grading to match painted BGs
        _Saturation ("Saturation", Range(0, 2)) = 1.08
        _Contrast ("Contrast", Range(0.5, 2)) = 1.05

        // Optional outline
        [Toggle(_OUTLINE_ON)] _OutlineOn ("Enable Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0.05, 0.07, 0.12, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.03)) = 0.007
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        // =========================================================
        // Forward pass (NOW receives main light shadows)
        // =========================================================
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _AmbientColor;
                float _AmbientIntensity;

                float _KeyLightIntensity;
                float _ToonThreshold;
                float _ToonSoftness;
                float4 _ShadowTint;
                float _ShadowStrength;

                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;

                float4 _EmissionColor;
                float _EmissionIntensity;

                float _Saturation;
                float _Contrast;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3; // <-- added for shadow receiving
            };

            float3 ApplySaturationContrast(float3 c, float sat, float con)
            {
                // Saturation
                float luma = dot(c, float3(0.2126, 0.7152, 0.0722));
                c = lerp(luma.xxx, c, sat);

                // Contrast around 0.5
                c = (c - 0.5) * con + 0.5;
                return c;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.normalWS    = nrm.normalWS;
                OUT.uv          = IN.uv;

                // Compute shadow coord for main light shadows
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Albedo
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;

                // Main light (now includes shadow attenuation)
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 L = normalize(mainLight.direction);

                // Half-Lambert-ish for softer toon feel (keeps night readable)
                float ndl = saturate(dot(N, -L));
                float lambert = ndl * 0.85 + 0.15;

                // Toon ramp with softness
                float toon = smoothstep(_ToonThreshold - _ToonSoftness, _ToonThreshold + _ToonSoftness, lambert);

                // Shadow tint mix (cool night shadows)
                float3 shadowed = lerp(albedo * _ShadowTint.rgb, albedo, toon);
                shadowed = lerp(albedo, shadowed, _ShadowStrength);

                // Apply main light color but keep intensity controlled
                // IMPORTANT: multiply by shadow attenuation so this shader RECEIVES shadows
                float shadowAtten = mainLight.shadowAttenuation;
                float3 lit = shadowed * (mainLight.color.rgb * _KeyLightIntensity) * shadowAtten;

                // Ambient night fill (prevents “dark sticker”)
                float3 ambient = _AmbientColor.rgb * _AmbientIntensity * albedo;

                // Neon rim bounce (NOT shadowed - looks like bounce light)
                float rimTerm = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float3 rim = _RimColor.rgb * rimTerm * _RimIntensity;

                // Emission lift (subtle)
                float3 emission = _EmissionColor.rgb * _EmissionIntensity;

                // Final color
                float3 col = ambient + lit + rim + emission;

                // Simple grading to match painted BGs
                col = ApplySaturationContrast(col, _Saturation, _Contrast);

                return half4(saturate(col), _BaseColor.a * tex.a);
            }
            ENDHLSL
        }

        // =========================================================
        // Shadow caster pass (URP-built, skinned-mesh safe)
        // =========================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }




        // =========================================================
        // Optional outline pass (thin, night-colored)
        // =========================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vertO
            #pragma fragment fragO
            #pragma shader_feature _OUTLINE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vertO (Attributes IN)
            {
                Varyings OUT;

                #if defined(_OUTLINE_ON)
                    float3 pos = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                #else
                    float3 pos = IN.positionOS.xyz;
                #endif

                VertexPositionInputs p = GetVertexPositionInputs(pos);
                OUT.positionCS = p.positionCS;
                return OUT;
            }

            half4 fragO (Varyings IN) : SV_Target
            {
                #if defined(_OUTLINE_ON)
                    return half4(_OutlineColor.rgb, 1);
                #else
                    discard;
                    return 0;
                #endif
            }
            ENDHLSL
        }
    }
}
