using UnityEngine; using UnityEngine.UI;
namespace LeniaBeauty {
  public class BeautyLockPalette : MonoBehaviour {
    const string kPaletteProp = "_PaletteTex";
    const string kMainProp    = "_MainTex";
    int idPalette = Shader.PropertyToID(kPaletteProp);
    int idMain    = Shader.PropertyToID(kMainProp);
    int idExposure= Shader.PropertyToID("_Exposure");
    int idGamma   = Shader.PropertyToID("_Gamma");
    int idPOffset = Shader.PropertyToID("_PaletteOffset");
    int idPScale  = Shader.PropertyToID("_PaletteScale");
    int idUseEdges= Shader.PropertyToID("_UseEdges");
    int idEdgeStr = Shader.PropertyToID("_EdgeStrength");
    int idEdgeThr = Shader.PropertyToID("_EdgeThreshold");
    int idUseTrail= Shader.PropertyToID("_UseTrail");
    int idTrailWt = Shader.PropertyToID("_TrailWeight");
    int idUseGlow = Shader.PropertyToID("_UseGlow");
    int idGlowStr = Shader.PropertyToID("_GlowStrength");

    Material mat; Texture2D palette; RawImage ri;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { var go=new GameObject("BeautyLockPalette"); DontDestroyOnLoad(go); go.AddComponent<BeautyLockPalette>(); }

    void Start() {
      ri = FindLeniaRawImage();
      if (!ri || ri.material==null || ri.material.shader==null) { Debug.LogWarning("[Beauty] No Lenia RawImage with Unlit/LeniaPalette found."); return; }
      // use a unique instance so we don't mutate shared assets
      ri.material = new Material(ri.material);
      mat = ri.material;

      // make sure the UI has a visible texture (prevents magenta if the shader expects _MainTex)
      if (ri.texture==null) {
        var src = mat.HasProperty(idMain) ? mat.GetTexture(idMain) : null;
        if (src) ri.texture = src;
      }

      palette = BuildNeon256x256();
      ApplyOnce();
    }

    void LateUpdate() {
      if (!mat) return;
      // Re-assert in case other scripts overwrite during update
      if (!ReferenceEquals(mat.GetTexture(idPalette), palette)) mat.SetTexture(idPalette, palette);
      SetFloatIf(idExposure, 6.0f);
      SetFloatIf(idGamma,    1.05f);
      SetFloatIf(idPScale,   0.90f);     // widen usable band
      SetFloatIf(idPOffset, -0.02f);     // slight shift so lows are near-black
      SetFloatIf(idUseEdges, 1.0f);
      SetFloatIf(idEdgeStr,  0.55f);
      SetFloatIf(idEdgeThr,  0.006f);
      // kill legacy trail/glow so they don't tint the colors
      SetFloatIf(idUseTrail, 0.0f);
      SetFloatIf(idTrailWt,  0.0f);
      SetFloatIf(idUseGlow,  0.0f);
      SetFloatIf(idGlowStr,  0.0f);
    }

    void ApplyOnce() {
      mat.SetTexture(idPalette, palette);
      SetFloatIf(idExposure, 6.0f);
      SetFloatIf(idGamma,    1.05f);
      SetFloatIf(idPScale,   0.90f);
      SetFloatIf(idPOffset, -0.02f);
      SetFloatIf(idUseEdges, 1.0f);
      SetFloatIf(idEdgeStr,  0.55f);
      SetFloatIf(idEdgeThr,  0.006f);
      SetFloatIf(idUseTrail, 0.0f);
      SetFloatIf(idTrailWt,  0.0f);
      SetFloatIf(idUseGlow,  0.0f);
      SetFloatIf(idGlowStr,  0.0f);
      Debug.Log("[Beauty] Locked Unlit/LeniaPalette: neon LUT + edges on, trail/glow off");
    }

    void SetFloatIf(int id, float v) { if (mat.HasProperty(id)) mat.SetFloat(id, v); }

    RawImage FindLeniaRawImage() {
      var all = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var r in all) if (r && r.material && r.material.shader && r.material.shader.name=="Unlit/LeniaPalette") return r;
      // fallback: grab largest RawImage with a material
      RawImage best=null; int bestPx=0;
      foreach (var r in all) { var t=r? r.texture : null; if (!t && r && r.material && r.material.mainTexture) t=r.material.mainTexture; if (!t) continue; int px=t.width*t.height; if (px>bestPx) { bestPx=px; best=r; } }
      return best;
    }

    Texture2D BuildNeon256x256() {
      // some shaders sample with arbitrary V; make the texture square to be safe
      int W=256,H=256; var tex=new Texture2D(W,H,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for (int x=0;x<W;x++) {
        float t=x/(float)(W-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0.08f,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1f,0.4f,1f,1), (t-0.55f)/0.25f);
        else            c=Color.Lerp(new Color(1f,0.4f,1f,1), new Color(0.16f,0f,0.16f,1), (t-0.80f)/0.20f);
        for (int y=0;y<H;y++) tex.SetPixel(x,y,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
