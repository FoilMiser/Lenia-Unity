Shader "Hidden/Lenia/BlitRFloat"
{ Properties{ _MainTex("RFloat",2D)="black"{} }
  SubShader{ Tags{ "RenderPipeline"="UniversalRenderPipeline" "Queue"="Overlay" "RenderType"="Opaque" } Cull Off ZWrite Off ZTest Always
    Pass{ HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
      struct Varyings{float4 positionHCS:SV_POSITION;float2 uv:TEXCOORD0;};
      Varyings vert(Attributes v){Varyings o;o.positionHCS=TransformObjectToHClip(v.positionOS);o.uv=v.uv;return o;}
      TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _Gamma,_Exposure,_Invert;
      half4 frag(Varyings i):SV_Target{float a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;if(_Invert>0.5)a=1.0-a;a=pow(saturate(a*_Exposure),max(1e-4,_Gamma));return half4(a,a,a,1);}
    ENDHLSL }
  }
}
