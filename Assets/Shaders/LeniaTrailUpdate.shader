Shader "Hidden/LeniaTrailUpdate"
{
    Properties{
        _MainTex("State", 2D) = "black" {}
        _TrailTex("TrailIn", 2D) = "black" {}
        _Decay("Decay", Float) = 0.965
        _Boost("Boost", Float) = 1.0
    }
    SubShader{
        Tags{ "RenderType"="Opaque" } ZWrite Off Cull Off ZTest Always
        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _TrailTex;
            float _Decay,_Boost;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i):SV_Target{
                float v = tex2D(_MainTex, i.uv).r * _Boost;
                float t = tex2D(_TrailTex, i.uv).r * _Decay;
                return float4(max(v,t),0,0,1);
            }
            ENDHLSL
        }
    }
}

