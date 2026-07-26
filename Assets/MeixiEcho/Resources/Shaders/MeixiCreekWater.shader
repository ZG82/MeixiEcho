Shader "MeixiEcho/Creek Water"
{
    Properties
    {
        _ShallowColor("Shallow Water", Color) = (0.16, 0.62, 0.66, 0.78)
        _DeepColor("Deep Water", Color) = (0.025, 0.19, 0.25, 0.84)
        _FoamColor("Bank Foam", Color) = (0.72, 0.92, 0.88, 0.72)
        _FlowSpeed("Flow Speed", Range(0, 2)) = 0.44
        _WaveStrength("Wave Strength", Range(0, 0.5)) = 0.055
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
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half _FlowSpeed;
                half _WaveStrength;
            CBUFFER_END

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
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float vertexTime = _Time.y * _FlowSpeed;
                float broadRipple = sin(output.positionWS.x * 1.18 + output.positionWS.z * 2.06 - vertexTime * 1.35);
                float crossingRipple = sin(output.positionWS.x * -2.37 + output.positionWS.z * 1.31 + vertexTime * 1.82);
                output.positionWS.y += (broadRipple * .62 + crossingRipple * .38) * _WaveStrength * .34;
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float2 p = input.positionWS.xz;
                half phaseA = p.x * 1.73h + p.y * 2.91h - time * 1.7h;
                half phaseB = p.x * -.83h + p.y * 2.27h + time * 1.15h;
                half waveA = sin(phaseA);
                half waveB = sin(phaseB);
                half microCurrent = waveA * waveB * .5h + .5h;

                // Two crossing current fields perturb the normal without moving the
                // ribbon vertices, so the creek keeps a continuous shoreline while
                // highlights travel downstream like shallow running water.
                half slopeX = (cos(phaseA) * 1.73h * waveB + waveA * cos(phaseB) * -.83h) * _WaveStrength;
                half slopeZ = (cos(phaseA) * 2.91h * waveB + waveA * cos(phaseB) * 2.27h) * _WaveStrength;
                half3 normalWS = normalize(half3(-slopeX, 1.0h, -slopeZ));
                half3 viewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 halfDirection = normalize(mainLight.direction + viewDirection);
                half specular = pow(saturate(dot(normalWS, halfDirection)), 88.0h) * mainLight.shadowAttenuation;
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), 3.4h);

                half bankDistance = min(input.uv.y, 1.0h - input.uv.y);
                half bankFoam = 1.0h - smoothstep(.018h, .16h, bankDistance);
                half foamBreakup = sin(input.uv.x * 10.5h - time * 4.2h + sin(input.uv.y * 19.0h) * 1.4h) * .5h + .5h;
                half brokenFoam = smoothstep(.72h, .96h, foamBreakup) * bankFoam;
                half foam = saturate(bankFoam * .16h + brokenFoam * .18h);

                half currentLineA = sin(input.uv.x * 11.7h - time * 4.1h + input.uv.y * 6.0h + waveB) * .5h + .5h;
                half currentLineB = sin(input.uv.x * 4.6h - time * 1.9h - input.uv.y * 23.0h + waveA) * .5h + .5h;
                half currentGlint = smoothstep(.94h, .998h, currentLineA) *
                                    smoothstep(.46h, .90h, currentLineB) * (1.0h - bankFoam) * .065h;

                half depthBlend = saturate(.22h + fresnel * .72h);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBlend);
                half3 ambient = SampleSH(normalWS);
                half3 color = waterColor * (ambient * .72h + mainLight.color * (.28h + diffuse * .46h) * mainLight.shadowAttenuation);
                color += mainLight.color * specular * .11h;
                color = lerp(color, _FoamColor.rgb, foam * _FoamColor.a);
                color += _FoamColor.rgb * currentGlint;
                color *= 1.0h + (microCurrent - .5h) * .022h;
                color += fresnel * half3(.038h, .060h, .066h);
                color = MixFog(color, input.fogFactor);

                half alpha = saturate(lerp(_DeepColor.a, _ShallowColor.a, bankFoam) + fresnel * .08h + foam * .12h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
