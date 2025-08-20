using UnityEngine;
[RequireComponent(typeof(MeshRenderer))]
public class LeniaWorldDisplay : MonoBehaviour
{
    public LeniaSimulator sim;
    public Camera targetCamera;
    public bool fitToCamera = true;
    public float exposure = 1.8f;

    Material mat; Texture2D ramp;

    void Start(){
        if(!sim) sim = FindObjectOfType<LeniaSimulator>();
        if(!targetCamera) targetCamera = Camera.main;

        var mr = GetComponent<MeshRenderer>();
        // Prefer the material already assigned on the MeshRenderer
        if (mr.sharedMaterial != null) mat = mr.material;
        else {
            var sh = Shader.Find("Universal Render Pipeline/Unlit/StateToColorURP");
            if (sh == null) { Debug.LogError("URP StateToColorURP shader not found"); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
            mat = new Material(sh); mr.material = mat;
        }

        ramp = BuildDefaultRamp();
        if (mat) { mat.SetTexture("_RampTex", ramp); mat.SetFloat("_Exposure", exposure); }
    }

    void LateUpdate(){
        if (sim && mat && sim.CurrentState) mat.SetTexture("_MainTex", sim.CurrentState);
        if (fitToCamera && targetCamera) FitToCamera();
    }

    void FitToCamera(){
        if(!targetCamera) return;
        // Face camera and fill view
        transform.LookAt(targetCamera.transform.position, Vector3.up);
        float z = Mathf.Abs(targetCamera.transform.InverseTransformPoint(transform.position).z);
        if (z < 0.01f) z = 1f;
        float h = targetCamera.orthographic ? targetCamera.orthographicSize*2f
                                            : 2f * z * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * targetCamera.aspect;
        transform.localScale = new Vector3(w, h, 1f);
    }

    Texture2D BuildDefaultRamp(){
        var t = new Texture2D(256,1,TextureFormat.RGB24,false,true);
        for (int x=0;x<256;x++){
            float u=x/255f;
            Color c = (u<0.5f)
                ? Color.Lerp(new Color(0.05f,0.1f,0.4f), new Color(0.0f,0.9f,0.9f), u/0.5f)
                : Color.Lerp(new Color(0.0f,0.9f,0.9f), new Color(1.0f,0.95f,0.7f), (u-0.5f)/0.5f);
            t.SetPixel(x,0,c);
        }
        t.wrapMode = TextureWrapMode.Clamp; t.Apply(false,false); return t;
    }
}
