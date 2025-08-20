Shader "Universal Render Pipeline/Unlit/StateToColorURP"
{
    Properties
    {
        _MainTex ("State", 2D) = "white" {}
        _RampTex ("Color Ramp", 2D) = "white" {}
        _Exposure ("Exposure", Range(0,5)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampTex);  SAMPLER(sampler_RampTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _RampTex_ST;
                float   _Exposure;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes v){
                Varyings o; o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            half4 frag(Varyings i):SV_Target{
                float s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                float3 c = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(s,0)).rgb;
                return half4(c * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
