using UnityEngine; using UnityEngine.UI; using System.Linq;

namespace LeniaBeauty {
  public class BeautyOverlayUI : MonoBehaviour {
    RawImage overlay; Material mat; Texture src;
    float min=0.26f, max=0.62f, exposure=5.0f, gamma=1.07f, edge=0.55f, edgeThr=0.005f, edgeW=0.02f, hiClamp=0.92f;
    int idLUT, idMin, idMax, idExp, idGam, idEdge, idThr, idW, idTint, idHi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyOverlayUI"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyOverlayUI>(); }

    void Start(){
      // 1) Find sim texture from existing Lenia RawImage
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      var source = ris.FirstOrDefault(r=> r && r.gameObject.name=="LeniaView") ?? ris.FirstOrDefault(r=> r && r.texture);
      if(source==null){ Debug.LogWarning("[Beauty] No RawImage source found."); return; }
      src = source.texture;
      if(src==null && source.material){
        var srcSh=source.material.shader; int pc = srcSh? srcSh.GetPropertyCount():0;
        for(int i=0;i<pc;i++){
          if(srcSh.GetPropertyType(i)!=UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
          string nm = srcSh.GetPropertyName(i).ToLowerInvariant();
          if(nm.Contains("palette")||nm.contains("lut")||nm.Contains("grad")||nm.Contains("trail")||nm.Contains("glow")) continue;
          var t=source.material.GetTexture(srcSh.GetPropertyNameId(i)); if(t){ src=t; break; }
        }
      }
      if(src==null){ Debug.LogWarning("[Beauty] Could not locate sim texture."); return; }

      // 2) Build overlay canvas on top
      var cv=new GameObject("BeautyCanvasV2", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c=cv.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=5000;
      var scaler=cv.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);

      overlay=new GameObject("BeautyRawImage", typeof(RawImage)).GetComponent<RawImage>(); overlay.transform.SetParent(cv.transform,false);
      var rt=overlay.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;

      // 3) Material using pretty shader
      var prettySh=Shader.Find("Hidden/Lenia/NeonPretty"); if(!prettySh){ Debug.LogError("[Beauty] Pretty shader not found."); return; }
      mat=new Material(prettySh); overlay.material=mat; overlay.texture=src; mat.mainTexture=src;

      idLUT=Shader.PropertyToID("_LUT"); idMin=Shader.PropertyToID("_Min"); idMax=Shader.PropertyToID("_Max");
      idExp=Shader.PropertyToID("_Exposure"); idGam=Shader.PropertyToID("_Gamma");
      idEdge=Shader.PropertyToID("_EdgeStrength"); idThr=Shader.PropertyToID("_EdgeThreshold"); idW=Shader.PropertyToID("_EdgeWidth");
      idTint=Shader.PropertyToID("_EdgeTint"); idHi=Shader.PropertyToID("_HighlightClamp");

      ApplyPalette(0); // Neon default
      PushParams();

      var old = GameObject.Find("LeniaViewCanvas"); if(old) old.SetActive(false);
      Debug.Log($"[Beauty] UI overlay active. Source {src.width}x{src.height}, shader={mat.shader.name}");
    }

    // -------- UI (IMGUI so we avoid Input System differences) --------
    void OnGUI(){
      if(!mat) return;
      float w=360, h=210; var r=new Rect(12, Screen.height-h-12, w, h);
      GUI.BeginGroup(r, GUI.skin.window);
      GUILayout.Label("Lenia Display — Palette & Tone");
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Neon", GUILayout.Width(110))) ApplyPalette(0);
      if(GUILayout.Button("Cobalt", GUILayout.Width(110))) ApplyPalette(1);
      if(GUILayout.Button("Fire & Ice", GUILayout.Width(110))) ApplyPalette(2);
      GUILayout.EndHorizontal();

      GUILayout.Space(6);
      min = SliderRow("Min", 0.00f, 0.50f, min);
      max = SliderRow("Max", 0.50f, 1.00f, max);
      if(max < min + 0.02f) max = min + 0.02f;

      exposure = SliderRow("Exposure", 2.0f, 7.0f, exposure);
      gamma    = SliderRow("Gamma",    0.85f, 1.30f, gamma);
      hiClamp  = SliderRow("Highlight",0.80f, 0.98f, hiClamp);
      GUILayout.EndVertical(); GUI.EndGroup();

      PushParams();
    }

    float SliderRow(string label, float a, float b, float v){
      GUILayout.BeginHorizontal();
      GUILayout.Label(label, GUILayout.Width(80));
      v = GUILayout.HorizontalSlider(v, a, b);
      GUILayout.Label(v.ToString("0.00"), GUILayout.Width(40));
      GUILayout.EndHorizontal();
      return v;
    }

    void PushParams(){
      mat.SetFloat(idMin, min); mat.SetFloat(idMax, max);
      mat.SetFloat(idExp, exposure); mat.SetFloat(idGam, gamma);
      mat.SetFloat(idEdge, edge); mat.SetFloat(idThr, edgeThr); mat.SetFloat(idW, edgeW);
      mat.SetFloat(idHi, hiClamp);
    }

    void ApplyPalette(int idx){
      Texture2D lut; Color edgeTint;
      switch(idx){
        default: // Neon Petri
          lut = MakeLUT(256, x=>{
            if(x<0.08f) return Color.Lerp(new Color(0.00f,0.00f,0.02f), new Color(0.00f,0.10f,0.18f), x/0.08f);
            if(x<0.35f) return Color.Lerp(new Color(0.00f,0.10f,0.18f), new Color(0.03f,0.85f,1f), (x-0.08f)/0.27f);
            if(x<0.55f) return Color.Lerp(new Color(0.03f,0.85f,1f), new Color(0.94f,0.97f,1f), (x-0.35f)/0.20f);
            if(x<0.80f) return Color.Lerp(new Color(0.94f,0.97f,1f), new Color(0.95f,0.45f,1f), (x-0.55f)/0.25f);
            return Color.Lerp(new Color(0.95f,0.45f,1f), new Color(0.10f,0.00f,0.12f), (x-0.80f)/0.20f);
          });
          edgeTint = new Color(0.95f,0.45f,1f,1f);
          min=0.26f; max=0.62f; exposure=5.0f; gamma=1.07f; hiClamp=0.92f;
          break;
        case 1: // Cobalt Porcelain
          lut = MakeLUT(256, x=>{
            if(x<0.10f) return Color.Lerp(new Color(0.02f,0.02f,0.06f), new Color(0.02f,0.06f,0.18f), x/0.10f);
            if(x<0.55f) return Color.Lerp(new Color(0.02f,0.06f,0.18f), new Color(0.12f,0.45f,1.0f), (x-0.10f)/0.45f);
            return Color.Lerp(new Color(0.12f,0.45f,1.0f), new Color(0.95f,0.97f,1.0f), (x-0.55f)/0.45f);
          });
          edgeTint = new Color(0.90f,0.95f,1f,1f);
          min=0.24f; max=0.66f; exposure=4.8f; gamma=1.08f; hiClamp=0.90f;
          break;
        case 2: // Fire & Ice
          lut = MakeLUT(256, x=>{
            if(x<0.35f) return Color.Lerp(new Color(0.02f,0.10f,0.12f), new Color(0.10f,0.95f,0.95f), x/0.35f);
            if(x<0.65f) return Color.Lerp(new Color(0.10f,0.95f,0.95f), new Color(0.96f,0.98f,1.0f), (x-0.35f)/0.30f);
            return Color.Lerp(new Color(0.96f,0.98f,1.0f), new Color(1.0f,0.25f,0.65f), (x-0.65f)/0.35f);
          });
          edgeTint = new Color(1.0f,0.25f,0.65f,1f);
          min=0.26f; max=0.68f; exposure=5.1f; gamma=1.06f; hiClamp=0.91f;
          break;
      }
      mat.SetTexture(idLUT, lut); mat.SetColor(idTint, edgeTint);
      PushParams();
    }

    Texture2D MakeLUT(int n, System.Func<float,Color> f){
      var t=new Texture2D(n,1,TextureFormat.RGBA32,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
      for(int i=0;i<n;i++){ t.SetPixel(i,0,f(i/(n-1f))); } t.Apply(false,false); return t;
    }
  }
}
