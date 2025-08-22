namespace LeniaBeauty {
  using UnityEngine; using UnityEngine.UI; using System.Linq;
  public class BeautyViewSimple : MonoBehaviour {
    Material mat; RawImage ri;
    void Start() {
      // 1) Find an existing RawImage that already shows the sim
      var allRI = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      RawImage srcRI = allRI.Where(x => x && x.texture).OrderByDescending(x => x.texture.width * x.texture.height).FirstOrDefault();
      if(srcRI==null){ Debug.LogWarning("[Beauty] No source RawImage with a texture found."); return; }
      var srcTex = srcRI.texture;
      // 2) Create our overlay canvas + RawImage
      var cv = new GameObject("BeautyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c  = cv.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 9999;
      var go = new GameObject("BeautyRawImage", typeof(RawImage)); go.transform.SetParent(cv.transform, false);
      ri = go.GetComponent<RawImage>(); var rt = ri.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
      // 3) Build neon LUT and material
      var sh = Shader.Find("Hidden/Lenia/TextureLUT"); if(sh==null){ Debug.LogError("[Beauty] Shader not found."); return; }
      mat = new Material(sh);
      ri.texture = srcTex; ri.material = mat;
      var lut = BuildNeonLUT(256); mat.SetTexture("_LUT", lut);
      mat.SetFloat("_Exposure", 6f); mat.SetFloat("_Gamma", 1.05f); mat.SetFloat("_Edge", 0.45f); mat.SetFloat("_Thresh", 0.008f);
      // 4) Nice camera defaults
      var cam = Camera.main; if(cam){ cam.allowHDR = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.01f,0.02f,0.04f,1f); }
      Debug.Log($"[Beauty] Hooked source texture {srcTex.width}x{srcTex.height} from '{srcRI.gameObject.name}'");
    }
    Texture2D BuildNeonLUT(int steps){
      var tex = new Texture2D(steps,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int i=0;i<steps;i++){ float t=i/(float)(steps-1); Color c;
        if(t<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), t/0.05f);
        else if(t<0.30f) c=Color.Lerp(new Color(0,0.08f,0.11f,1), new Color(0.04f,0.91f,1f,1), (t-0.05f)/0.25f);
        else if(t<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1f,1), Color.white, (t-0.30f)/0.25f);
        else if(t<0.80f) c=Color.Lerp(Color.white, new Color(1,0.4f,1,1), (t-0.55f)/0.25f);
        else c=Color.Lerp(new Color(1,0.4f,1,1), new Color(0.16f,0,0.16f,1), (t-0.80f)/0.20f);
        tex.SetPixel(i,0,c);
      }
      tex.Apply(false,false); return tex;
    }
  }
}
