Shader "Meixi/InventoryBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0,5)) = 2.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _BlurSize;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _BlurSize;
                half4 sum = 0;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * .22;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + texel * float2(1,0)) * .12;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - texel * float2(1,0)) * .12;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + texel * float2(0,1)) * .12;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - texel * float2(0,1)) * .12;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + texel * float2(1,1)) * .075;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - texel * float2(1,1)) * .075;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + texel * float2(1,-1)) * .075;
                sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - texel * float2(1,-1)) * .075;
                return sum;
            }
            ENDHLSL
        }
    }
}
