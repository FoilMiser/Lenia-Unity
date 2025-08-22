namespace LeniaBeauty {
  using UnityEngine; using UnityEngine.UI;
  public class BeautyLockPalette : MonoBehaviour {
    const string TargetShader = "Unlit/LeniaPalette";
    readonly int idPalette   = Shader.PropertyToID("_PaletteTex");
    readonly int idMain      = Shader.PropertyToID("_MainTex");
    readonly int idExposure  = Shader.PropertyToID("_Exposure");
    readonly int idGamma     = Shader.PropertyToID("_Gamma");
    readonly int idPOffset   = Shader.PropertyToID("_PaletteOffset");
    readonly int idPScale    = Shader.PropertyToID("_PaletteScale");
    readonly int idUseEdges  = Shader.PropertyToID("_UseEdges");
    readonly int idEdgeStr   = Shader.PropertyToID("_EdgeStrength");
    readonly int idEdgeThr   = Shader.PropertyToID("_EdgeThreshold");
    readonly int idUseTrail  = Shader.PropertyToID("_UseTrail");
    readonly int idTrailWt   = Shader.PropertyToID("_TrailWeight");
    readonly int idUseGlow   = Shader.PropertyToID("_UseGlow");
    readonly int idGlowStr   = Shader.PropertyToID("_GlowStrength");

    Material mat; RawImage ri; Texture2D palette;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyLockPalette"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyLockPalette>(); }

    void Start(){
      ri = FindRawImageWithShader(TargetShader);
      if(ri==null || ri.material==null || ri.material.shader==null || ri.material.shader.name!=TargetShader){
        Debug.LogWarning("[Beauty] Unlit/LeniaPalette RawImage not found. Nothing to lock."); enabled=false; return;
      }
      ri.material = new Material(ri.material); // instance so we don't edit shared
      mat = ri.material;

      if(ri.texture==null && mat.HasProperty(idMain)){ var src = mat.GetTexture(idMain); if(src) ri.texture = src; }

      palette = BuildNeon256x256();
      ApplyOnce();
      Debug.Log("[Beauty] Locking palette on '"+mat.shader.name+"'");
    }

    void LateUpdate(){
      if(mat==null || mat.shader==null || mat.shader.name!=TargetShader) return;

      if(mat.HasProperty(idPalette)){
        var cur = mat.GetTexture(idPalette);
        if(!ReferenceEquals(cur, palette)) mat.SetTexture(idPalette, palette);
      }
      SetIf(idExposure, 6.0f);
      SetIf(idGamma,    1.05f);
      SetIf(idPScale,   0.90f);
      SetIf(idPOffset, -0.02f);
      SetIf(idUseEdges, 1.0f);
      SetIf(idEdgeStr,  0.55f);
      SetIf(idEdgeThr,  0.006f);
      SetIf(idUseTrail, 0.0f); SetIf(idTrailWt, 0.0f);
      SetIf(idUseGlow,  0.0f); SetIf(idGlowStr, 0.0f);
    }

    void ApplyOnce(){
      if(mat.HasProperty(idPalette)) mat.SetTexture(idPalette, palette);
      SetIf(idExposure, 6.0f); SetIf(idGamma, 1.05f);
      SetIf(idPScale, 0.90f);  SetIf(idPOffset,-0.02f);
      SetIf(idUseEdges,1.0f);  SetIf(idEdgeStr,0.55f); SetIf(idEdgeThr,0.006f);
      SetIf(idUseTrail,0.0f);  SetIf(idTrailWt,0.0f);
      SetIf(idUseGlow,0.0f);   SetIf(idGlowStr,0.0f);
    }

    void SetIf(int id, float v){ if(mat.HasProperty(id)) mat.SetFloat(id, v); }

    RawImage FindRawImageWithShader(string shaderName){
      var all = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach(var r in all){ if(r && r.gameObject.name=="LeniaView" && r.material && r.material.shader && r.material.shader.name==shaderName) return r; }
      foreach(var r in all){ if(r && r.material && r.material.shader && r.material.shader.name==shaderName) return r; }
      return null;
    }

    Texture2D BuildNeon256x256(){
      int W=256,H=256; var tex=new Texture2D(W,H,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int x=0;x<W;x++){ float t=x/(float)(W-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0.08f,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1f,0.4f,1f,1), (t-0.55f)/0.25f);
        else            c=Color.Lerp(new Color(1f,0.4f,1f,1), new Color(0.16f,0f,0.16f,1), (t-0.80f)/0.20f);
        for(int y=0;y<H;y++) tex.SetPixel(x,y,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
