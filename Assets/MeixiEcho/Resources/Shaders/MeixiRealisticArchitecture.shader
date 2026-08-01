Shader "MeixiEcho/Realistic Architecture"
{
    Properties
    {
        _BaseMap("Village Material Atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.12
        _MicroStrength("Micro Relief", Range(0, 1)) = 0.35
        _Weathering("Weathering", Range(0, 1)) = 0.55
        _MaterialKind("Material Kind", Range(0, 3)) = 0
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
            Name "ArchitectureForward"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _MicroStrength;
                half _Weathering;
                half _MaterialKind;
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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 geometryNormal = normalize(input.normalWS);

                half broad = sin(input.positionWS.x * 1.73h + input.positionWS.z * 1.19h) *
                    sin(input.positionWS.y * 2.13h - input.positionWS.z * .77h) * .5h + .5h;
                half fineX = sin(input.positionWS.z * 23.1h + input.positionWS.y * 17.3h);
                half fineZ = sin(input.positionWS.x * 19.7h - input.positionWS.y * 13.9h);
                half relief = _MicroStrength * lerp(.026h, .055h, step(1.5h, _MaterialKind));
                half3 normalWS = normalize(geometryNormal + half3(fineX, (fineX + fineZ) * .12h, fineZ) * relief);

                half groundDamp = (1.0h - smoothstep(.30h, 2.25h, input.positionWS.y)) * _Weathering;
                half rainStreak = saturate((broad - .38h) * 2.1h) * saturate(1.0h - abs(geometryNormal.y));
                half roofMaterial = step(2.5h, _MaterialKind);
                half timberMaterial = step(1.5h, _MaterialKind) * (1.0h - roofMaterial);
                half stoneMaterial = step(.5h, _MaterialKind) * (1.0h - step(1.5h, _MaterialKind));
                half dampness = saturate(groundDamp * (.55h + rainStreak * .45h) + roofMaterial * .08h);

                half3 albedo = baseSample * _BaseColor.rgb;
                albedo *= lerp(.90h, 1.07h, broad * _Weathering);
                albedo = lerp(albedo, albedo * half3(.48h, .57h, .52h), dampness);
                albedo = lerp(albedo, albedo * half3(.70h, .78h, .66h), rainStreak * stoneMaterial * .20h);
                albedo = lerp(albedo, albedo * half3(.86h, .74h, .62h), timberMaterial * broad * .12h);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half smoothness = saturate(_Smoothness + dampness * .34h + roofMaterial * .06h);
                half specular = pow(saturate(dot(normalWS, halfDirection)), lerp(18.0h, 116.0h, smoothness)) *
                    lerp(.018h, .24h, smoothness) * mainLight.shadowAttenuation;

                half3 color = albedo * (ambient * .76h + mainLight.color *
                    (.22h + diffuse * .72h) * mainLight.shadowAttenuation);
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
