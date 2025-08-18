Shader "Unlit/StateToColor"
{
    Properties
    {
        _MainTex ("State", 2D) = "black" {}
        _RampTex ("Color Ramp", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_TexelSize;
            sampler2D _RampTex;
            float _Exposure;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert (appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag (v2f i):SV_Target
            {
                float a = tex2D(_MainTex, i.uv).r * _Exposure;
                a = saturate(a);
                float3 col = tex2D(_RampTex, float2(a,0.5)).rgb;
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
