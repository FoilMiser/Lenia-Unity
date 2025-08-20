using UnityEngine;
[DefaultExecutionOrder(65)]
public class LeniaPalette : MonoBehaviour
{
    public LeniaDisplay display; public int palette = 0; public float exposure = 1f;
    Texture2D ramp; Material mat;

    void Start(){
        if(!display) display = FindFirstObjectByType<LeniaDisplay>();
        if(display) mat = display.GetComponent<UnityEngine.UI.RawImage>().material;
        Rebuild();
    }

    void Rebuild(){
        if(!mat) return;
        if(ramp==null) { ramp = new Texture2D(256,1,TextureFormat.RGB24,false,true); ramp.wrapMode = TextureWrapMode.Clamp; }
        for(int x=0;x<256;x++){ float u=x/255f; ramp.SetPixel(x,0, Eval(u)); }
        ramp.Apply(false,false);
        mat.SetTexture("_RampTex", ramp);
        mat.SetFloat("_Exposure", exposure);
    }

    Color Eval(float u){
        switch(palette){
            case 1: // DeepSea
                return Color.Lerp(new Color(0.01f,0.02f,0.08f), new Color(0.0f,0.85f,0.9f), Mathf.SmoothStep(0,1,u));
            case 2: // Magma
                return Color.Lerp(new Color(0.0f,0.0f,0.0f),  Color.Lerp(new Color(1.0f,0.5f,0.0f), new Color(1.0f,0.95f,0.8f), Mathf.SmoothStep(0,1,u)), Mathf.Pow(u,0.6f));
            default: // Teal-Cyan-Gold
                return (u<0.5f)
                    ? Color.Lerp(new Color(0.05f,0.1f,0.4f), new Color(0.0f,0.9f,0.9f), u/0.5f)
                    : Color.Lerp(new Color(0.0f,0.9f,0.9f), new Color(1.0f,0.95f,0.7f), (u-0.5f)/0.5f);
        }
    }

    void OnGUI(){
        if(GUI.Button(new Rect(8,140,120,26), "Palette A")){ palette=0; Rebuild(); }
        if(GUI.Button(new Rect(8,170,120,26), "Palette B")){ palette=1; Rebuild(); }
        if(GUI.Button(new Rect(8,200,120,26), "Palette C")){ palette=2; Rebuild(); }
        exposure = GUI.HorizontalSlider(new Rect(8,232,120,18), exposure, 0.5f, 2.5f); mat?.SetFloat("_Exposure", exposure);
    }
}


