using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AssignCheckerNow : MonoBehaviour
{
    public RawImage target;
    public int size = 64;
    Texture2D tex;

    void OnEnable(){ EnsureTarget(); EnsureTex(); Bind(); }
    void OnDisable(){
        if (tex){
            if (Application.isPlaying) Destroy(tex);
            else DestroyImmediate(tex);
            tex = null;
        }
    }

    void EnsureTarget(){ if (!target) target = GetComponent<RawImage>(); }

    void EnsureTex(){
        if (tex) return;
        int N = Mathf.Max(4, size);
        tex = new Texture2D(N, N, TextureFormat.RGBA32, false, true);
        for (int y=0;y<N;y++)
        for (int x=0;x<N;x++){
            bool a = (((x/8)+(y/8)) & 1) == 0;
            tex.SetPixel(x,y, a ? new Color(0.15f,0.15f,0.15f,1f) : new Color(0.85f,0.85f,0.85f,1f));
        }
        tex.Apply(false);
        tex.name = "AssignCheckerNow";
    }

    void Bind(){
        if (!target || !tex) return;

        // If playing and a RenderTexture is already assigned, stop overriding it.
        if (Application.isPlaying && target.texture is RenderTexture){
            enabled = false; // stand down
            return;
        }

        // Only (re)apply the checker when no texture or the checker itself is there.
        if (target.texture == null || target.texture == tex)
            target.texture = tex;
    }

    void Update(){ EnsureTarget(); EnsureTex(); Bind(); }
}
