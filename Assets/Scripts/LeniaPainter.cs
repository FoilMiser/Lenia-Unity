using UnityEngine;

[DefaultExecutionOrder(80)]
public class LeniaPainter : MonoBehaviour
{
    public LeniaSimulator sim;
    [Range(0.002f, 0.2f)] public float brushRadiusUV = 0.03f;
    [Range(0f,1f)] public float hardness = 0.5f;
    [Range(0f,1f)] public float opacity  = 1.0f;

    Material brushMat;

    void Start(){
        if(!sim) sim = FindObjectOfType<LeniaSimulator>();
        var sh = Shader.Find("Hidden/BrushBlit");
        if(sh) brushMat = new Material(sh);
    }

    void Update(){
        if(!sim || brushMat==null || sim.EnvTex==null) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if(!shift) return;

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector2 uv = new Vector2(Input.mousePosition.x/(float)Screen.width,
                                     Input.mousePosition.y/(float)Screen.height);
            float target = Input.GetMouseButton(0) ? 0f : 1f; // LMB=wall, RMB=open
            PaintAtUV(uv, target);
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore)) brushRadiusUV = Mathf.Max(0.002f, brushRadiusUV*0.8f);
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))      brushRadiusUV = Mathf.Min(0.2f,   brushRadiusUV*1.25f);
    }

    void PaintAtUV(Vector2 uv, float targetValue)
    {
        var env = sim.EnvTex;
        var tmp = RenderTexture.GetTemporary(env.descriptor);
        Graphics.Blit(env, tmp);

        brushMat.SetTexture("_MainTex", tmp);
        brushMat.SetVector("_BrushCenter", new Vector4(uv.x, uv.y, 0, 0));
        brushMat.SetFloat("_BrushRadius", brushRadiusUV);
        brushMat.SetFloat("_Hardness", hardness);
        brushMat.SetFloat("_Opacity", opacity);
        brushMat.SetFloat("_TargetValue", targetValue);

        Graphics.Blit(tmp, env, brushMat);
        RenderTexture.ReleaseTemporary(tmp);
    }
}
