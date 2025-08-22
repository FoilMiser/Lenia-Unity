using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;

namespace LeniaBeauty {
  public class BeautyTakeover : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyTakeover"); Object.DontDestroyOnLoad(go); go.AddComponent<BeautyTakeover>(); }

    void Start(){
      // 1) Find the Lenia RawImage (Unlit/LeniaPalette) and grab its true sim texture
      var ri = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
               .FirstOrDefault(r => r && r.gameObject.name=="LeniaView" && r.material && r.material.shader && r.material.shader.name=="Unlit/LeniaPalette")
            ?? GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
               .FirstOrDefault(r => r && r.material && r.material.shader && r.material.shader.name=="Unlit/LeniaPalette");
      if(!ri || !ri.material){ Debug.LogWarning("[Beauty] LeniaView RawImage not found."); return; }

      Texture src = ri.texture;
      if(src==null){ var id = Shader.PropertyToID("_MainTex"); if(ri.material.HasProperty(id)) src = ri.material.GetTexture(id); }
      if(src==null){ Debug.LogWarning("[Beauty] Could not locate sim texture."); return; }

      // 2) Disable the LeniaView MonoBehaviour that re-writes the material
      var writers = ri.GetComponents<MonoBehaviour>().Where(m => m && m.GetType().Name.ToLowerInvariant().Contains("leniaview"));
      foreach(var w in writers){
        var beh = w as Behaviour;
        if(beh) beh.enabled = false; // non-destructive
        Debug.Log("[Beauty] Disabled writer: "+w.GetType().Name);
      }

      // 3) Create our material + LUT and assign
      var sh = Shader.Find("Hidden/Lenia/NeonLUTSimple");
      if(!sh){ Debug.LogError("[Beauty] Shader not found."); return; }
      var mat = new Material(sh);
      ri.material = mat;
      ri.texture = src;
      mat.mainTexture = src;

      var lut = BuildNeonLUT(256);
      int idL = Shader.PropertyToID("_LUT");
      if(mat.HasProperty(idL)) mat.SetTexture(idL, lut);
      mat.SetFloat("_Exposure", 6f);
      mat.SetFloat("_Gamma", 1.05f);
      mat.SetFloat("_Edge", 0.55f);
      mat.SetFloat("_Thresh", 0.006f);

      // 4) Camera background / HDR nice defaults
      var cam = Camera.main;
      if(cam){ cam.allowHDR = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.01f,0.02f,0.04f,1); }

      Debug.Log($"[Beauty] Takeover complete. Source={src.width}x{src.height}, material={ri.material.shader.name}");
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
