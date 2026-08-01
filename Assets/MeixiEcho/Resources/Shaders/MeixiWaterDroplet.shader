Shader "MeixiEcho/Water Droplet"
{
    Properties
    {
        _BaseColor("Droplet Color", Color) = (0.68, 0.90, 0.92, 0.78)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+8"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "WaterDroplet"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half distanceFromCenter = length(input.uv - .5h) * 2.0h;
                half alpha = 1.0h - smoothstep(.58h, 1.0h, distanceFromCenter);
                half highlight = 1.0h - smoothstep(.0h, .52h, length(input.uv - half2(.36h, .34h)) * 2.0h);
                half3 color = input.color.rgb + highlight * .16h;
                return half4(color, input.color.a * alpha);
            }
            ENDHLSL
        }
    }
}
