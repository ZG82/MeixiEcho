Shader "MeixiEcho/Realistic Ground"
{
    Properties
    {
        _BaseMap("Ground Albedo", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Smoothness("Dry Smoothness", Range(0, 1)) = 0.04
        _Wetness("Creek-bank Wetness", Range(0, 1)) = 0.3
        _NormalStrength("Micro Normal", Range(0, 2)) = 0.6
        _MacroVariation("Macro Variation", Range(0, 1)) = 0.24
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "GroundForward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

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
            float4 _BaseMap_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Wetness;
                half _NormalStrength;
                half _MacroVariation;
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
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 texel = max(_BaseMap_TexelSize.xy * 2.0, float2(.0002, .0002));
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half heightX = dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + float2(texel.x, 0)).rgb,
                    half3(.299h, .587h, .114h));
                half heightZ = dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + float2(0, texel.y)).rgb,
                    half3(.299h, .587h, .114h));
                half centerHeight = dot(baseSample.rgb, half3(.299h, .587h, .114h));

                half3 geometryNormal = normalize(input.normalWS);
                half3 tangent = normalize(abs(geometryNormal.y) > .88h ? half3(1, 0, 0) : cross(half3(0, 1, 0), geometryNormal));
                half3 bitangent = normalize(cross(geometryNormal, tangent));
                half2 gradient = half2(heightX - centerHeight, heightZ - centerHeight) * _NormalStrength * 8.0h;
                half3 normalWS = normalize(geometryNormal - tangent * gradient.x - bitangent * gradient.y);

                float macroA = sin(input.positionWS.x * .31 + input.positionWS.z * .23);
                float macroB = sin(input.positionWS.x * -.13 + input.positionWS.z * .41 + 1.7);
                half macro = saturate(macroA * macroB * .5h + .5h);
                half stochastic = Hash21(floor(input.positionWS.xz * .72));
                half variation = lerp(1.0h - _MacroVariation, 1.0h + _MacroVariation * .55h,
                    macro * .72h + stochastic * .28h);

                float creekCenter = -11.0 + sin((input.positionWS.x + 15.0) * .21) * 2.0 +
                    sin((input.positionWS.x - 4.0) * .075) * .34;
                half creekRange = 1.0h - smoothstep(2.35h, 5.6h, abs(input.positionWS.z - creekCenter));
                creekRange *= 1.0h - smoothstep(21.0h, 25.0h, abs(input.positionWS.x));
                half wetness = saturate(_Wetness * creekRange * (.68h + macro * .32h));

                half3 albedo = baseSample.rgb * _BaseColor.rgb * variation;
                albedo = lerp(albedo, albedo * half3(.48h, .57h, .52h), wetness);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half smoothness = saturate(_Smoothness + wetness * .58h);
                half specularPower = lerp(18.0h, 156.0h, smoothness);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) *
                    lerp(.035h, .28h, smoothness) * mainLight.shadowAttenuation;

                half3 color = albedo * (ambient * .82h + mainLight.color * (.18h + diffuse * .78h) * mainLight.shadowAttenuation);
                color += mainLight.color * specular;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
