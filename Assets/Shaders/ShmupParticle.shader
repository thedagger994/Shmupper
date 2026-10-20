Shader "Shmupper/Particle"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        // Additive, so sparks and embers read as light against the stone rather than as solid
        // chips of plastic. Depth is tested but not written, which keeps overlapping particles
        // from culling each other.
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ShmupParticle"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Mesh particles carry their per-particle colour in the vertex colour channel.
                // Folding alpha into rgb is what makes a particle fade out under additive
                // blending, where the alpha channel itself has no effect.
                half3 c = IN.color.rgb * _BaseColor.rgb * IN.color.a;
                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
