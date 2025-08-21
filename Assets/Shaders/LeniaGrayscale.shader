Shader "Unlit/LeniaGrayscale"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Exposure ("Exposure", Float) = 8.0 _Gamma ("Gamma", Float) = 1.0 }
    SubShader { Tags { "RenderType"="Opaque" } LOD 100
        Pass {
            ZWrite Off Cull Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Exposure; float _Gamma;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag(v2f i):SV_Target {
                float v = tex2D(_MainTex, i.uv).r * _Exposure;
                v = pow(saturate(v), max(1e-3,_Gamma));
                return float4(v,v,v,1);
            }
            ENDHLSL
        }
    }
}
