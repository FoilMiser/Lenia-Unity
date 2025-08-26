using UnityEngine;

[ExecuteAlways]
public class SimCpuDebugWriter : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    Texture2D src;
    float t;

    void OnEnable(){ if (!sim) sim = FindAnyObjectByType<FlowLeniaSimulation>(); EnsureTex(); }
    void OnDisable(){ if (src){ if (Application.isPlaying) Destroy(src); else DestroyImmediate(src); src=null; } }

    void EnsureTex(){
        if (src) return;
        src = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
        src.wrapMode = TextureWrapMode.Clamp;
        src.filterMode = FilterMode.Point;
    }

    void Update(){
        if (!sim) sim = FindAnyObjectByType<FlowLeniaSimulation>();
        if (!sim) return;

        sim.EnsureInitialized();
        var rt = sim.CurrentTexture;
        if (!rt) return;

        // animate a simple gradient to prove we can write into the RT
        EnsureTex();
        t += Time.unscaledDeltaTime;
        for (int y=0; y<src.height; y++)
        for (int x=0; x<src.width; x++){
            float u = (x + t*60f) / src.width;
            float v = (y) / (float)src.height;
            src.SetPixel(x,y, new Color(u%1f, v, 0.5f, 1f));
        }
        src.Apply(false);

        Graphics.Blit(src, rt); // write into the sim RT
    }
}
