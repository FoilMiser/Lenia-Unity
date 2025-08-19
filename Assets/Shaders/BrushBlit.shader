Shader "Hidden/BrushBlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;           // current env
            float4 _MainTex_TexelSize;

            float2 _BrushCenter;          // UV 0..1
            float  _BrushRadius;          // UV radius
            float  _Hardness;             // 0..1
            float  _Opacity;              // 0..1
            float  _TargetValue;          // 0 = wall, 1 = open

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert (appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                float src = tex2D(_MainTex, i.uv).r;
                float d = distance(i.uv, _BrushCenter);

                float inner = _BrushRadius * _Hardness;
                float outer = _BrushRadius;

                float w = (outer > inner)
                        ? 1.0 - saturate((d - inner) / (outer - inner))   // soft falloff
                        : step(d, outer);                                  // hard

                float a = saturate(w * _Opacity);
                float outv = lerp(src, _TargetValue, a);
                return float4(outv, 0, 0, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
