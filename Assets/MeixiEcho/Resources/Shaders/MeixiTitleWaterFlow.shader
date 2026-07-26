Shader "MeixiEcho/UI/TitleWaterFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                fixed4 original = tex2D(_MainTex, uv);

                float lowerBand = smoothstep(0.08, 0.24, uv.y) * (1.0 - smoothstep(0.52, 0.68, uv.y));
                float horizontalFocus = smoothstep(0.08, 0.30, uv.x) * (1.0 - smoothstep(0.76, 0.96, uv.x));
                float coolSurface = saturate((original.b - original.r) * 4.2 + (original.g - original.r) * 1.4 + 0.12);
                float waterMask = lowerBand * horizontalFocus * coolSurface;

                float time = _Time.y;
                float waveA = sin(dot(uv, float2(31.0, 17.0)) - time * 0.54);
                float waveB = sin(dot(uv, float2(-19.0, 29.0)) + time * 0.37 + 1.7);
                float waveC = sin(dot(uv, float2(13.0, 43.0)) - time * 0.23 + 4.1);
                float2 flowUv = uv;
                flowUv.x += (waveA * 0.00072 + waveB * 0.00046) * waterMask;
                flowUv.y += (waveB * 0.00042 + waveC * 0.00034) * waterMask;
                fixed4 flowing = tex2D(_MainTex, flowUv);

                float irregularWave = waveA * 0.46 + waveB * 0.34 + waveC * 0.20;
                float ripple = smoothstep(0.70, 0.94, irregularWave);
                fixed3 waterGlint = fixed3(0.16, 0.48, 0.60) * ripple * waterMask * 0.065;
                fixed4 result = lerp(original, flowing, waterMask * 0.52);
                result.rgb += waterGlint;
                return result * input.color;
            }
            ENDCG
        }
    }
}
