Shader "Hidden/Lenia/PaletteBlit"
{
    Properties
    {
        _MainTex  ("Source", 2D)   = "black" {}
        _LUT      ("Palette", 2D)  = "white" {}
        _Exposure ("Exposure", Float) = 1.0
        _Pan      ("Pan (UV)", Vector) = (0,0,0,0)
        _Zoom     ("Zoom",    Float)  = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _LUT;

            float4 _MainTex_TexelSize;   // auto-provided by Unity
            float  _Exposure;
            float4 _Pan;                 // xy used
            float  _Zoom;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Pan/zoom in UV space.
                // zoom>1 = zoom in; zoom<1 = zoom out.
                float z = max(_Zoom, 0.05);
                float2 uv = (i.uv - 0.5) / z + 0.5 + _Pan.xy;

                // Sample scalar field and map with LUT.
                float v = saturate(tex2D(_MainTex, uv).r);
                fixed4 col = tex2D(_LUT, float2(v, 0.5));

                col.rgb *= _Exposure;
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }
}