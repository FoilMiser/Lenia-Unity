Shader "Lenia/BeautyFinal" {
  Properties{
    _LeniaTex("Lenia",2D)="white"{}
    _LUT("Palette",2D)="white"{}
    _WhitePoint("White Point",Float)=0.88
    _HighlightClamp("Highlight Clamp",Float)=0.86

    _BaseTex("Base",2D)="black"{}
    _GlowTex("Glow",2D)="black"{}
    _GlowStrength("Glow Strength",Float)=0.35
  }
  SubShader{
    Tags{ "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
    CGINCLUDE
      #include "UnityCG.cginc"
      sampler2D _LeniaTex; float4 _LeniaTex_TexelSize;
      sampler2D _LUT;
      float _WhitePoint, _HighlightClamp;

      sampler2D _BaseTex; sampler2D _GlowTex; float _GlowStrength;

      struct v2f{ float4 pos:SV_Position; float2 uv:TEXCOORD0; };
      v2f vert(appdata_full i){ v2f o; o.pos=UnityObjectToClipPos(i.vertex); o.uv=i.texcoord; return o; }

      float3 ACES(float3 x){ const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14; return saturate((x*(a*x+b))/(x*(c*x+d)+e)); }
    ENDCG

    // PASS 0: Palette + dual-scale edges (outputs base color)
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      sampler2D _MainTex;  // unused (Blit API quirk)

      fixed4 frag(v2f i):SV_Target{
        float v = tex2D(_LeniaTex, i.uv).r;

        // normalize from the sim (assume 0..1 already; add gentle sigmoid for softness)
        v = smoothstep(0.0, 1.0, v);

        // palette lookup with tiny dither to avoid banding
        float d = frac(sin(dot(i.uv, float2(12.9898,78.233))) * 43758.5453) - 0.5;
        float t = saturate(v + d/1024.0);
        float3 col = tex2D(_LUT, float2(t, 0.5)).rgb;

        // white cap BEFORE any mapping
        col = min(col, _WhitePoint.xxx);

        // edges at two scales (sobel-ish)
        float2 px=_LeniaTex_TexelSize.xy;
        float dx1 = tex2D(_LeniaTex, i.uv + float2( px.x,0)).r - tex2D(_LeniaTex, i.uv + float2(-px.x,0)).r;
        float dy1 = tex2D(_LeniaTex, i.uv + float2(0, px.y)).r - tex2D(_LeniaTex, i.uv + float2(0,-px.y)).r;
        float g1 = sqrt(dx1*dx1 + dy1*dy1);

        float2 p2=px*2;
        float dx2 = tex2D(_LeniaTex, i.uv + float2( p2.x,0)).r - tex2D(_LeniaTex, i.uv + float2(-p2.x,0)).r;
        float dy2 = tex2D(_LeniaTex, i.uv + float2(0, p2.y)).r - tex2D(_LeniaTex, i.uv + float2(0,-p2.y)).r;
        float g2 = sqrt(dx2*dx2 + dy2*dy2);

        float e1 = saturate((g1 - 0.0045) / 0.020);
        float e2 = saturate((g2 - 0.0045) / 0.030);
        float e  = saturate(0.65*e1 + 0.25*e2) * smoothstep(0.20,0.80,v);

        // color the edges by slightly increasing saturation (no white rims)
        float3 tint = normalize(float3(0.95,0.45,1.0)+1e-3);
        col = lerp(col, saturate(col * tint), e*0.55);

        // tone map + hard highlight cap
        col = ACES(col);
        col = min(col, _HighlightClamp.xxx);

        return float4(col,1);
      }
      ENDCG
    }

    // PASS 1: Composite base + blurred glow
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      fixed4 frag(v2f i):SV_Target{
        float3 base = tex2D(_BaseTex, i.uv).rgb;
        float3 glow = tex2D(_GlowTex, i.uv).rgb;
        float3 col  = saturate(base + _GlowStrength * glow);
        return float4(col,1);
      }
      ENDCG
    }
  }
}
