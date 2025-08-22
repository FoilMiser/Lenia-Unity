using UnityEngine; using UnityEngine.UI; using System; using System.Linq; using System.Reflection;
using UnityEngine.Rendering; // Volume core APIs (no URP hard ref)
namespace LeniaBeauty {
  public class BeautyPresetNeon : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply() {
      // 0) Kill any stray experimental canvases from earlier attempts
      var bc = GameObject.Find("BeautyCanvas"); if (bc) Destroy(bc);

      // 1) Find the main Lenia view MB (by type or name) and swap its palette LUT
      var mb = FindLeniaView();
      if (mb == null) { Debug.LogWarning("[Beauty] Could not find Lenia view component"); }
      else {
        int set = TrySetPaletteLUT(mb, BuildNeonLUT(512));
        int toggles = TrySetCommonToggles(mb);
        TrySetNamed(mb, "DispExposure", 5.8f);
        TrySetNamed(mb, "DispGamma", 1.05f);
        TrySetNamed(mb, "PaletteScale", 0.86f);
        TryInvokeAny(mb, "Rebuild", "Refresh", "OnValidate", "Apply", "ApplySettings");
        Debug.Log($"[Beauty] Neon palette applied: lutSet={set}, toggles={toggles}");
      }

      // 2) Ensure any RawImage actually shows the sim with UI/Default (never magenta)
      ForceUIDisplayForAllRawImages();

      // 3) Add URP Bloom + ACES Tonemapping via reflection (no compile-time URP dependency)
      TryConfigureURPPostFX(0.55f /*bloomIntensity*/, 0.9f /*threshold*/, "ACES");
    }

    // ---------- Helpers ----------
    static MonoBehaviour FindLeniaView() {
      var all = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      // Prefer something on "LeniaView" object, otherwise by type name heuristic
      foreach (var go in new[] { GameObject.Find("LeniaView"), GameObject.Find("LeniaViewCanvas") }) {
        if (!go) continue;
        var mb = go.GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(m => m && m.GetType().Name.ToLowerInvariant().Contains("leniaview"));
        if (mb) return mb;
      }
      return all.FirstOrDefault(m => m && m.GetType().Name.ToLowerInvariant().Contains("leniaview"));
    }

