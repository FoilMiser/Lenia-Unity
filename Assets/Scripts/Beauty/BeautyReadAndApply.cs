using UnityEngine; using UnityEngine.UI; using System.Text; using System.Linq;
namespace LeniaBeauty {
  public class BeautyReadAndApply : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){ var go=new GameObject("BeautyBoot"); DontDestroyOnLoad(go); go.AddComponent<BeautyReadAndApply>(); }
    void Start(){ Invoke(nameof(Apply), 0.1f); Invoke(nameof(Apply), 0.6f); }
    void Apply(){
      // Pick the Lenia RawImage by name or just the largest with a material
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      RawImage ri = ris.FirstOrDefault(r=>r && r.gameObject.name=="LeniaView") ?? ris.OrderByDescending(r=> r&&r.material&&r.material.shader ? (r.material.mainTexture? r.material.mainTexture.width*r.material.mainTexture.height:0) : 0).FirstOrDefault();
      if(!ri || ri.material==null || ri.material.shader==null){ Debug.LogWarning("[Beauty/Probe] No RawImage with a material shader found."); return; }
      var mat = ri.material; var sh = mat.shader;
      var sb = new StringBuilder(); sb.Append($"[Beauty/Probe] Shader='{sh.name}' props=");
      int pc = sh.GetPropertyCount(); int setLUT=0, setFloats=0;
      // Build a neon 1D LUT
      var lut = BuildNeonLUT(512);

      for(int i=0;i<pc;i++){
        var type = sh.GetPropertyType(i);
        string name = sh.GetPropertyName(i);
        sb.Append($"[{i}:{type}:{name}]");
        if(type == UnityEngine.Rendering.ShaderPropertyType.Texture){
          string lower = name.ToLowerInvariant();
          // Heuristic: if it looks like palette/LUT/grad, assign our LUT
          if(lower.Contains("palette") || lower.Contains("lut") || lower.Contains("grad")){
            mat.SetTexture(sh.GetPropertyNameId(i), lut); setLUT++;
          }
        } else if(type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range){
          string lower = name.ToLowerInvariant();
          if(lower.Contains("exposure")) { mat.SetFloat(sh.GetPropertyNameId(i), 5.8f); setFloats++; }
          else if(lower.Contains("gamma")) { mat.SetFloat(sh.GetPropertyNameId(i), 1.05f); setFloats++; }
          else if(lower.Contains("palettescale")) { mat.SetFloat(sh.GetPropertyNameId(i), 0.86f); setFloats++; }
          else if(lower.Contains("edgestrength") || lower == "_edge") { mat.SetFloat(sh.GetPropertyNameId(i), 0.45f); setFloats++; }
          else if(lower.Contains("edgethreshold") || lower.Contains("edge_thresh")) { mat.SetFloat(sh.GetPropertyNameId(i), 0.008f); setFloats++; }
          else if(lower=="useglow" || lower=="usetrail"){ mat.SetFloat(sh.GetPropertyNameId(i), 0f); setFloats++; }
        }
      }

      // Make sure the RawImage actually shows something (UI/Default path, no magenta)
      if(ri.texture==null){
        // Prefer any non-LUT texture already on the material
        Texture best=null; int bestPx=0;
        for(int i=0;i<pc;i++){
          if(sh.GetPropertyType(i)!=UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
          string nm=sh.GetPropertyName(i).ToLowerInvariant();
          if(nm.Contains("lut")||nm.Contains("palette")||nm.Contains("grad")) continue;
          var tex = mat.GetTexture(sh.GetPropertyNameId(i));
          if(tex!=null){ int px = tex.width*tex.height; if(px>bestPx){ best=tex; bestPx=px; } }
        }
        if(best!=null){ ri.texture=best; ri.material=mat; }
      }

      Debug.Log($"{sb} | setLUT={setLUT}, setFloats={setFloats}");
    }

    Texture2D BuildNeonLUT(int n){
      var t=new Texture2D(n,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
      for(int i=0;i<n;i++){ float x=i/(float)(n-1); Color c;
        if(x<0.05f) c=Color.Lerp(new Color(0,0,0.02f,1), new Color(0,0.08f,0.11f,1), x/0.05f);
        else if(x<0.30f) c=Color.Lerp(new Color(0,0.08f,0.11f,1), new Color(0.04f,0.91f,1,1), (x-0.05f)/0.25f);
        else if(x<0.55f) c=Color.Lerp(new Color(0.04f,0.91f,1,1), Color.white, (x-0.30f)/0.25f);
        else if(x<0.80f) c=Color.Lerp(Color.white, new Color(1,0.4f,1,1), (x-0.55f)/0.25f);
        else c=Color.Lerp(new Color(1,0.4f,1,1), new Color(0.16f,0,0.16f,1), (x-0.80f)/0.20f);
        t.SetPixel(i,0,c);
      }
      t.Apply(false,false); return t;
    }
  }
}
