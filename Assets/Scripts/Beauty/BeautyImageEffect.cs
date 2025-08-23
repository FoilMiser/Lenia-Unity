using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace LeniaBeauty {
  // Drops a full-screen pass on the main camera. No UI. No Input API. No interference.
  [ExecuteAlways, RequireComponent(typeof(Camera))]
  public class BeautyImageEffect : MonoBehaviour {
    [Header("Glow")]
    [Range(0f,1f)] public float glowStrength = 0.35f;
    [Range(0.5f,4f)] public float glowRadius = 1.6f;   // 1.0=1px, 2.0=2px-ish

    [Header("Palette Select (0=Neon,1=Cobalt,2=Fire&Ice)")]
    [Range(0,2)] public int palette = 0;

    Material palMat, blurMat;
    Texture leniaTex;            // simulation texture
    RenderTexture tmpA, tmpB;    // working buffers
    Texture2D lutNeon,lutCobalt,lutFireIce;

    int PID_Lenia = Shader.PropertyToID("_LeniaTex");
    int PID_LUT   = Shader.PropertyToID("_LUT");
    int PID_Glow  = Shader.PropertyToID("_GlowStrength");
    int PID_Base  = Shader.PropertyToID("_BaseTex");
    int PID_GTex  = Shader.PropertyToID("_GlowTex");
    int PID_BlurDir=Shader.PropertyToID("_BlurDir");
    int PID_White = Shader.PropertyToID("_WhitePoint");
    int PID_Hi    = Shader.PropertyToID("_HighlightClamp");

    // Auto-install at startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
      var cam = Camera.main ?? Object.FindObjectsOfType<Camera>().FirstOrDefault();
      if (!cam) return;
      if (!cam.GetComponent<BeautyImageEffect>()) cam.gameObject.AddComponent<BeautyImageEffect>();
    }

    void OnEnable(){
      // Kill old canvases that fight the output
      var old = GameObject.FindObjectsOfType<Canvas>(true)
        .Where(c=> c && (c.name.ToLower().Contains("leniaviewcanvas") || c.name.ToLower().Contains("beautycanvas")))
        .ToArray();
      foreach(var c in old) c.gameObject.SetActive(false);

      var palSh = Shader.Find("Lenia/BeautyFinal");
      var blurSh= Shader.Find("Lenia/BeautyBlur");
      if(!palSh || !blurSh) { enabled=false; Debug.LogError("[Beauty] Shaders missing."); return; }
      palMat = new Material(palSh); blurMat = new Material(blurSh);

      BuildPalettes();
    }

    void OnDisable(){
      ReleaseRT(ref tmpA); ReleaseRT(ref tmpB);
      if(palMat) DestroyImmediate(palMat);
      if(blurMat) DestroyImmediate(blurMat);
    }

    void ReleaseRT(ref RenderTexture rt){ if(rt){ rt.Release(); DestroyImmediate(rt); rt=null; } }

    void BuildPalettes(){
      lutNeon    = BuildLUT(x=>{
        if(x<0.10f) return new Color(0.00f,0.10f,0.16f);
        if(x<0.35f) return Color.Lerp(new Color(0.00f,0.10f,0.16f), new Color(0.06f,0.90f,1.00f), (x-0.10f)/0.25f);
        if(x<0.55f) return Color.Lerp(new Color(0.06f,0.90f,1.00f), new Color(0.90f,0.95f,1.00f), (x-0.35f)/0.20f);
        if(x<0.80f) return Color.Lerp(new Color(0.90f,0.95f,1.00f), new Color(0.95f,0.45f,1.00f), (x-0.55f)/0.25f);
        return Color.Lerp(new Color(0.95f,0.45f,1.00f), new Color(0.12f,0.00f,0.14f), (x-0.80f)/0.20f);
      });
      lutCobalt  = BuildLUT(x=>{
        if(x<0.12f) return new Color(0.02f,0.03f,0.07f);
        if(x<0.55f) return Color.Lerp(new Color(0.03f,0.06f,0.15f), new Color(0.12f,0.46f,1.00f), (x-0.12f)/0.43f);
        return Color.Lerp(new Color(0.12f,0.46f,1.00f), new Color(0.82f,0.90f,1.00f), (x-0.55f)/0.45f);
      });
      lutFireIce = BuildLUT(x=>{
        if(x<0.30f) return new Color(0.06f,0.85f,0.85f);
        if(x<0.62f) return Color.Lerp(new Color(0.06f,0.85f,0.85f), new Color(0.86f,0.94f,1.00f), (x-0.30f)/0.32f);
        return Color.Lerp(new Color(0.86f,0.94f,1.00f), new Color(1.00f,0.42f,0.66f), (x-0.62f)/0.38f);
      });
    }

    Texture2D BuildLUT(System.Func<float,Color> f){
      int n=1024; var t=new Texture2D(n,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      var px=new Color[n]; for(int i=0;i<n;i++){ var c=f(i/(n-1f)); float cap=0.88f; c.r=Mathf.Min(c.r,cap); c.g=Mathf.Min(c.g,cap); c.b=Mathf.Min(c.b,cap); px[i]=c; }
      t.SetPixels(px); t.Apply(false,false); return t;
    }

    Texture PickLUT(){
      switch(Mathf.Clamp(palette,0,2)){
        default: return lutNeon;
        case 1:  return lutCobalt;
        case 2:  return lutFireIce;
      }
    }

    Texture TryFindLeniaTexture(){
      // 1) RawImage named LeniaView (usual project)
      var ri = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
               .FirstOrDefault(r=> r && r.gameObject.name=="LeniaView" && (r.texture || (r.material && r.material.mainTexture)));
      if(ri) return ri.texture ? ri.texture : ri.material.mainTexture;

      // 2) Any RawImage that clearly shows the sim (has a big RenderTexture)
      var cand = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                 .Select(r=> r ? (r.texture ? r.texture : (r.material? r.material.mainTexture : null)) : null)
                 .FirstOrDefault(t=> t is RenderTexture rt && rt.width>=512 && rt.height>=512);
      if(cand) return cand;

      // 3) Fallback: any material using "Lenia" shader
      var rends = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach(var r in rends) foreach(var m in r.sharedMaterials){
        if(m && m.shader && m.shader.name.ToLower().Contains("lenia")){
          var t = m.mainTexture; if(t) return t;
        }
      }
      return null;
    }

    void EnsureBuffers(int w, int h){
      int bw=Mathf.Max(2, w/2), bh=Mathf.Max(2, h/2);
      if(!tmpA || tmpA.width!=w || tmpA.height!=h){ ReleaseRT(ref tmpA); tmpA = new RenderTexture(w,h,0,RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear){ filterMode=FilterMode.Bilinear }; tmpA.Create(); }
      if(!tmpB || tmpB.width!=bw || tmpB.height!=bh){ ReleaseRT(ref tmpB); tmpB = new RenderTexture(bw,bh,0,RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear){ filterMode=FilterMode.Bilinear }; tmpB.Create(); }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst){
      if(leniaTex==null) leniaTex = TryFindLeniaTexture();
      if(leniaTex==null || palMat==null || blurMat==null){ Graphics.Blit(src,dst); return; }

      int w = (leniaTex is RenderTexture rt)? rt.width : src.width;
      int h = (leniaTex is RenderTexture rt2)? rt2.height: src.height;
      EnsureBuffers(w,h);

      // 0) palette pass into full-res tmpA
      palMat.SetTexture(PID_Lenia, leniaTex);
      palMat.SetTexture(PID_LUT, PickLUT());
      palMat.SetFloat(PID_White, 0.88f);
      palMat.SetFloat(PID_Hi,    0.86f);
      Graphics.Blit(leniaTex, tmpA, palMat, 0);   // pass 0 = palette+edges

      // 1) downsample then blur (horizontal+vertical) into tmpB
      Graphics.Blit(tmpA, tmpB);                  // downsample
      blurMat.SetVector(PID_BlurDir, new Vector2(glowRadius,0));
      Graphics.Blit(tmpB, tmpB, blurMat, 0);
      blurMat.SetVector(PID_BlurDir, new Vector2(0,glowRadius));
      Graphics.Blit(tmpB, tmpB, blurMat, 0);

      // 2) composite base + glow to screen
      palMat.SetTexture(PID_Base, tmpA);
      palMat.SetTexture(PID_GTex, tmpB);
      palMat.SetFloat(PID_Glow, glowStrength);
      Graphics.Blit(tmpA, dst, palMat, 1);        // pass 1 = composite
    }
  }
}
