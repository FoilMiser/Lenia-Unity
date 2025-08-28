Shader "Hidden/Lenia/PaletteBlit"
{
    Properties {
        _MainTex ("Source", 2D) = "black" {}
        _LUT     ("Palette", 2D) = "white" {}
        _Exposure("Exposure", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _LUT;
            float _Exposure;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert (appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float v = saturate(tex2D(_MainTex, i.uv).r);
                fixed4 col = tex2D(_LUT, float2(v, 0.5));
                col.rgb *= _Exposure;
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }
}
