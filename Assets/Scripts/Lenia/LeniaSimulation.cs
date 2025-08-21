using UnityEngine;
using UnityEngine.UI;

public class LeniaSimulation : MonoBehaviour
{
    [Header("View")]
    public RawImage view;               // optional; auto-created if null
    public Vector2Int resolution = new Vector2Int(512, 512);

    [Header("Rules")]
    public LeniaKernelProfile kernelProfile;
    public LeniaGrowthProfile growth;

    [Header("Runtime")]
    public ComputeShader leniaCS;
    [Range(0f,1f)] public float seedFill = 0.15f;
    public bool autoRun = true;
    public int stepsPerFrame = 1;

    RenderTexture _A, _B;
    int _kStep;

    void Awake()
    {
        if (leniaCS == null) Debug.LogError("Assign Lenia.compute to leniaCS");
        _kStep = leniaCS.FindKernel("Step");
        EnsureRTs();
        EnsureView();
        Reseed();
        ApplyProfiles();
    }

    void EnsureRTs()
    {
        CreateRT(ref _A);
        CreateRT(ref _B);
    }

    void CreateRT(ref RenderTexture rt)
    {
        if (rt != null && rt.IsCreated() && rt.width == resolution.x && rt.height == resolution.y) return;
        if (rt != null) rt.Release();
        rt = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        rt.Create();
    }

    void EnsureView()
    {
        if (view == null)
        {
            var go = GameObject.Find("LeniaViewCanvas") ?? new GameObject("LeniaViewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (go.GetComponent<Canvas>() is Canvas c) c.renderMode = RenderMode.ScreenSpaceOverlay;
            var imgGO = new GameObject("LeniaView", typeof(RawImage));
            imgGO.transform.SetParent(go.transform, false);
            view = imgGO.GetComponent<RawImage>();
            imgGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1024, 1024);
        }
        view.texture = _A;
    }

    public void ApplyProfiles()
    {
        var K = kernelProfile ? kernelProfile.GetOrBuildKernelTexture() : null;
        leniaCS.SetTexture(_kStep, "_KernelTex", K);
        leniaCS.SetInt("_KernelRadius", kernelProfile ? kernelProfile.radius : 24);
        leniaCS.SetFloat("_Mu", growth ? growth.mu : 0.15f);
        leniaCS.SetFloat("_Sigma", growth ? growth.sigma : 0.015f);
        leniaCS.SetFloat("_Dt", growth ? growth.dt : 0.1f);

        leniaCS.SetInts("_Resolution", new int[] { resolution.x, resolution.y });
        leniaCS.SetTexture(_kStep, "_StateIn", _A);
        leniaCS.SetTexture(_kStep, "_StateOut", _B);
    }

    public void Reseed(int? seed = null)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = _A;
        Texture2D tmp = new Texture2D(resolution.x, resolution.y, TextureFormat.RFloat, false, true);
        var rnd = new System.Random(seed ?? (int)System.DateTime.Now.Ticks);
        var data = new Color[resolution.x * resolution.y];
        for (int i = 0; i < data.Length; i++)
        {
            float v = (float)(rnd.NextDouble() < seedFill ? rnd.NextDouble() : 0.0);
            data[i] = new Color(v,0,0,0);
        }
        tmp.SetPixels(data);
        tmp.Apply();
        Graphics.Blit(tmp, _A);
        Destroy(tmp);
        RenderTexture.active = prev;
    }

    void Update()
    {
        if (!autoRun) return;
        for (int i = 0; i < stepsPerFrame; i++) Step();
    }

    public void Step()
    {
        leniaCS.Dispatch(_kStep, Mathf.CeilToInt(resolution.x / 8f), Mathf.CeilToInt(resolution.y / 8f), 1);
        // ping-pong
        var t = _A; _A = _B; _B = t;
        leniaCS.SetTexture(_kStep, "_StateIn", _A);
        leniaCS.SetTexture(_kStep, "_StateOut", _B);
        if (view) view.texture = _A;
    }
    public RenderTexture CurrentTexture => _A;
    // --- Compatibility shims for legacy UI scripts ---
    public void SetStepsPerFrame(int v){ stepsPerFrame = Mathf.Max(0, v); }
    public void SetAutoRun(bool run){ autoRun = run; }
    public void SetSeedFill(float v){ seedFill = Mathf.Clamp01(v); }
    public void SetResolution(UnityEngine.Vector2Int res){ if (res.x<8||res.y<8) return; resolution = res; EnsureRTs(); ApplyProfiles(); }
    public void SetResolutionWH(int w,int h){ SetResolution(new UnityEngine.Vector2Int(Mathf.Max(8,w), Mathf.Max(8,h))); }
    public void SetKernelProfile(LeniaKernelProfile k){ kernelProfile = k; ApplyProfiles(); }
    public void SetGrowthProfile(LeniaGrowthProfile g){ growth = g; ApplyProfiles(); }
    public void SetMuSigma(float mu, float sigma){ if (growth != null){ growth.mu = mu; growth.sigma = Mathf.Max(1e-4f, sigma);} leniaCS.SetFloat("_Mu", growth? growth.mu:mu); leniaCS.SetFloat("_Sigma", growth? growth.sigma:sigma); }
    public void SetDt(float dt){ if (growth != null) growth.dt = Mathf.Max(1e-4f, dt); leniaCS.SetFloat("_Dt", growth? growth.dt:dt); }
}

