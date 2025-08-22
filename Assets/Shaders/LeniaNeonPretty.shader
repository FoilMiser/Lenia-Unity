Shader "Hidden/Lenia/NeonPretty" {
  Properties{
    _MainTex("Main",2D)="white"{}
    _LUT("Palette",2D)="white"{}
    _Min("Min",Float)=0.26
    _Max("Max",Float)=0.62
    _Exposure("Exposure",Float)=5.0
    _Gamma("Gamma",Float)=1.07
    _EdgeStrength("Edge Strength",Float)=0.55
    _EdgeThreshold("Edge Threshold",Float)=0.005
    _EdgeWidth("Edge Width",Float)=0.020
    _EdgeTint("Edge Tint",Color)=(0.95,0.45,1.0,1)
    _HighlightClamp("Highlight Clamp",Float)=0.92
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
      float _Min,_Max,_Exposure,_Gamma,_EdgeStrength,_EdgeThreshold,_EdgeWidth,_HighlightClamp; float4 _EdgeTint;

      struct app{ float4 pos:POSITION; float2 uv:TEXCOORD0; };
      struct v2f{ float4 pos:SV_Position; float2 uv:TEXCOORD0; };
      v2f vert(app i){ v2f o; o.pos=UnityObjectToClipPos(i.pos); o.uv=i.uv; return o; }

      float3 ACES(float3 x){ const float a=2.51,b=0.03,c=2.43,d=0.59,e=0.14; return saturate((x*(a*x+b))/(x*(c*x+d)+e)); }

      fixed4 frag(v2f i):SV_Target{
        float v = tex2D(_MainTex, i.uv).r;

        // normalize into [0,1] window (prevents washout)
        float vn = saturate( (v - _Min) / max(1e-5, _Max - _Min) );

        // palette sample
        float3 col = tex2D(_LUT, float2(vn,0.5)).rgb;

        // subtle colored edge, not additive white
        float2 px=_MainTex_TexelSize.xy;
        float dx = tex2D(_MainTex, i.uv + float2( px.x,0)).r - tex2D(_MainTex, i.uv + float2(-px.x,0)).r;
        float dy = tex2D(_MainTex, i.uv + float2(0, px.y)).r - tex2D(_MainTex, i.uv + float2(0,-px.y)).r;
        float g = sqrt(dx*dx + dy*dy);
        float e = saturate((g - _EdgeThreshold) / max(_EdgeWidth,1e-5));
        col = lerp(col, _EdgeTint.rgb, e * _EdgeStrength);

        // tone map + white clamp (avoid bright leaks)
        col *= _Exposure;
        col = pow(col, 1.0/_Gamma);
        col = ACES(col);
        col = min(col, _HighlightClamp.xxx);
        col /= _HighlightClamp;

        return float4(col,1);
      }
      ENDCG
    }
  }
}
