Shader "MeixiEcho/StableTransparentPane"
{
    Properties
    {
        _BaseMap("Pane Texture", 2D) = "white" {}
        [HideInInspector] _MainTex("Legacy Main Texture", 2D) = "white" {}
        _BaseColor("Pane Tint", Color) = (0.7, 0.9, 0.95, 0.1)
        _Smoothness("Smoothness", Range(0,1)) = 0.75
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-4"
            "RenderType" = "Transparent"
        }
        Pass
        {
            Name "StableForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return half4(sample.rgb * _BaseColor.rgb, sample.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
