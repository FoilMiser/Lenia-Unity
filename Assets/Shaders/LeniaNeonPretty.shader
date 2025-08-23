Shader "Hidden/Lenia/NeonPretty" {
  Properties{
    _MainTex("Main",2D)="white"{}
    _LUT("Palette",2D)="white"{}
    _Min("Min",Float)=0.26
    _Max("Max",Float)=0.62
    _Contrast("Contrast",Float)=1.12
    _Exposure("Exposure",Float)=4.9
    _Gamma("Gamma",Float)=1.06
    _HighlightClamp("Highlight Clamp",Float)=0.90
    _BackColor("Background",Color)=(0.02,0.04,0.10,1)

    _EdgeStrength("Edge Strength",Float)=0.55
    _EdgeThreshold("Edge Threshold",Float)=0.0045
    _EdgeWidth("Edge Width",Float)=0.020
    _EdgeFineW("Edge Fine Weight",Float)=0.65
    _EdgeCoarseW("Edge Coarse Weight",Float)=0.25
    _EdgeTint("Edge Tint",Color)=(0.95,0.45,1.0,1)

    _ShadeAmt("Shading Mix",Float)=0.07
    _ShadeDepth("Shading Depth",Float)=3.0
  }
  SubShader{
    Tags{ "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha
    Cull Off ZWrite Off ZTest Always
    Pass{
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      sampler2D _MainTex; float4 _MainTex_TexelSize;
      sampler2D _LUT;
      float _Min,_Max,_Contrast,_Exposure,_Gamma,_HighlightClamp; float4 _BackColor;
      float _EdgeStrength,_EdgeThreshold,_EdgeWidth,_EdgeFineW,_EdgeCoarseW; float4 _EdgeTint;
      float _ShadeAmt,_ShadeDepth;

      struct app{ float4 pos:POSITION; float2 uv:TEXCOORD0; };
      struct v2f{ float4 pos:SV_Position; float2 uv:TEXCOORD0; };
      v2f vert(app i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }

      float nrand(float2 uv){ return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453); }
      float3 ACES(float3 x){ const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14; return saturate((x*(a*x+b))/(x*(c*x+d)+e)); }

      fixed4 frag(v2f i):SV_Target{
        float v = tex2D(_MainTex, i.uv).r;

        // normalize + contrast + gentle sigmoid
        float vn = saturate( (v - _Min) / max(1e-5, _Max - _Min) );
        vn = pow(vn, _Contrast);
        vn = smoothstep(0.0, 1.0, vn);

        // tiny dither before LUT (remove banding)
        vn = saturate(vn + (nrand(i.uv) - 0.5) / 1024.0);

        // palette
        float3 col = tex2D(_LUT, float2(vn, 0.5)).rgb;

        // deep, consistent background
        float backMix = smoothstep(0.02, 0.12, vn);
        col = lerp(_BackColor.rgb, col, backMix);

        // dual-scale edges, masked to mid-range
        float2 px = _MainTex_TexelSize.xy;
        float dx1 = tex2D(_MainTex, i.uv + float2( px.x,0)).r - tex2D(_MainTex, i.uv + float2(-px.x,0)).r;
        float dy1 = tex2D(_MainTex, i.uv + float2(0, px.y)).r - tex2D(_MainTex, i.uv + float2(0,-px.y)).r;
        float g1 = sqrt(dx1*dx1 + dy1*dy1);

        float2 p2 = px * 2.0;
        float dx2 = tex2D(_MainTex, i.uv + float2( p2.x,0)).r - tex2D(_MainTex, i.uv + float2(-p2.x,0)).r;
        float dy2 = tex2D(_MainTex, i.uv + float2(0, p2.y)).r - tex2D(_MainTex, i.uv + float2(0,-p2.y)).r;
        float g2 = sqrt(dx2*dx2 + dy2*dy2);

        float e1 = saturate((g1 - _EdgeThreshold) / max(_EdgeWidth,1e-5));
        float e2 = saturate((g2 - _EdgeThreshold) / max(_EdgeWidth*1.5,1e-5));
        float e  = saturate(_EdgeFineW*e1 + _EdgeCoarseW*e2);
        float edgeMask = smoothstep(0.20, 0.80, vn);
        e *= edgeMask;

        // multiplicative tint (prevents white rims)
        float3 tint = normalize(_EdgeTint.rgb + 1e-3);
        col = lerp(col, saturate(col * tint), e * _EdgeStrength);

        // tiny shape shading
        float3 n = normalize(float3(-dx1*_ShadeDepth, -dy1*_ShadeDepth, 1.0));
        float3 L = normalize(float3(0.4, 0.6, 0.7));
        float s  = saturate(dot(n,L));
        col = lerp(col, col * (s*1.1), saturate(_ShadeAmt));

        // tone map + TRUE highlight clamp (no re-brighten)
        col *= _Exposure;
        col = pow(col, 1.0/_Gamma);
        col = ACES(col);
        col = min(col, _HighlightClamp.xxx);

        return float4(col,1);
      }
      ENDCG
    }
  }
}
