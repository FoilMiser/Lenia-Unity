using UnityEngine; using UnityEngine.UI; using System.Linq; using System.Reflection;
namespace LeniaBeauty {
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
    bool loggedOnce=false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyRig"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyRig>(); }

    void Awake(){
      ri = FindRawImageWithShader(TargetShader);
      if(!ri || !ri.material || !ri.material.shader || ri.material.shader.name!=TargetShader){
        Debug.LogWarning("[BeautyRig] Unlit/LeniaPalette RawImage not found."); enabled=false; return;
      }
      ri.material = new Material(ri.material); // instance
      mat = ri.material;
      if(!ri.texture && mat.HasProperty(PID_MainTex)) ri.texture = mat.GetTexture(PID_MainTex);

      // find a LeniaView-like component next to the RawImage
      leniaViewMB = ri.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m && m.GetType().Name.ToLowerInvariant().Contains("leniaview"))
                 ?? ri.GetComponentInParent<MonoBehaviour>();

      neon = BuildNeonLUT_256x1();
      ApplyAll();
      Canvas.willRenderCanvases += OnCanvasRender; // apply just before UI draws
      Invoke(nameof(ApplyAll), 0.25f);             // catch late init
      Debug.Log("[BeautyRig] Active; locking palette & params on '"+mat.shader.name+"'");
    }

    void OnDestroy(){ Canvas.willRenderCanvases -= OnCanvasRender; }
    void OnCanvasRender(){ ApplyAll(); }
    void LateUpdate(){ ApplyAll(); } // extra safety

    void ApplyAll(){
      if(!mat) return;
      // A) Material
      if(mat.HasProperty(PID_PaletteTex)) mat.SetTexture(PID_PaletteTex, neon);
      SetF(PID_Exposure,6.0f); SetF(PID_Gamma,1.05f);
      SetF(PID_PalScale,0.90f); SetF(PID_PalOffset,-0.02f);
      SetF(PID_UseEdges,1.0f);  SetF(PID_EdgeStr,0.55f); SetF(PID_EdgeThr,0.006f);
      SetF(PID_UseTrail,0.0f);  SetF(PID_TrailWt,0.0f);
      SetF(PID_UseGlow,0.0f);   SetF(PID_GlowStr,0.0f);

      // B) Component (so internal scripts stop overwriting)
      if(leniaViewMB){
        var tp=leniaViewMB.GetType();
        foreach(var f in tp.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
          if(typeof(Texture).IsAssignableFrom(f.FieldType) && LooksLikePalette(f.Name)) { try{ f.SetValue(leniaViewMB, neon);}catch{} }
        foreach(var p in tp.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
          if(p.CanWrite && typeof(Texture).IsAssignableFrom(p.PropertyType) && LooksLikePalette(p.Name)) { try{ p.SetValue(leniaViewMB, neon);}catch{} }
        SetMBFloat(tp,"UseTrail",0f); SetMBFloat(tp,"_UseTrail",0f);
        SetMBFloat(tp,"UseGlow",0f);  SetMBFloat(tp,"_UseGlow",0f);
        SetMBFloat(tp,"UseEdges",1f); SetMBFloat(tp,"_UseEdges",1f);
        SetMBFloat(tp,"EdgeStrength",0.55f); SetMBFloat(tp,"_EdgeStrength",0.55f);
        SetMBFloat(tp,"EdgeThreshold",0.006f); SetMBFloat(tp,"_EdgeThreshold",0.006f);
        SetMBFloat(tp,"DispExposure",6.0f); SetMBFloat(tp,"DispGamma",1.05f);
        SetMBFloat(tp,"PaletteScale",0.90f); SetMBFloat(tp,"PaletteOffset",-0.02f);
        TryInvoke(leniaViewMB,"Apply","ApplySettings","Rebuild","Refresh","OnValidate");
      }

      if(!loggedOnce){
        var cur = mat.HasProperty(PID_PaletteTex)? mat.GetTexture(PID_PaletteTex) : null;
        Debug.Log($"[BeautyRig] Applied LUT to material: {(cur?cur.width:0)}x{(cur?cur.height:0)}, edges ON, trail/glow OFF");
        loggedOnce=true;
      }
    }

    void SetF(int id,float v){ if(mat.HasProperty(id)) mat.SetFloat(id,v); }
    bool LooksLikePalette(string n){ n=n.ToLowerInvariant(); return n.Contains("palette")||n.Contains("lut")||n.Contains("grad"); }
    void SetMBFloat(System.Type tp,string name,float v){
      var f=tp.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if(f!=null && f.FieldType==typeof(float)) { try{ f.SetValue(leniaViewMB,v);}catch{} }
      var p=tp.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if(p!=null && p.PropertyType==typeof(float) && p.CanWrite) { try{ p.SetValue(leniaViewMB,v);}catch{} }
    }
    void TryInvoke(object o, params string[] names){
      var tp=o.GetType();
      foreach(var n in names){ var m=tp.GetMethod(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(m!=null && m.GetParameters().Length==0){ try{ m.Invoke(o,null);}catch{} } }
    }

    RawImage FindRawImageWithShader(string shaderName){
      var all = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach(var r in all) if(r && r.gameObject.name=="LeniaView" && r.material && r.material.shader && r.material.shader.name==shaderName) return r;
      foreach(var r in all) if(r && r.material && r.material.shader && r.material.shader.name==shaderName) return r;
      return null;
    }

    Texture2D BuildNeonLUT_256x1(){
      int W=256; var tex=new Texture2D(W,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int x=0;x<W;x++){ float t=x/(float)(W-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1f,0.4f,1f,1), (t-0.55f)/0.25f);
        else            c=Color.Lerp(new Color(1f,0.4f,1f,1), new Color(0.16f,0f,0.16f,1), (t-0.80f)/0.20f);
        tex.SetPixel(x,0,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
