Shader "UI/ELROI Tutorial Spotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,0.78)
        _HoleRect ("Hole Rect", Vector) = (0.4,0.4,0.6,0.6)
        _Shape ("Shape", Float) = 1
        _CornerRadius ("Corner Radius", Float) = 0.02
        _Feather ("Feather", Float) = 0.01
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ELROI Tutorial Spotlight"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float4 _HoleRect;
            float _Shape;
            float _CornerRadius;
            float _Feather;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 center = (_HoleRect.xy + _HoleRect.zw) * 0.5;
                float2 halfSize = max((_HoleRect.zw - _HoleRect.xy) * 0.5, float2(0.0001, 0.0001));
                float distanceToEdge;

                if (_Shape > 1.5)
                {
                    distanceToEdge = (length((input.uv - center) / halfSize) - 1.0) * min(halfSize.x, halfSize.y);
                }
                else
                {
                    float radius = _Shape > 0.5 ? min(_CornerRadius, min(halfSize.x, halfSize.y)) : 0.0;
                    float2 q = abs(input.uv - center) - halfSize + radius;
                    distanceToEdge = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                }

                float inside = 1.0 - smoothstep(-max(_Feather, 0.00001), 0.0, distanceToEdge);
                fixed4 result = input.color;
                result.a *= (1.0 - inside);
                return result;
            }
            ENDCG
        }
    }
}
