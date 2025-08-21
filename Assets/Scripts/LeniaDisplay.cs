using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class LeniaDisplay : MonoBehaviour
{
    public LeniaSimulation sim;
    Material _mat; Texture2D _ramp; RawImage _ri;

    void Awake()
    {
        _ri = GetComponent<RawImage>();
        _mat = new Material(Shader.Find("Unlit/StateToColor"));
        _ramp = BuildDefaultRamp();
        _mat.SetTexture("_RampTex", _ramp);
        _mat.SetFloat("_Exposure", 1f);
        _ri.material = _mat;
    }

    void Update()
    {
        if (sim && sim.CurrentState) _mat.SetTexture("_MainTex", sim.CurrentState);
    }

    Texture2D BuildDefaultRamp()
    {
        var t = new Texture2D(256, 1, TextureFormat.RGB24, false, true);
        for (int x=0;x<256;x++)
        {
            float u = x/255f;
            Color c = (u < 0.5f)
                ? Color.Lerp(new Color(0.05f,0.1f,0.4f), new Color(0.0f,0.9f,0.9f), u/0.5f)
                : Color.Lerp(new Color(0.0f,0.9f,0.9f), new Color(1.0f,0.95f,0.7f), (u-0.5f)/0.5f);
            t.SetPixel(x,0,c);
        }
        t.Apply(false); t.wrapMode = TextureWrapMode.Clamp; return t;
    }
}