    static int TrySetPaletteLUT(MonoBehaviour target, Texture2D lut) {
      int set=0; var tp = target.GetType();
      // Find Texture2D property/field likely to be the palette
      // Heuristic: name contains palette/lut/grad AND height <= 4
      foreach (var p in tp.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)) {
        if (!p.CanWrite || !typeof(Texture).IsAssignableFrom(p.PropertyType)) continue;
        var n = p.Name.ToLowerInvariant();
        if (!(n.Contains("palette")||n.Contains("lut")||n.Contains("grad"))) continue;
        try { p.SetValue(target, lut); set++; } catch {}
      }
      foreach (var f in tp.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)) {
        if (!typeof(Texture).IsAssignableFrom(f.FieldType)) continue;
        var n = f.Name.ToLowerInvariant();
        if (!(n.Contains("palette")||n.Contains("lut")||n.Contains("grad"))) continue;
        try { f.SetValue(target, lut); set++; } catch {}
      }
      return set;
    }

    static int TrySetCommonToggles(MonoBehaviour target) {
      int hits=0;
      hits+=TrySetNamed(target,"UseEdges",false)?1:0;
      hits+=TrySetNamed(target,"UseTrail",false)?1:0;
      hits+=TrySetNamed(target,"UseGlow",false)?1:0; // optional: disable legacy glow; Bloom will replace it
      hits+=TrySetNamed(target,"EdgeStrength",0.45f)?1:0;
      hits+=TrySetNamed(target,"EdgeThreshold",0.008f)?1:0;
      return hits;
    }
    static bool TrySetNamed(MonoBehaviour target, string name, object val) {
      var tp=target.GetType();
      var f=tp.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if (f!=null) { try { f.SetValue(target, Convert.ChangeType(val, f.FieldType)); return true; } catch {} }
      var p=tp.GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      if (p!=null && p.CanWrite) { try { p.SetValue(target, Convert.ChangeType(val, p.PropertyType)); return true; } catch {} }
      return false;
    }
    static void TryInvokeAny(MonoBehaviour target, params string[] names) {
      var tp=target.GetType();
      foreach (var n in names) {
        var m=tp.GetMethod(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if (m!=null && m.GetParameters().Length==0) { try { m.Invoke(target,null); } catch {} }
      }
    }

    static void ForceUIDisplayForAllRawImages() {
      var ris = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      int fixedCnt=0;
      foreach (var ri in ris) {
        if (!ri) continue;
        Texture src = ri.texture;
        if (src==null && ri.material) {
          if (ri.material.mainTexture) src = ri.material.mainTexture;
          if (src==null && ri.material.shader) {
            var sh=ri.material.shader; int pc=sh.GetPropertyCount();
            for(int i=0;i<pc;i++){
              if (sh.GetPropertyType(i)!=UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
              var nm=sh.GetPropertyName(i).ToLowerInvariant();
              if (nm.Contains("lut")||nm.Contains("palette")||nm.Contains("grad")) continue;
              var tt=ri.material.GetTexture(sh.GetPropertyNameId(i)); if (tt!=null) { src=tt; break; }
            }
          }
        }
        if (src!=null) { ri.texture=src; ri.material=null; fixedCnt++; }
      }
      Debug.Log("[Beauty] RawImages normalized to UI/Default: "+fixedCnt);
    }

    static void TryConfigureURPPostFX(float bloomIntensity, float threshold, string toneMap = "ACES") {
      // Find or create a Global Volume
      GameObject gv = GameObject.Find("Global Volume"); if (!gv) gv = new GameObject("Global Volume");
      var vol = gv.GetComponent<Volume>(); if (!vol) vol = gv.AddComponent<Volume>(); vol.isGlobal = true;
      if (vol.profile==null) vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
      var profile = vol.profile;

      // Fetch URP component types by reflection (no hard ref)
      const string urpAsm = "Unity.RenderPipelines.Universal.Runtime";
      Type BloomT = Type.GetType("UnityEngine.Rendering.Universal.Bloom, "+urpAsm);
      Type ToneT  = Type.GetType("UnityEngine.Rendering.Universal.Tonemapping, "+urpAsm);
      Type ToneModeT = Type.GetType("UnityEngine.Rendering.Universal.TonemappingMode, "+urpAsm);
      if (BloomT!=null) {
        var bloom = profile.components.FirstOrDefault(c=>c && c.GetType()==BloomT) ?? profile.Add(BloomT, true);
        SetFloatParam(bloom,"intensity", bloomIntensity);
        SetFloatParam(bloom,"threshold", threshold);
        SetFloatParam(bloom,"scatter", 0.7f);
      }
      if (ToneT!=null && ToneModeT!=null) {
        var tm = profile.components.FirstOrDefault(c=>c && c.GetType()==ToneT) ?? profile.Add(ToneT, true);
        var aces = Enum.Parse(ToneModeT, toneMap, true);
        SetEnumParam(tm,"mode", aces);
      }
    }
    static void SetFloatParam(object comp, string name, float v) {
      var tp = comp.GetType();
      var f = tp.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      var p = f!=null?null: tp.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      object param = f!=null? f.GetValue(comp) : p?.GetValue(comp);
      if (param==null) return;
      var valProp = param.GetType().GetProperty("value"); if (valProp!=null) try { valProp.SetValue(param, v); } catch {}
    }
    static void SetEnumParam(object comp, string name, object enumVal) {
      var tp = comp.GetType();
      var f = tp.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      var p = f!=null?null: tp.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
      object param = f!=null? f.GetValue(comp) : p?.GetValue(comp);
      if (param==null) return;
      var valProp = param.GetType().GetProperty("value"); if (valProp!=null) try { valProp.SetValue(param, enumVal); } catch {}
    }

    static Texture2D BuildNeonLUT(int steps){
      var tex=new Texture2D(steps,1,TextureFormat.RGBA32,false,true){ wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
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
