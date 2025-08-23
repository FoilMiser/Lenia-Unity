Shader "Lenia/BeautyBlur"{
  Properties{ _MainTex("Main",2D)="black"{} }
  SubShader{
    Tags{ "RenderType"="Transparent" } Cull Off ZWrite Off ZTest Always Blend One Zero
    Pass{
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      sampler2D _MainTex; float4 _MainTex_TexelSize;
      float2 _BlurDir; // pixels per tap (set as (radius,0) then (0,radius))

      struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
      struct v2f { float4 pos: SV_Position; float2 uv: TEXCOORD0; };

      v2f vert(appdata v){
        v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
      }

      fixed4 frag(v2f i):SV_Target{
        // 7-tap gaussian blur (separable). Do two passes: horizontal then vertical.
        float2 step = _BlurDir * _MainTex_TexelSize.xy;
        float w0=0.2941176, w1=0.2352941, w2=0.1176471, w3=0.0294118;
        float3 c = tex2D(_MainTex, i.uv).rgb * w0;
        c += (tex2D(_MainTex, i.uv + step).rgb + tex2D(_MainTex, i.uv - step).rgb) * w1;
        c += (tex2D(_MainTex, i.uv + 2*step).rgb + tex2D(_MainTex, i.uv - 2*step).rgb) * w2;
        c += (tex2D(_MainTex, i.uv + 3*step).rgb + tex2D(_MainTex, i.uv - 3*step).rgb) * w3;
        return float4(c,1);
      }
      ENDCG
    }
  }
}
