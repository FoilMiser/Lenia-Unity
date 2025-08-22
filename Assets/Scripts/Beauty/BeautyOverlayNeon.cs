using UnityEngine; using UnityEngine.UI; using System.Linq;
namespace LeniaBeauty {
  public class BeautyOverlayNeon : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyOverlayNeon"); DontDestroyOnLoad(go); go.AddComponent<BeautyOverlayNeon>(); }

    void Start(){
      // Find the existing Lenia RawImage that actually shows the sim
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      RawImage source = ris.FirstOrDefault(r=> r && r.gameObject.name=="LeniaView" && r.material && r.material.shader);
      if(source==null) source = ris.FirstOrDefault(r=> r && r.material && r.material.shader && r.material.shader.name.Contains("Lenia"));
      if(source==null){ Debug.LogWarning("[BeautyOverlay] No Lenia RawImage found."); return; }

      // Pull the true sim texture out of its material (skip palette/trail/glow)
      Texture src = source.texture;
      if(src==null && source.material && source.material.shader){
        var sh = source.material.shader; int pc = sh.GetPropertyCount(); int bestPx=0;
        for(int i=0;i<pc;i++){
          if(sh.GetPropertyType(i)!=UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
          string nm = sh.GetPropertyName(i).ToLowerInvariant();
          if(nm.Contains("palette")||nm.Contains("lut")||nm.Contains("grad")||nm.Contains("trail")||nm.Contains("glow")) continue;
          var tt = source.material.GetTexture(sh.GetPropertyNameId(i));
          if(tt!=null){ int px = (tt.width>0 && tt.height>0)? tt.width*tt.height:0; if(px>bestPx){bestPx=px; src=tt;} }
        }
      }
      if(src==null){ Debug.LogWarning("[BeautyOverlay] Could not find sim texture on Lenia material."); return; }

      // Build overlay canvas on top
      var cv = new GameObject("BeautyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c = cv.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 5000;
      var go = new GameObject("BeautyRawImage", typeof(RawImage)); go.transform.SetParent(cv.transform, false);
      var ri = go.GetComponent<RawImage>(); var rt = ri.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

      // Our tiny LUT shader (added as Hidden/Lenia/NeonLUTSimple)
      var shader = Shader.Find("Hidden/Lenia/NeonLUTSimple");
      if(shader==null){ Debug.LogError("[BeautyOverlay] Shader not found."); return; }
      var mat = new Material(shader);
      ri.material = mat; ri.texture = src; mat.mainTexture = src;

      // Generate a neon palette
      var lut = BuildNeonLUT(256);
      mat.SetTexture("_LUT", lut);
      mat.SetFloat("_Exposure", 6f);
      mat.SetFloat("_Gamma", 1.05f);
      mat.SetFloat("_Edge", 0.55f);
      mat.SetFloat("_Thresh", 0.006f);

      // Hide the old canvas so it can't overwrite visuals
      var oldCanvas = GameObject.Find("LeniaViewCanvas");
      if(oldCanvas) oldCanvas.SetActive(false);

      // Nice camera defaults
      var cam = Camera.main; if(cam){ cam.allowHDR=true; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(0.01f,0.02f,0.04f,1f); }

      Debug.Log($"[BeautyOverlay] Overlay active. Source {src.width}x{src.height}, shader={mat.shader.name}. Old canvas {(oldCanvas? "disabled":"not found")}.");
    }

    Texture2D BuildNeonLUT(int n){
      var tex = new Texture2D(n,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int i=0;i<n;i++){ float t=i/(float)(n-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1f,0.4f,1f,1), (t-0.55f)/0.25f);
        else            c=Color.Lerp(new Color(1f,0.4f,1f,1), new Color(0.16f,0f,0.16f,1), (t-0.80f)/0.20f);
        tex.SetPixel(i,0,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
