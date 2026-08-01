Shader "MeixiEcho/Realistic Foliage"
{
    Properties
    {
        _BaseMap("Foliage Atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.38
        _WindStrength("Wind Strength", Range(0, 0.4)) = 0.08
        _Translucency("Leaf Translucency", Range(0, 1)) = 0.45
        _ColorVariation("Natural Variation", Range(0, 0.5)) = 0.2
        _Smoothness("Smoothness", Range(0, 1)) = 0.04
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
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
                half _WindStrength;
                half _Translucency;
                half _ColorVariation;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half variation : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half Hash21(float2 p)
            {
                p = frac(p * half2(123.34h, 456.21h));
                p += dot(p, p + 45.32h);
                return frac(p.x * p.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half bendWeight = saturate(input.uv.y * .72h + .22h);
                half broadWind = sin(_Time.y * .78h + positionWS.x * .17h + positionWS.z * .13h);
                half gust = sin(_Time.y * 1.87h + positionWS.x * -.31h + positionWS.z * .24h) * .34h;
                half flutter = sin(_Time.y * 4.2h + positionWS.x * 1.7h + positionWS.z * 1.3h) * .10h;
                half wind = (broadWind + gust + flutter) * _WindStrength * bendWeight;
                positionWS.x += wind;
                positionWS.z += wind * .43h + broadWind * _WindStrength * .17h;
                positionWS.y -= abs(wind) * _WindStrength * .22h;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.variation = Hash21(floor(positionWS.xz * 1.37h));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 sampleColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = sampleColor * _BaseColor;
                clip(color.a - _Cutoff);

                half naturalTint = lerp(1.0h - _ColorVariation, 1.0h + _ColorVariation * .55h, input.variation);
                half tipVariation = lerp(.92h, 1.08h, saturate(input.uv.y));
                color.rgb *= half3(naturalTint * .94h, naturalTint * tipVariation, naturalTint * .87h);

                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half frontLight = saturate(dot(normalWS, mainLight.direction));
                half backLight = saturate(dot(-normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half transmission = pow(saturate(dot(-mainLight.direction, viewDirection)), 2.4h) * _Translucency;
                half edgeTransmission = backLight * _Translucency * .72h;
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half specular = pow(saturate(dot(normalWS, halfDirection)), lerp(18.0h, 96.0h, _Smoothness)) *
                    lerp(.018h, .12h, _Smoothness);
                half3 lighting = ambient * .72h + mainLight.color *
                    (.20h + frontLight * .68h + edgeTransmission + transmission * .32h) * mainLight.shadowAttenuation;
                color.rgb *= lighting;
                color.rgb += mainLight.color * specular * mainLight.shadowAttenuation;
                color.rgb = MixFog(color.rgb, input.fogFactor);
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
