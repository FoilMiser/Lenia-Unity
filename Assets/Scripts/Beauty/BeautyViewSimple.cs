namespace LeniaBeauty {
  using UnityEngine; using UnityEngine.UI; using System.Collections; using System.Reflection;

  public class BeautyViewSimple : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { var go=new GameObject("BeautyBoot"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyViewSimple>(); }

    Material mat; RawImage ri;

    IEnumerator Start() {
      // Build overlay canvas + RawImage
      var cv=new GameObject("BeautyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      var c=cv.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=9999;
      var go=new GameObject("BeautyRawImage", typeof(RawImage)); go.transform.SetParent(cv.transform,false);
      ri=go.GetComponent<RawImage>(); var rt=ri.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;

      var sh=Shader.Find("Hidden/Lenia/TextureLUT"); if(sh==null){ Debug.LogError("[Beauty] Shader not found"); yield break; }
      mat=new Material(sh); ri.material=mat;
      mat.SetTexture("_LUT", BuildNeonLUT(256));
      mat.SetFloat("_Exposure",6f); mat.SetFloat("_Gamma",1.05f); mat.SetFloat("_Edge",0.45f); mat.SetFloat("_Thresh",0.008f);

      var cam=Camera.main; if(cam){ cam.allowHDR=true; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(0.01f,0.02f,0.04f,1f); }

      // Wait up to ~3 seconds for a source texture to appear, then bind to RawImage + material
      for(int t=0;t<180;t++){
        var tex = FindSourceTexture();
        if(tex!=null){
          ri.texture = tex;
          mat.SetTexture("_MainTex", tex);
          Debug.Log($"[Beauty] Attached source {tex.width}x{tex.height}");
          yield break;
        }
        yield return null;
      }
      Debug.LogWarning("[Beauty] Could not find source texture.");
    }

    Texture FindSourceTexture(){
      // Prefer an existing RawImage that already has the sim texture
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      RawImage best=null; int bestPx=0;
      foreach(var r in ris){ if(r && r.texture){ int px=r.texture.width*r.texture.height; if(px>bestPx){best=r; bestPx=px;} } }
      if(best) return best.texture;

      // Fallback: reflection on a component with property "CurrentTexture" (e.g., LeniaSimulation)
      var mbs = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach(var mb in mbs){
        var tp=mb.GetType();
        var p=tp.GetProperty("CurrentTexture", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(p!=null){ var tex=p.GetValue(mb) as Texture; if(tex) return tex; }
      }
      return null;
    }

    Texture2D BuildNeonLUT(int steps){
      var tex=new Texture2D(steps,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int i=0;i<steps;i++){
        float t=i/(float)(steps-1); Color c;
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
