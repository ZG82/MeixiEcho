Shader "MeixiEcho/Realistic Rock"
{
    Properties
    {
        _BaseMap("Rock Surface", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _TextureScale("World Texture Scale", Range(0.2, 5)) = 1.35
        _Smoothness("Dry Smoothness", Range(0, 1)) = 0.14
        _Wetness("Wetness", Range(0, 1)) = 0.45
        _MossStrength("Moss Strength", Range(0, 1)) = 0.36
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
            Name "RockForward"
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
                half4 _BaseColor;
                half _TextureScale;
                half _Smoothness;
                half _Wetness;
                half _MossStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
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
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half3 SampleTriplanar(float3 worldPosition, half3 weights)
            {
                float scale = max(.05, _TextureScale);
                half3 sampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldPosition.zy * scale).rgb;
                half3 sampleY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldPosition.xz * scale).rgb;
                half3 sampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, worldPosition.xy * scale).rgb;
                return sampleX * weights.x + sampleY * weights.y + sampleZ * weights.z;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 geometryNormal = normalize(input.normalWS);
                half3 blendWeights = pow(abs(geometryNormal), 4.0h);
                blendWeights /= max(.001h, blendWeights.x + blendWeights.y + blendWeights.z);
                half3 rockSample = SampleTriplanar(input.positionWS, blendWeights);

                half broadNoise = sin(input.positionWS.x * 2.37h + input.positionWS.z * 1.73h) *
                    sin(input.positionWS.y * 3.12h - input.positionWS.z * 2.08h) * .5h + .5h;
                half fineNoise = sin(input.positionWS.x * 11.1h - input.positionWS.z * 8.7h + input.positionWS.y * 6.4h) * .5h + .5h;
                half upward = saturate(geometryNormal.y * .82h + .18h);
                half moss = saturate((broadNoise * .72h + fineNoise * .28h - .38h) * 2.2h) * upward * _MossStrength;

                float creekCenter = -11.0 + sin((input.positionWS.x + 15.0) * .21) * 2.0 +
                    sin((input.positionWS.x - 4.0) * .075) * .34;
                half nearCreek = 1.0h - smoothstep(2.2h, 4.8h, abs(input.positionWS.z - creekCenter));
                nearCreek *= 1.0h - smoothstep(21.0h, 25.0h, abs(input.positionWS.x));
                half submerged = 1.0h - smoothstep(-.30h, .16h, input.positionWS.y);
                half wetness = saturate(_Wetness * max(nearCreek * .72h, submerged));

                half3 albedo = rockSample * _BaseColor.rgb;
                albedo = lerp(albedo, albedo * half3(.42h, .52h, .48h), wetness);
                albedo = lerp(albedo, albedo * half3(.46h, .68h, .39h), moss);

                half3 proceduralNormal = normalize(geometryNormal + half3(
                    sin(input.positionWS.z * 18.3h + input.positionWS.y * 9.7h),
                    sin(input.positionWS.x * 16.1h - input.positionWS.z * 7.2h) * .42h,
                    sin(input.positionWS.x * 14.8h + input.positionWS.y * 11.5h)) * .045h);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(proceduralNormal, mainLight.direction));
                half3 ambient = SampleSH(proceduralNormal);
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half smoothness = saturate(_Smoothness + wetness * .61h);
                half specular = pow(saturate(dot(proceduralNormal, halfDirection)), lerp(20.0h, 142.0h, smoothness)) *
                    lerp(.025h, .32h, smoothness) * mainLight.shadowAttenuation;

                half3 color = albedo * (ambient * .78h + mainLight.color * (.22h + diffuse * .72h) * mainLight.shadowAttenuation);
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
