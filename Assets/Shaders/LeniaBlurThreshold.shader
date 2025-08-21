Shader "Hidden/LeniaBlurThreshold"
{
    Properties{
        _MainTex("Source", 2D) = "black" {}
        _Threshold("Threshold", Float) = 0.35
        _Sigma("Sigma", Float) = 2.0
        _Direction("Blur Direction", Vector) = (1,0,0,0)
    }
    SubShader{
        Tags{ "RenderType"="Opaque" } ZWrite Off Cull Off ZTest Always
        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float  _Threshold, _Sigma;
            float4 _Direction;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }

            float w(float x, float s){ return exp(-0.5 * (x*x)/(s*s + 1e-6)); }

            fixed4 frag(v2f i):SV_Target{
                float2 stepv = _Direction.xy * _MainTex_TexelSize.xy;
                float s = max(0.25, _Sigma);
                float ww = 0.0, acc = 0.0;
                [unroll] for (int k=-4; k<=4; k++){
                    float2 uv = i.uv + stepv * k;
                    float v = tex2D(_MainTex, uv).r;
                    v = max(0.0, v - _Threshold);
                    float wk = w(k, s);
                    ww += wk; acc += wk * v;
                }
                float outv = (ww > 1e-6) ? (acc / ww) : 0.0;
                return float4(outv,0,0,1);
            }
            ENDHLSL
        }
    }
}
