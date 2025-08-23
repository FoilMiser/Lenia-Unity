using UnityEngine; using UnityEngine.UI; using System.Linq;

namespace LeniaBeauty {
  public class BeautyOverlayUX : MonoBehaviour {
    RawImage overlay; Material mat; Texture src;

    // display params (will be overwritten per-palette)
    float min=0.26f, max=0.62f, contrast=1.12f, exposure=4.6f, gamma=1.06f, hiClamp=0.88f, whitePoint=0.90f;
    float edge=0.55f, edgeThr=0.0045f, edgeW=0.020f, edgeFine=0.65f, edgeCoarse=0.25f, shade=0.06f, shadeDepth=3.0f;
    Color back = new Color(0.02f,0.04f,0.10f,1f), edgeTint = new Color(0.95f,0.45f,1f,1f);

    // shader ids
    int idLUT,idMin,idMax,idCtr,idExp,idGam,idHi,idBack,idEdge,idThr,idW,idEF,idEC,idTint,idShade,idShadeD,idWP;

    bool show=false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyOverlayUX"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyOverlayUX>(); }

    void Start(){
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      var source = ris.FirstOrDefault(r=> r && r.gameObject.name=="LeniaView") ?? ris.FirstOrDefault(r=> r && r.texture);
      if(source==null){ Debug.LogWarning("[Beauty] No RawImage source found."); return; }
      src = source.texture; if(!src && source.material){ src = source.material.mainTexture; }
      if(src==null){ Debug.LogWarning("[Beauty] Could not locate sim texture."); return; }

      var cv=new GameObject("BeautyCanvasV3", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c=cv.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=5000;
      var scaler=cv.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);

      overlay = new GameObject("BeautyRawImage", typeof(RawImage)).GetComponent<RawImage>(); overlay.transform.SetParent(cv.transform,false);
      var rt=overlay.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;

      var prettySh = Shader.Find("Hidden/Lenia/NeonPretty"); if(!prettySh){ Debug.LogError("[Beauty] Pretty shader not found."); return; }
      mat = new Material(prettySh); overlay.material = mat; overlay.texture = src; mat.mainTexture = src;

      idLUT=Shader.PropertyToID("_LUT"); idMin=Shader.PropertyToID("_Min"); idMax=Shader.PropertyToID("_Max"); idCtr=Shader.PropertyToID("_Contrast");
      idExp=Shader.PropertyToID("_Exposure"); idGam=Shader.PropertyToID("_Gamma"); idHi=Shader.PropertyToID("_HighlightClamp"); idBack=Shader.PropertyToID("_BackColor");
      idEdge=Shader.PropertyToID("_EdgeStrength"); idThr=Shader.PropertyToID("_EdgeThreshold"); idW=Shader.PropertyToID("_EdgeWidth");
      idEF=Shader.PropertyToID("_EdgeFineW"); idEC=Shader.PropertyToID("_EdgeCoarseW"); idTint=Shader.PropertyToID("_EdgeTint");
      idShade=Shader.PropertyToID("_ShadeAmt"); idShadeD=Shader.PropertyToID("_ShadeDepth"); idWP=Shader.PropertyToID("_WhitePoint");

      ApplyPalette(0);  // start on Neon so differences are obvious
      PushParams();

      var old = GameObject.Find("LeniaViewCanvas"); if(old) old.SetActive(false);
      Debug.Log($"[Beauty] Overlay UX active. Source {src.width}x{src.height}, shader={mat.shader.name}");
    }

    void PushParams(){
      if(!mat) return;
      mat.SetFloat(idMin,min); mat.SetFloat(idMax,max); mat.SetFloat(idCtr,contrast);
      mat.SetFloat(idExp,exposure); mat.SetFloat(idGam,gamma); mat.SetFloat(idHi,hiClamp); mat.SetFloat(idWP,whitePoint);
      mat.SetColor(idBack, back); mat.SetFloat(idEdge, edge); mat.SetFloat(idThr, edgeThr);
      mat.SetFloat(idW, edgeW); mat.SetFloat(idEF, edgeFine); mat.SetFloat(idEC, edgeCoarse);
      mat.SetColor(idTint, edgeTint); mat.SetFloat(idShade, shade); mat.SetFloat(idShadeD, shadeDepth);
    }

