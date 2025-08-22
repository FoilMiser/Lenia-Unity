using UnityEngine; using UnityEngine.UI; using System.Reflection; using System.Linq;
namespace LeniaBeauty {
  // Robust, minimal: writes palette to BOTH the material AND the LeniaView component,
  // and re-applies right before UI renders so nothing can overwrite it.
  public class BeautyRig : MonoBehaviour {
    const string TargetShader = "Unlit/LeniaPalette";
    static readonly int PID_PaletteTex = Shader.PropertyToID("_PaletteTex");
    static readonly int PID_MainTex    = Shader.PropertyToID("_MainTex");
    static readonly int PID_Exposure   = Shader.PropertyToID("_Exposure");
    static readonly int PID_Gamma      = Shader.PropertyToID("_Gamma");
    static readonly int PID_PalOffset  = Shader.PropertyToID("_PaletteOffset");
    static readonly int PID_PalScale   = Shader.PropertyToID("_PaletteScale");
    static readonly int PID_UseEdges   = Shader.PropertyToID("_UseEdges");
    static readonly int PID_EdgeStr    = Shader.PropertyToID("_EdgeStrength");
    static readonly int PID_EdgeThr    = Shader.PropertyToID("_EdgeThreshold");
    static readonly int PID_UseTrail   = Shader.PropertyToID("_UseTrail");
    static readonly int PID_TrailWt    = Shader.PropertyToID("_TrailWeight");
    static readonly int PID_UseGlow    = Shader.PropertyToID("_UseGlow");
    static readonly int PID_GlowStr    = Shader.PropertyToID("_GlowStrength");

    RawImage ri; Material mat; MonoBehaviour leniaViewMB; Texture2D neon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyRig"); DontDestroyOnLoad(go); go.AddComponent<BeautyRig>(); }

    void Awake(){
      // 1) Find the Lenia RawImage using Unlit/LeniaPalette
      ri = FindRawImageWithShader(TargetShader);
      if(!ri || !ri.material || !ri.material.shader || ri.material.shader.name!=TargetShader){
        Debug.LogWarning("[BeautyRig] Unlit/LeniaPalette RawImage not found."); enabled=false; return;
      }
      // unique material instance so we don't mutate shared asset
      ri.material = new Material(ri.material);
      mat = ri.material;
      // source texture in UI so we never go magenta
      if(!ri.texture && mat.HasProperty(PID_MainTex)) ri.texture = mat.GetTexture(PID_MainTex);

      // 2) Find the LeniaView component living on the LeniaView GameObject (or parent)
      leniaViewMB = ri.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m && m.GetType().Name.ToLowerInvariant().Contains("leniaview"))
                 ?? ri.GetComponentInParent<MonoBehaviour>();

      // 3) Build palette once
      neon = BuildNeonLUT_256x1();

      // 4) Apply now and before each canvas render (beats other updaters)
      ApplyAll();
      Canvas.willRenderCanvases -= OnCanvasRender;
      Canvas.willRenderCanvases += OnCanvasRender;
      Invoke(nameof(ApplyAll), 0.25f); // catch late init
      Debug.Log("[BeautyRig] Active on '"+mat.shader.name+"' (locking palette & params each frame)");
    }

    void OnDestroy(){ Canvas.willRenderCanvases -= OnCanvasRender; }
    void OnCanvasRender(){ ApplyAll(); }
    void LateUpdate(){ ApplyAll(); } // extra safety

