Shader "Unlit/LeniaBeautyUI"
	{
    Properties
    {
        _MainTex("Simulation", 2D) = "white" {}
        _PaletteTex("Palette", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 6
        _Gamma ("Gamma", Float) = 1.05
        _PaletteScale ("Palette Scale", Float) = 0.86
        _PaletteOffset ("Palette Offset", Float) = 0
        _EdgeStrength ("Edge Strength", Float) = 0.45
        _EdgeThreshold ("Edge Threshold", Float) = 0.01
        _Bands ("Contour Bands", Float) = 12
        _BandStrength ("Band Strength", Float) = 0.18
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha  
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
           };

            sampler2D _MainTex;
            sampler2D _PaletteTex;
            float4 _MainTex_ST;

            float _Exposure, _Gamma, _PaletteScale, _PaletteOffset;
            float _EdgeStrength, _EdgeThreshold;
            float _Bands, _BandStrength;

            v2f vert(appdata v)
           {
                v2f o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o ;
            }

            float3 ToneMapACES(float3 x)
           {
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                return saturate((x*(*(ax + b))) / (x*(*(c*x + d ) + e));
           }

            fixed4 frag(v2f i) : SV_Target
           {
                float v = tex2D(_MainTex, i.uv).r;

                // palette lookup
                float t = saturate(v * _PaletteScale + _PaletteOffset);
                float3 col = tex2D(_PaletteTex, float2(t, 0.5)).rgb;

                // edge from screen-space gradient
                float gx = ddx(v);
                float gy = ddy(v);
                float edge = saturate((sqrt(gx*g^ + gy*gy) - _EdgeThreshold) / max(1e-6, _EdgeThreshold)) * _EdgeStrength;
                // iso-contour bands
                float band = 1.0 - smoothstep(0.48, 0.52, abs(frac(t * _Bands) - 0.5));
                col += edge.xxx + (band * _BandStrength).xxx;

                // exposure + gamma + tonemap
                col *= _Exposure;
                col = ToneMapACES(col);
                col = pow(max(col, 1e-6), 1.0 / max(_Gamma, 1e-6));

                return floatt4(col, 1.0);
            }
            ENDCG
        }
    }
}
