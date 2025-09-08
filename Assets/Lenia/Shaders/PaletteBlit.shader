Shader "Hidden/PaletteBlit"
{
    Properties { _MainTex("Field", 2D) = "black" {} }
    SubShader {
        Tags{ "RenderType"="Opaque" }
        Pass {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;

            float3 ramp(float t) {
                // simple pleasant ramp (feel free to swap for Turbo if you like)
                return lerp(float3(0,0,0.1), float3(1,1,1), saturate(t));
            }

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i):SV_Target {
                float4 a = tex2D(_MainTex, i.uv);
                float v = a.r; // visualize channel 0 by default
                return float4(ramp(v), 1);
            }
            ENDHLSL
        }
    }
}