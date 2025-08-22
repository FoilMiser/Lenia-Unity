using UnityEngine; using UnityEngine.UI; using System.Linq;

namespace LeniaBeauty {
  public class BeautyOverlayPretty : MonoBehaviour {
    RawImage overlay; Material mat; Texture src;
    float min=0.22f, max=0.70f, exposure=5.5f, gamma=1.05f, edge=0.6f, edgeThr=0.005f, edgeW=0.02f;
    int idLUT, idMin, idMax, idExp, idGam, idEdge, idThr, idW, idTint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyOverlayPretty"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyOverlayPretty>(); }

    void Start(){
      // 1) find the real sim texture from the existing Lenia RawImage
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      var source = ris.FirstOrDefault(r=> r && r.gameObject.name=="LeniaView") ?? ris.FirstOrDefault(r=> r && r.texture);
      if(source==null){ Debug.LogWarning("[Beauty] No RawImage source found."); return; }

      src = source.texture;
      if(src==null && source.material){
        Shader srcSh = source.material.shader;
        int pc = (srcSh != null) ? srcSh.GetPropertyCount() : 0;
        for(int i=0;i<pc;i++){
          if(srcSh.GetPropertyType(i)!=UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
          string nm = srcSh.GetPropertyName(i).ToLowerInvariant();
          if(nm.Contains("palette")||nm.Contains("lut")||nm.Contains("grad")||nm.Contains("trail")||nm.Contains("glow")) continue;
          var t = source.material.GetTexture(srcSh.GetPropertyNameId(i));
          if(t!=null){ src=t; break; }
        }
      }
      if(src==null){ Debug.LogWarning("[Beauty] Could not locate sim texture."); return; }

      // 2) Build overlay canvas on top
      var cv=new GameObject("BeautyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c=cv.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=5000;
      overlay=new GameObject("BeautyRawImage", typeof(RawImage)).GetComponent<RawImage>(); overlay.transform.SetParent(cv.transform,false);
      var rt=overlay.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;

      // 3) Material using new pretty shader
      var prettySh=Shader.Find("Hidden/Lenia/NeonPretty"); if(!prettySh){ Debug.LogError("[Beauty] Pretty shader not found."); return; }
      mat=new Material(prettySh); overlay.material=mat; overlay.texture=src; mat.mainTexture=src;

      idLUT=Shader.PropertyToID("_LUT"); idMin=Shader.PropertyToID("_Min"); idMax=Shader.PropertyToID("_Max");
      idExp=Shader.PropertyToID("_Exposure"); idGam=Shader.PropertyToID("_Gamma");
      idEdge=Shader.PropertyToID("_EdgeStrength"); idThr=Shader.PropertyToID("_EdgeThreshold"); idW=Shader.PropertyToID("_EdgeWidth");
      idTint=Shader.PropertyToID("_EdgeTint");

      // 4) default palette & params
      ApplyPalette(0); // Neon
      PushParams();

      var old = GameObject.Find("LeniaViewCanvas"); if(old) old.SetActive(false);
      Debug.Log($"[Beauty] Pretty overlay active. Source {src.width}x{src.height}, shader={mat.shader.name}");
    }

    void Update(){
      // Palettes
      if(Input.GetKeyDown(KeyCode.Alpha1)) ApplyPalette(0);
      if(Input.GetKeyDown(KeyCode.Alpha2)) ApplyPalette(1);
      if(Input.GetKeyDown(KeyCode.Alpha3)) ApplyPalette(2);
      // Min/Max window
      if(Input.GetKeyDown(KeyCode.LeftBracket))  { min=Mathf.Clamp01(min-0.02f); PushParams(); }
      if(Input.GetKeyDown(KeyCode.RightBracket)) { min=Mathf.Clamp01(min+0.02f); PushParams(); }
      if(Input.GetKeyDown(KeyCode.Semicolon))    { max=Mathf.Clamp01(max-0.02f); PushParams(); }
      if(Input.GetKeyDown(KeyCode.Quote))        { max=Mathf.Clamp01(max+0.02f); PushParams(); }
      // Exposure/Gamma
      if(Input.GetKeyDown(KeyCode.Minus))        { exposure=Mathf.Max(0.1f,exposure-0.3f); PushParams(); }
      if(Input.GetKeyDown(KeyCode.Equals))       { exposure+=0.3f; PushParams(); }
      if(Input.GetKeyDown(KeyCode.Comma))        { gamma=Mathf.Clamp(gamma-0.02f,0.7f,2.0f); PushParams(); }
      if(Input.GetKeyDown(KeyCode.Period))       { gamma=Mathf.Clamp(gamma+0.02f,0.7f,2.0f); PushParams(); }
    }

    void PushParams(){
      if(!mat) return;
      mat.SetFloat(idMin, min); mat.SetFloat(idMax, max);
      mat.SetFloat(idExp, exposure); mat.SetFloat(idGam, gamma);
      mat.SetFloat(idEdge, edge); mat.SetFloat(idThr, edgeThr); mat.SetFloat(idW, edgeW);
    }

    void ApplyPalette(int idx){
      Texture2D lut; Color edgeTint;
      switch(idx){
        default: // Neon Petri
          lut = MakeLUT(256, x=>{
            if(x<0.08f) return Color.Lerp(new Color(0.00f,0.00f,0.02f), new Color(0.00f,0.10f,0.18f), x/0.08f);
            if(x<0.35f) return Color.Lerp(new Color(0.00f,0.10f,0.18f), new Color(0.03f,0.85f,1f), (x-0.08f)/0.27f);
            if(x<0.55f) return Color.Lerp(new Color(0.03f,0.85f,1f), Color.white, (x-0.35f)/0.20f);
            if(x<0.80f) return Color.Lerp(Color.white, new Color(0.95f,0.45f,1f), (x-0.55f)/0.25f);
            return Color.Lerp(new Color(0.95f,0.45f,1f), new Color(0.10f,0.00f,0.12f), (x-0.80f)/0.20f);
          });
          edgeTint = new Color(0.95f,0.45f,1f,1f);
          min=0.24f; max=0.68f; exposure=5.6f; gamma=1.05f; edge=0.65f; edgeThr=0.005f; edgeW=0.02f;
          break;
        case 1: // Cobalt Porcelain
          lut = MakeLUT(256, x=>{
            if(x<0.10f) return Color.Lerp(new Color(0.02f,0.02f,0.06f), new Color(0.02f,0.06f,0.18f), x/0.10f);
            if(x<0.55f) return Color.Lerp(new Color(0.02f,0.06f,0.18f), new Color(0.12f,0.45f,1.0f), (x-0.10f)/0.45f);
            return Color.Lerp(new Color(0.12f,0.45f,1.0f), new Color(0.98f,0.98f,1.0f), (x-0.55f)/0.45f);
          });
          edgeTint = new Color(0.90f,0.95f,1f,1f);
          min=0.22f; max=0.72f; exposure=5.2f; gamma=1.05f; edge=0.55f; edgeThr=0.0045f; edgeW=0.02f;
          break;
        case 2: // Fire & Ice
          lut = MakeLUT(256, x=>{
            if(x<0.35f) return Color.Lerp(new Color(0.02f,0.10f,0.12f), new Color(0.10f,0.95f,0.95f), x/0.35f);
            if(x<0.65f) return Color.Lerp(new Color(0.10f,0.95f,0.95f), Color.white, (x-0.35f)/0.30f);
            return Color.Lerp(Color.white, new Color(1.0f,0.25f,0.65f), (x-0.65f)/0.35f);
          });
          edgeTint = new Color(1.0f,0.25f,0.65f,1f);
          min=0.26f; max=0.70f; exposure=5.4f; gamma=1.05f; edge=0.62f; edgeThr=0.0055f; edgeW=0.022f;
          break;
      }
      if(mat){ mat.SetTexture(idLUT, lut); mat.SetColor(idTint, edgeTint); }
      PushParams();
    }

    Texture2D MakeLUT(int n, System.Func<float,Color> f){
      var t=new Texture2D(n,1,TextureFormat.RGBA32,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
      for(int i=0;i<n;i++){ t.SetPixel(i,0,f(i/(n-1f))); } t.Apply(false,false); return t;
    }
  }
}
