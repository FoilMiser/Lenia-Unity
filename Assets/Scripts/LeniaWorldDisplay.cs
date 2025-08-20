using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class LeniaWorldDisplay : MonoBehaviour
{
    public LeniaSimulator sim;
    public Camera targetCamera;
    public bool fitToCamera = true;
    public float exposure = 1.8f;

    private Material mat;
    private Texture2D ramp;

    void Awake(){
#if UNITY_2023_1_OR_NEWER
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
#else
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
#endif
        if (!targetCamera) targetCamera = Camera.main;
    }

    void Start(){
        var mr = GetComponent<MeshRenderer>();
        // Prefer existing assigned material
        if (mr.sharedMaterial != null) mat = mr.material;
        else {
            var sh = Shader.Find("Universal Render Pipeline/Unlit/StateToColorURP");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(sh);
            mr.material = mat;
        }

        ramp = BuildDefaultRamp();
        if (mat){
            mat.SetTexture("_RampTex", ramp);
            mat.SetFloat("_Exposure", exposure);
        }
    }

    void LateUpdate(){
        if (sim && mat && sim.CurrentState) mat.SetTexture("_MainTex", sim.CurrentState);
        if (fitToCamera && targetCamera) FitToCamera();
    }

    // Lock the quad to the camera plane (no tilt) and fill the view
    void FitToCamera(){
        if (!targetCamera) return;
        float d = Vector3.Dot(transform.position - targetCamera.transform.position, targetCamera.transform.forward);
        if (d < 0.5f) d = 3f;
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * d;
        transform.rotation = targetCamera.transform.rotation;
        float h = targetCamera.orthographic
            ? targetCamera.orthographicSize * 2f
            : 2f * d * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * targetCamera.aspect;
        transform.localScale = new Vector3(w, h, 1f);
    }

    Texture2D BuildDefaultRamp(){
        var t = new Texture2D(256, 1, TextureFormat.RGB24, false, true);
        for (int x = 0; x < 256; x++){
            float u = x / 255f;
            Color c = (u < 0.5f)
                ? Color.Lerp(new Color(0.05f, 0.10f, 0.40f), new Color(0.00f, 0.90f, 0.90f), u / 0.5f)
                : Color.Lerp(new Color(0.00f, 0.90f, 0.90f), new Color(1.00f, 0.95f, 0.70f), (u - 0.5f) / 0.5f);
            t.SetPixel(x, 0, c);
        }
        t.wrapMode = TextureWrapMode.Clamp;
        t.Apply(false, false);
        return t;
    }
}

