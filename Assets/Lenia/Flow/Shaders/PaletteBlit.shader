Shader "Hidden/Lenia/PaletteBlit"
{
    Properties
    {
        _MainTex   ("Input",   2D)   = "black" {}
        _LUT       ("Palette", 2D)   = "white" {}
        _Exposure  ("Exposure",Float)= 1
        _Pan       ("Pan",     Vector)= (0,0,0,0) // float2 used
        _Zoom      ("Zoom",    Float)= 1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _LUT;
            float  _Exposure;
            float2 _Pan;
            float  _Zoom;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply pan/zoom in UV space
                float z  = max(_Zoom, 0.0001);
                float2 uv = (i.uv - 0.5) / z + 0.5 + _Pan;

                float v = tex2D(_MainTex, uv).r;
                v = saturate(v * _Exposure);
                return tex2D(_LUT, float2(v, 0.5));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
