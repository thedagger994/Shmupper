Shader "Shmupper/VertexLit"
{
    Properties
    {
        _BaseMap ("Stone", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Ambient ("Ambient", Color) = (0.17,0.18,0.24,1)
        _Boost ("Light Boost", Range(0,4)) = 1.25
        _DetailStrength ("Texture Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ShmupVertexLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float  fogCoord    : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _Ambient;
                float  _Boost;
                float  _DetailStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.color = IN.color;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // The stone map is a greyscale detail pattern, not an albedo. Lerping toward
                // white by _DetailStrength lets the texture be dialled out entirely without
                // changing the material's colour, which keeps the per-floor hue in one place.
                half3 stone = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                stone = lerp(half3(1, 1, 1), stone, _DetailStrength);

                half3 lightTerm = IN.color.rgb * _Boost + _Ambient.rgb;
                half3 c = _BaseColor.rgb * stone * lightTerm;

                c = MixFog(c, IN.fogCoord);
                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
