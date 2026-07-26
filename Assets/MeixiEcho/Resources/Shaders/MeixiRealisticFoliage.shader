Shader "MeixiEcho/Realistic Foliage"
{
    Properties
    {
        _BaseMap("Foliage Atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }

        Pass
        {
            Name "ForwardLitFoliage"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half windWeight = saturate(input.positionOS.y * .16h);
                half wind = sin(_Time.y * 1.15h + positionWS.x * .23h + positionWS.z * .17h) * .045h;
                positionWS.xz += half2(wind, wind * .55h) * windWeight;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - _Cutoff);
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half frontLight = saturate(dot(normalWS, mainLight.direction));
                half backLight = saturate(dot(-normalWS, mainLight.direction)) * .34h;
                half3 ambient = SampleSH(normalWS);
                half translucency = saturate(dot(-mainLight.direction, GetWorldSpaceNormalizeViewDir(input.positionWS))) * .18h;
                half3 lighting = ambient * .74h + mainLight.color *
                    (.24h + frontLight * .66h + backLight + translucency) * mainLight.shadowAttenuation;
                color.rgb *= lighting;
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }
    }
}
