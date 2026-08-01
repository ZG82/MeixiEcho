Shader "MeixiEcho/Creek Water"
{
    Properties
    {
        _ShallowColor("Shallow Water", Color) = (0.16, 0.62, 0.66, 0.48)
        _DeepColor("Deep Water", Color) = (0.025, 0.19, 0.25, 0.82)
        _FoamColor("Foam", Color) = (0.78, 0.94, 0.90, 0.82)
        _FlowSpeed("Flow Speed", Range(0, 2)) = 0.44
        _WaveStrength("Wave Strength", Range(0, 0.5)) = 0.055
        _RefractionStrength("Refraction", Range(0, 0.08)) = 0.018
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.24
        _Clarity("Water Clarity", Range(0, 1)) = 0.72
        _FoamWidth("Contact Foam Width", Range(0.02, 0.8)) = 0.24
        _CausticsStrength("Caustics", Range(0, 1)) = 0.16
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
            Name "CreekForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half _FlowSpeed;
                half _WaveStrength;
                half _RefractionStrength;
                half _ReflectionStrength;
                half _Clarity;
                half _FoamWidth;
                half _CausticsStrength;
            CBUFFER_END

            int _MeixiWaterRippleCount;
            float4 _MeixiWaterRipples[12];
            int _MeixiWaterObstacleCount;
            float4 _MeixiWaterObstacles[12];
            float4 _MeixiWaterInteractor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float vertexTime = _Time.y * _FlowSpeed;
                float broadRipple = sin(output.positionWS.x * 1.18 + output.positionWS.z * 2.06 - vertexTime * 1.35);
                float crossingRipple = sin(output.positionWS.x * -2.37 + output.positionWS.z * 1.31 + vertexTime * 1.82);
                float travellingPulse = sin(output.positionWS.x * 3.7 - vertexTime * 3.1 + sin(output.positionWS.z * 1.4));
                output.positionWS.y += (broadRipple * .52 + crossingRipple * .31 + travellingPulse * .17) * _WaveStrength * .30;
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float2 p = input.positionWS.xz;
                float2 screenUV = input.screenPosition.xy / max(input.screenPosition.w, .0001);

                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float surfaceEyeDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float waterDepth = max(0.0, sceneEyeDepth - surfaceEyeDepth);
                half depth01 = saturate(waterDepth / 1.05);

                half phaseA = p.x * 1.73h + p.y * 2.91h - time * 1.7h;
                half phaseB = p.x * -.83h + p.y * 2.27h + time * 1.15h;
                half phaseC = p.x * 3.92h - p.y * 1.24h - time * 2.35h;
                half waveA = sin(phaseA);
                half waveB = sin(phaseB);
                half waveC = sin(phaseC);
                half2 slope = half2(
                    cos(phaseA) * 1.73h * waveB + waveA * cos(phaseB) * -.83h,
                    cos(phaseA) * 2.91h * waveB + waveA * cos(phaseB) * 2.27h) * _WaveStrength;
                slope += half2(cos(phaseC) * 3.92h, cos(phaseC) * -1.24h) * _WaveStrength * .18h;

                half rippleFoam = 0.0h;
                [unroll]
                for (int rippleIndex = 0; rippleIndex < 12; rippleIndex++)
                {
                    if (rippleIndex >= _MeixiWaterRippleCount) break;
                    float4 ripple = _MeixiWaterRipples[rippleIndex];
                    float age = _Time.y - ripple.z;
                    float alive = saturate(1.0 - age / 2.8) * step(0.0, age);
                    float2 delta = p - ripple.xy;
                    float distanceToRipple = length(delta);
                    float ringRadius = age * lerp(.72, 1.22, ripple.w);
                    float ring = exp2(-pow((distanceToRipple - ringRadius) * 8.2, 2.0)) * alive * ripple.w;
                    slope += normalize(delta + .0001) * ring * .24;
                    rippleFoam += ring * .34;
                }

                half obstacleFoam = 0.0h;
                [unroll]
                for (int obstacleIndex = 0; obstacleIndex < 12; obstacleIndex++)
                {
                    if (obstacleIndex >= _MeixiWaterObstacleCount) break;
                    float4 obstacle = _MeixiWaterObstacles[obstacleIndex];
                    float2 delta = p - obstacle.xy;
                    float distanceToRock = length(delta);
                    float edge = abs(distanceToRock - obstacle.z);
                    float contact = 1.0 - smoothstep(.018, .095, edge);
                    float downstream = saturate(delta.x / max(.08, obstacle.z * 2.8)) *
                        exp2(-abs(delta.y) * 5.2 / max(.15, obstacle.z)) *
                        (1.0 - smoothstep(obstacle.z, obstacle.z * 5.2, distanceToRock));
                    half angularBreakup = sin(atan2(delta.y, delta.x) * 5.0h + p.x * 1.7h - time * 2.1h) * .5h + .5h;
                    obstacleFoam += (contact * lerp(.10h, .24h, angularBreakup) + downstream * .09h) * obstacle.w;
                    slope += normalize(delta + .0001) * contact * .065h;
                }

                half3 normalWS = normalize(half3(-slope.x, 1.0h, -slope.y));
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half specular = pow(saturate(dot(normalWS, halfDirection)), 112.0h) * mainLight.shadowAttenuation;
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), 4.2h);

                float creekCenter = -11.0 + sin((p.x + 15.0) * .21) * 2.0 + sin((p.x - 4.0) * .075) * .34;
                float creekSample = saturate((p.x + 22.0) / 43.0) * 44.0;
                float creekHalfWidth = 2.25 + sin(creekSample * 1.73) * .18 + sin(creekSample * .53) * .12;
                half bankDistance = creekHalfWidth - abs(p.y - creekCenter);
                half bankFoam = 1.0h - smoothstep(.015h, .34h, bankDistance);
                half depthFoam = 1.0h - smoothstep(.018h, max(.025h, _FoamWidth), waterDepth);
                // Depth textures can report a near-zero value over broad flat
                // surfaces on some cameras. Constrain contact foam to the actual
                // procedural bank instead of allowing a white card across the creek.
                depthFoam *= 1.0h - smoothstep(.16h, .58h, bankDistance);
                half foamBreakup = sin(p.x * 3.9h - time * 4.2h + sin(p.y * 5.7h) * 1.4h) * .5h + .5h;
                half foam = saturate((bankFoam * .16h + depthFoam * .30h) * smoothstep(.42h, .94h, foamBreakup) +
                    rippleFoam * .72h + obstacleFoam * .78h);

                half2 distortion = normalWS.xz * _RefractionStrength * saturate(.25h + depth01);
                half3 refractedScene = SampleSceneColor(saturate(screenUV + distortion));
                half3 waterTint = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                half absorption = saturate(depth01 * lerp(.58h, .90h, 1.0h - _Clarity) + fresnel * .16h);
                half3 color = lerp(refractedScene, waterTint, absorption);

                half causticA = sin(p.x * 5.8h - time * 2.8h + sin(p.y * 4.1h));
                half causticB = sin(p.y * 7.3h + time * 2.1h + sin(p.x * 3.7h));
                half caustics = pow(saturate(causticA * causticB * .5h + .5h), 5.0h) *
                    _CausticsStrength * (1.0h - depth01) * (1.0h - foam);
                color += half3(.34h, .48h, .40h) * caustics;

                half3 ambient = SampleSH(normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflect(-viewDirection, normalWS), .08h, 1.0h);
                color *= ambient * .38h + mainLight.color * (.58h + diffuse * .30h) * mainLight.shadowAttenuation;
                color += reflection * fresnel * _ReflectionStrength;
                color += mainLight.color * specular * .30h;
                color = lerp(color, _FoamColor.rgb, saturate(foam * _FoamColor.a));
                color = MixFog(color, input.fogFactor);

                half alpha = saturate(lerp(_ShallowColor.a, _DeepColor.a, depth01) + fresnel * .12h + foam * .12h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