    Texture2D LUT1024(System.Func<float,Color> f){
      int n=1024; var t=new Texture2D(n,1,TextureFormat.RGBA32,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
      for(int i=0;i<n;i++){ t.SetPixel(i,0,f(i/(n-1f))); } t.Apply(false,false); return t;
    }

    // hard cap each channel so none of the palettes ever hit pure white
    Color Cap(Color c, float cap){ c.r=Mathf.Min(c.r,cap); c.g=Mathf.Min(c.g,cap); c.b=Mathf.Min(c.b,cap); return c; }

    void ApplyPalette(int idx){
      Texture2D lut;
      switch(idx){
        // -------- 0: NEON (teal -> porcelain-ish -> lilac), magenta edges
        default:
          lut = LUT1024(x=>{
            if(x<0.10f) return new Color(0.00f,0.10f,0.16f);
            if(x<0.35f) return Color.Lerp(new Color(0.00f,0.10f,0.16f), new Color(0.06f,0.90f,1.00f), (x-0.10f)/0.25f);
            if(x<0.55f) return Color.Lerp(new Color(0.06f,0.90f,1.00f), new Color(0.90f,0.95f,1.00f), (x-0.35f)/0.20f);
            if(x<0.80f) return Color.Lerp(new Color(0.90f,0.95f,1.00f), new Color(0.95f,0.45f,1.00f), (x-0.55f)/0.25f);
            return Color.Lerp(new Color(0.95f,0.45f,1.00f), new Color(0.12f,0.00f,0.14f), (x-0.80f)/0.20f);
          });
          edgeTint = new Color(0.95f,0.45f,1f,1f);
          min=0.26f; max=0.62f; contrast=1.12f; exposure=4.8f; gamma=1.06f; hiClamp=0.88f; whitePoint=0.90f;
          back = new Color(0.02f,0.04f,0.10f,1f); edge=0.58f; edgeFine=0.65f; edgeCoarse=0.25f;
          break;

        // -------- 1: COBALT (navy -> cobalt -> PALE BLUE, NO pink), cobalt edges
        case 1:
          lut = LUT1024(x=>{
            if(x<0.12f) return new Color(0.02f,0.03f,0.07f);
            if(x<0.55f) return Color.Lerp(new Color(0.03f,0.06f,0.15f), new Color(0.12f,0.46f,1.00f), (x-0.12f)/0.43f);
            return Color.Lerp(new Color(0.12f,0.46f,1.00f), new Color(0.82f,0.90f,1.00f), (x-0.55f)/0.45f);
          });
          edgeTint = new Color(0.72f,0.86f,1.0f,1f);     // light cobalt
          min=0.24f; max=0.66f; contrast=1.22f; exposure=4.3f; gamma=1.08f; hiClamp=0.86f; whitePoint=0.88f;
          back = new Color(0.02f,0.04f,0.10f,1f); edge=0.52f; edgeFine=0.55f; edgeCoarse=0.30f;
          break;

        // -------- 2: FIRE & ICE (teal -> porcelain -> ROSE), rosy edges (clearly different from cobalt)
        case 2:
          lut = LUT1024(x=>{
            if(x<0.30f) return new Color(0.06f,0.85f,0.85f);
            if(x<0.62f) return Color.Lerp(new Color(0.06f,0.85f,0.85f), new Color(0.86f,0.94f,1.00f), (x-0.30f)/0.32f);
            return Color.Lerp(new Color(0.86f,0.94f,1.00f), new Color(1.00f,0.42f,0.66f), (x-0.62f)/0.38f);
          });
          edgeTint = new Color(1.0f,0.42f,0.66f,1f);     // rosy
          min=0.26f; max=0.68f; contrast=1.10f; exposure=4.4f; gamma=1.05f; hiClamp=0.86f; whitePoint=0.88f;
          back = new Color(0.02f,0.04f,0.10f,1f); edge=0.62f; edgeFine=0.60f; edgeCoarse=0.30f;
          break;
      }
      // cap the LUT itself to avoid near-white source colors
      var px = lut.GetPixels();
      for(int i=0;i<px.Length;i++) px[i] = Cap(px[i], 0.88f);
      lut.SetPixels(px); lut.Apply(false,false);

      mat.SetTexture(idLUT, lut);
      PushParams();
    }

    void OnGUI(){
      if(mat==null) return;
      if(GUI.Button(new Rect(10,10,28,28), "⚙")) show = !show;
      if(!show) return;

      GUILayout.BeginArea(new Rect(12, 48, 360, 330), GUI.skin.window);
      GUILayout.Label("Lenia — Palette & Tone");
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Neon", GUILayout.Width(110)))   ApplyPalette(0);
      if(GUILayout.Button("Cobalt", GUILayout.Width(110))) ApplyPalette(1);
      if(GUILayout.Button("Fire & Ice", GUILayout.Width(110))) ApplyPalette(2);
      GUILayout.EndHorizontal();

      Row("Min", ref min, 0.00f, 0.50f); Row("Max", ref max, 0.50f, 1.00f); if(max < min + 0.02f) max = min + 0.02f;
      Row("Contrast", ref contrast, 0.80f, 1.50f); Row("Exposure", ref exposure, 2.0f, 6.0f);
      Row("Gamma", ref gamma, 0.85f, 1.30f); Row("Highlight", ref hiClamp, 0.80f, 0.94f); Row("WhitePoint", ref whitePoint, 0.82f, 0.92f);

      GUILayout.Space(6); GUILayout.Label("Edges");
      Row("Strength", ref edge, 0.0f, 1.0f); Row("Fine W", ref edgeFine, 0.0f, 1.0f);
      Row("Coarse W", ref edgeCoarse, 0.0f, 1.0f); Row("Threshold", ref edgeThr, 0.0025f, 0.0080f); Row("Width", ref edgeW, 0.010f, 0.040f);

      if(GUILayout.Button("Apply")) PushParams();
      GUILayout.EndArea();
    }
    void Row(string label, ref float v, float a, float b){ GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(88)); v = GUILayout.HorizontalSlider(v, a, b); GUILayout.Label(v.ToString("0.00"), GUILayout.Width(44)); GUILayout.EndHorizontal(); }
  }
}