    void ApplyAll(){
      if(!mat) return;
      // A) Write to the material
      SetTex(mat, PID_PaletteTex, neon);
      SetFloat(mat, PID_Exposure, 6.0f);
      SetFloat(mat, PID_Gamma,    1.05f);
      SetFloat(mat, PID_PalScale, 0.90f);
      SetFloat(mat, PID_PalOffset,-0.02f);
      SetFloat(mat, PID_UseEdges, 1.0f);
      SetFloat(mat, PID_EdgeStr,  0.55f);
      SetFloat(mat, PID_EdgeThr,  0.006f);
      SetFloat(mat, PID_UseTrail, 0.0f); SetFloat(mat, PID_TrailWt, 0.0f);
      SetFloat(mat, PID_UseGlow,  0.0f); SetFloat(mat, PID_GlowStr, 0.0f);

      // B) Write to the LeniaView *component* so its own code stops overwriting
      if(leniaViewMB){
        var tp = leniaViewMB.GetType();
        // any Texture/Texture2D property/field containing palette/lut/grad
        foreach(var f in tp.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)){
          if(typeof(Texture).IsAssignableFrom(f.FieldType) && NameLooksPalette(f.Name)){
            try{ f.SetValue(leniaViewMB, neon); }catch{}
          }
        }
        foreach(var p in tp.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)){
          if(p.CanWrite && typeof(Texture).IsAssignableFrom(p.PropertyType) && NameLooksPalette(p.Name)){
            try{ p.SetValue(leniaViewMB, neon); }catch{}
          }
        }
        // mirror key floats if present
        SetIfFloatOnMB(tp, "_UseTrail", 0f); SetIfFloatOnMB(tp, "UseTrail", 0f);
        SetIfFloatOnMB(tp, "_UseGlow",  0f); SetIfFloatOnMB(tp, "UseGlow",  0f);
        SetIfFloatOnMB(tp, "_UseEdges", 1f); SetIfFloatOnMB(tp, "UseEdges", 1f);
        SetIfFloatOnMB(tp, "_EdgeStrength", 0.55f); SetIfFloatOnMB(tp, "_EdgeThreshold", 0.006f);
        SetIfFloatOnMB(tp, "DispExposure", 6.0f); SetIfFloatOnMB(tp, "DispGamma", 1.05f);
        SetIfFloatOnMB(tp, "PaletteScale", 0.90f); SetIfFloatOnMB(tp, "PaletteOffset",-0.02f);
        // call any obvious apply/rebuild method
        TryInvoke(leniaViewMB, "Apply", "ApplySettings", "Rebuild", "Refresh", "OnValidate");
      }
    }

    bool NameLooksPalette(string n){ n=n.ToLowerInvariant(); return n.Contains("palette")||n.contains("lut")||n.Contains("grad"); }
    void TryInvoke(object o, params string[] names){
      var tp=o.GetType();
      foreach(var n in names){
        var m=tp.GetMethod(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(m!=null && m.GetParameters().Length==0){ try{ m.Invoke(o,null);}catch{} }
      }
    }
    void SetIfFloatOnMB(System.Type tp, string name, float v){
      var f=tp.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if(f!=null && f.FieldType==typeof(float)){ try{ f.SetValue(leniaViewMB,v);}catch{} }
      var p=tp.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if(p!=null && p.PropertyType==typeof(float) && p.CanWrite){ try{ p.SetValue(leniaViewMB,v);}catch{} }
    }

    void SetTex(Material m, int id, Texture t){ if(m.HasProperty(id)) m.SetTexture(id,t); }
    void SetFloat(Material m, int id, float v){ if(m.HasProperty(id)) m.SetFloat(id,v); }

    RawImage FindRawImageWithShader(string shaderName){
      var all = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      // prefer object literally named LeniaView
      foreach(var r in all){ if(r && r.gameObject.name=="LeniaView" && r.material && r.material.shader && r.material.shader.name==shaderName) return r; }
      foreach(var r in all){ if(r && r.material && r.material.shader && r.material.shader.name==shaderName) return r; }
      return null;
    }

    Texture2D BuildNeonLUT_256x1(){
      int W=256; var tex=new Texture2D(W,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int x=0;x<W;x++){ float t=x/(float)(W-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0.08f,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1f,0.4f,1f,1), (t-0.55f)/0.25f);
        else            c=Color.Lerp(new Color(1f,0.4f,1f,1), new Color(0.16f,0f,0.16f,1), (t-0.80f)/0.20f);
        tex.SetPixel(x,0,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
