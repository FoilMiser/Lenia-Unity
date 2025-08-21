Shader "Unlit/LeniaPalette"
{
    Properties{
        _MainTex        ("State", 2D) = "black" {}
        _TrailTex       ("Trail", 2D) = "black" {}
        _PaletteTex     ("Palette", 2D) = "white" {}

        _Exposure       ("Exposure", Float) = 8.0
        _Gamma          ("Gamma", Float) = 1.2
        _PaletteOffset  ("Palette Offset", Float) = 0.0
        _PaletteScale   ("Palette Scale", Float) = 1.0

        _UseEdges       ("Use Edges", Float) = 1
        _EdgeStrength   ("Edge Strength", Float) = 0.8
        _EdgeThreshold  ("Edge Threshold", Float) = 0.015

        _UseTrail       ("Use Trail", Float) = 1
        _TrailWeight    ("Trail Weight", Float) = 0.6
        _TrailTint      ("Trail Tint", Color) = (1,0.85,0.2,1)
        _TrailMode      ("Trail Mode (0=Screen,1=Add,2=Lerp)", Float) = 2.0

        _UseGlow        ("Use Glow", Float) = 1
        _GlowTex        ("GlowTex", 2D) = "black" {}
        _GlowStrength   ("Glow Strength", Float) = 0.7
        _GlowTint       ("Glow Tint", Color) = (1,0.8,0.4,1)
    }
    SubShader{
        Tags{ "RenderType"="Opaque" }
        ZWrite Off Cull Off ZTest Always
        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _TrailTex, _PaletteTex, _GlowTex;
            float4 _MainTex_TexelSize;

            float  _Exposure, _Gamma, _PaletteOffset, _PaletteScale;
            float  _UseEdges, _EdgeStrength, _EdgeThreshold;
            float  _UseTrail, _TrailWeight, _TrailMode;
            float4 _TrailTint;

            float  _UseGlow, _GlowStrength;
            float4 _GlowTint;

            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_full v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }

            float3 palette(float v){
                v = saturate(v * _PaletteScale + _PaletteOffset);
                return tex2D(_PaletteTex, float2(v, 0.5)).rgb;
            }

            fixed4 frag(v2f i):SV_Target{
                // base field
                float a  = tex2D(_MainTex, i.uv).r;
                float vv = pow( saturate(a * _Exposure), max(1e-3, _Gamma) );
                float3 col = palette(vv);

                // edges (simple gradient magnitude)
                if (_UseEdges > 0.5){
                    float2 px = _MainTex_TexelSize.xy;
                    float vl = tex2D(_MainTex, i.uv - float2(px.x,0)).r;
                    float vr = tex2D(_MainTex, i.uv + float2(px.x,0)).r;
                    float vu = tex2D(_MainTex, i.uv + float2(0,px.y)).r;
                    float vd = tex2D(_MainTex, i.uv - float2(0,px.y)).r;
                    float gx = abs(vr - vl);
                    float gy = abs(vu - vd);
                    float g  = sqrt(gx*gx + gy*gy);
                    float edge = smoothstep(_EdgeThreshold*0.5, _EdgeThreshold, g) * _EdgeStrength;
                    col = saturate(col + edge.xxx);
                }

                // trail (three blend modes)
                if (_UseTrail > 0.5){
                    float tr = tex2D(_TrailTex, i.uv).r;
                    float t = saturate(tr * _TrailWeight);
                    float3 tint = saturate(_TrailTint.rgb);
                    float3 trailCol = tint * t;

                    if (_TrailMode < 0.5) {
                        // SCREEN
                        col = 1.0 - (1.0 - col) * (1.0 - trailCol);
                    } else if (_TrailMode < 1.5) {
                        // ADD
                        col = saturate(col + trailCol);
                    } else {
                        // LERP to tint
                        col = lerp(col, tint, t);
                    }
                }

                // glow (screen composite)
                if (_UseGlow > 0.5){
                    float g = tex2D(_GlowTex, i.uv).r * _GlowStrength;
                    float3 gt = saturate(_GlowTint.rgb) * saturate(g);
                    col = 1.0 - (1.0 - col) * (1.0 - gt);
                }

                return float4(col,1);
            }
            ENDHLSL
        }
    }
}
