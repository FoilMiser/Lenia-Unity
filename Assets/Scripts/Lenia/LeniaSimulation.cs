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

    void EnsureView(){
        if (view == null)
        {
            var go = GameObject.Find("LeniaViewCanvas") ?? new GameObject("LeniaViewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (go.GetComponent<Canvas>() is Canvas c) c.renderMode = RenderMode.ScreenSpaceOverlay;
            var imgGO = new GameObject("LeniaView", typeof(RawImage));
            imgGO.transform.SetParent(go.transform, false);
            view = imgGO.GetComponent<RawImage>();
            imgGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1024, 1024);
        }
        view.texture = _A; ApplyDisplayMaterialIfMissing();
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
        if (view) view.texture = _A; ApplyDisplayMaterialIfMissing();
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
    // --- Compatibility properties for legacy scripts ---
    public RenderTexture EnvTex => _A;
    public RenderTexture CurrentState => _A;

    // ===== Legacy compatibility shims for UI/Presets =====
    private bool _useRingKernel = true;
    public bool useRingKernel { get => _useRingKernel; set { _useRingKernel = value; /* always ring kernel */ } }
    public bool useMultiRing
    {
        get => kernelProfile && kernelProfile.ringCount > 1;
        set { if (kernelProfile) { kernelProfile.EnsureRingCount(value ? 2 : 1); ApplyProfiles(); } }
    }
    public int width  { get => resolution.x; set => SetResolutionWH(value, resolution.y); }
    public int height { get => resolution.y; set => SetResolutionWH(resolution.x, value); }
    public float ringCenter
    {
        get => (kernelProfile && kernelProfile.means != null && kernelProfile.means.Length > 0) ? kernelProfile.means[0] : 0.5f;
        set { if (!kernelProfile) return; kernelProfile.EnsureRingCount(1); kernelProfile.means[0] = Mathf.Clamp01(value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ringWidth
    {
        get => (kernelProfile && kernelProfile.stddevs != null && kernelProfile.stddevs.Length > 0) ? kernelProfile.stddevs[0] : 0.08f;
        set { if (!kernelProfile) return; kernelProfile.EnsureRingCount(1); kernelProfile.stddevs[0] = Mathf.Max(1e-4f, value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Center
    {
        get => (kernelProfile && kernelProfile.means != null && kernelProfile.means.Length > 1) ? kernelProfile.means[1] : 0.62f;
        set { if (!kernelProfile) return; kernelProfile.EnsureRingCount(2); kernelProfile.means[1] = Mathf.Clamp01(value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Width
    {
        get => (kernelProfile && kernelProfile.stddevs != null && kernelProfile.stddevs.Length > 1) ? kernelProfile.stddevs[1] : 0.07f;
        set { if (!kernelProfile) return; kernelProfile.EnsureRingCount(2); kernelProfile.stddevs[1] = Mathf.Max(1e-4f, value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Weight
    {
        get => (kernelProfile && kernelProfile.peaks != null && kernelProfile.peaks.Length > 1) ? kernelProfile.peaks[1] : 0.65f;
        set { if (!kernelProfile) return; kernelProfile.EnsureRingCount(2); kernelProfile.peaks[1] = Mathf.Max(0f, value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public void ApplyPreset() { ApplyProfiles(); }

    // Flexible legacy overload: ApplyPreset(bool useRing, bool useMulti, float ringCenter, float ringWidth, [float ring2Center/Width/Weight...])
    public void ApplyPreset(params object[] args)
    {
        // No-arg still supported by the other overload
        if (args == null || args.Length == 0) { ApplyProfiles(); return; }
        if (kernelProfile == null) return;

        bool? useRing = null, useMulti = null;
        var vals = new System.Collections.Generic.List<float>();

        foreach (var a in args)
        {
            if (a is bool b)
            { if (useRing == null) useRing = b; else useMulti = b; }
            else if (a is float f) vals.Add(f);
            else if (a is double d) vals.Add((float)d);
            else if (a is int i) vals.Add(i);
            else if (a != null)
            {
                float parsed;
                if (float.TryParse(a.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                    vals.Add(parsed);
            }
        }

        // Decide ring count
        int rc = (useMulti.HasValue && useMulti.Value) ? 2 : 1;
        if (vals.Count >= 4) rc = 2; // heuristic: if 4+ floats provided, assume two rings

        kernelProfile.EnsureRingCount(rc);

        // Map values: [c0, w0, (c1, w1, [w1Peak])]
        if (rc >= 1 && vals.Count >= 2)
        {
            kernelProfile.means[0]   = Mathf.Clamp01(vals[0]);
            kernelProfile.stddevs[0] = Mathf.Max(1e-4f, vals[1]);
        }
        if (rc >= 2 && vals.Count >= 4)
        {
            kernelProfile.means[1]   = Mathf.Clamp01(vals[2]);
            kernelProfile.stddevs[1] = Mathf.Max(1e-4f, vals[3]);
        }
        if (rc >= 2 && vals.Count >= 5)
        {
            // optional peak/weight for ring2
            kernelProfile.peaks[1]   = Mathf.Max(0f, vals[4]);
        }

        kernelProfile.Invalidate();
        ApplyProfiles();
    }

    void ApplyDisplayMaterialIfMissing()
    {
        if (view != null && (view.material == null))
        {
            var mat = Resources.Load<Material>("LeniaDisplay");
            if (mat != null)
            {
                view.material = mat;
                view.color = Color.white;
            }
        }
    }
}

