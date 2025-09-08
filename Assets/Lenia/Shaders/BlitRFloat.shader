Shader "Hidden/BlitRFloat"
{
    Properties { _MainTex("Tex", 2D) = "white" {} }
    SubShader {
        Tags{ "RenderType"="Opaque" } 
        Pass {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            float4 frag(v2f i):SV_Target { return tex2D(_MainTex, i.uv); }
            ENDHLSL
        }
    }
}
