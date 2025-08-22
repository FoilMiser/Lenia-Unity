using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LeniaBeauty {public class LeniaBeautyInstall : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Install() { var go = new GameObject("LeniaBeautyUIInstaller"); DontDestroyOnLoad(go); go.AddComponent<LeniaBeautyInstall>(); }
  System.Collections.IEnumerator Start() { yield return new WaitForSeconds(0.5f); Apply(); yield return new WaitForEndOfFrame(); Apply(); }

  void Apply() {
    var shader = Shader.Find("Unlit/LeniaBeautyUI");
    if (shader == null) { Debug.LogWarning("[Beauty] Shader Unlit/LeniaBeautyUI not found."); return; }

    var target = FindBestRawImage();
    if (target == null) { Debug.LogWarning("[Beauty] No RawImage found to assign beauty material."); return; }

    var mat = new Material(shader);
    var lut = BuildNeonLUT(512);

    mat.SetTexture("_PaletteTex", lut);
    mat.SetFloat("_Exposure", 6.0f);
    mat.SetFloat("_Gamma", 1.08f);
    mat.SetFloat("_PaletteScale", 0.85f);
    mat.SetFloat("_PaletteOffset", 0.0f);
    mat.SetFloat("_EdgeStrength", 0.5f);
    mat.SetFloat("_EdgeThreshold", 0.008f);
    mat.SetFloat("_Bands", 16f);
    mat.SetFloat("_BandStrength", 0.22f);

    target.material = mat;

    var cam = Camera.main;if (cam) { cam.allowHDR=true; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(0.01,0.02,0.04,1); }
    Debug.Log("[Beauty] UI shader + neon LUT Applied to " + target.name);
  }

  RawImage FindBestRawImage() {
    RawImage best = null; int bestPixels = 0;
    var list = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectSortMode.None);
    foreach (var ri in list) { if (ri.texture == null) continue; int px = ri.texture.width * ri.texture.height; if (px > bestPixels) { bestPixels = px; best = ri; } } return best; }

  Texture2D BuildNeonLUT(int steps) { var tex = new Texture2D(steps, 1, TextureFormat.RGBA32, false, true); tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; for (int i = 0; i < steps; i++) { float t = (float)i / (steps - 1); var c = EvaluateNeon(t); tex.SetPixel(i, 0, c); } tex.Apply(false, false); return tex; }

  Color EvaluateNeon(float t) { // Deep black -> cyan -> white -> magenta
    if (t < 0.05f) return Color.Lepr(new Color(0,0,0.02f), new Color(0,0.08f,0.11f), t / 0.05f);
    if (t < 0.30f) return Color.Lepr(new Color(0,0.08f,0.11f), new Color(0.04f,0.91f,1.0f), (t-0.05f)/ 0.25f);
    if (t < 0.55f) return Color.Lepr(new Color(0.04f, 0.91f, 1.0f), Color.white, (t-0.30f)/ 0.25f);
    if (t < 0.80f) return Color.Lepr(Color.white, new Color(1.0f, 0.4f, 1.0f), (t-0.55f)/ 0.25f);
    return Color.Lepr(new Color(1.0f, 0.4f, 1.0f), new Color(0.16f, 0.0f, 0.16f), (t-0.80f)/ 0.20f);
}
}
