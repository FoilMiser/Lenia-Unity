using UnityEngine;
using UnityEngine.UI;

public class LeniaSimulation : MonoBehaviour
{
    // -------- View --------
    [Header("View")]
    public RawImage view;
    public Vector2Int resolution = new Vector2Int(512, 512);

    // -------- Rules --------
    [Header("Rules")]
    public LeniaKernelProfile kernelProfile;
    public LeniaGrowthProfile  growth;

    // -------- Runtime --------
    [Header("Runtime")]
    public ComputeShader leniaCS;
    [Range(0f,1f)] public float seedFill = 0.15f;
    public bool  autoRun = true;
    public int   stepsPerFrame = 1;

    // -------- Display Settings --------
    [Header("Display Settings")]
    public float dispExposure = 8f;
    public float dispGamma    = 1.2f;
    public float paletteOffset = 0f;
    public float paletteScale  = 1f;

    public bool  useEdges = true;
    public float edgeStrength = 0.6f;
    public float edgeThreshold = 0.015f;

    public bool  useTrail = true;
    public float trailDecay = 0.975f;
    public float trailBoost = 1.4f;
    public float trailWeight = 0.8f;
    public Color trailTint = new Color(1f,0.3f,0.3f,1f);
    [Range(0,2)] public int trailMode = 2; // 0=Screen, 1=Add, 2=Lerp

    public bool  useGlow = true;
    public float glowThreshold = 0.35f;
    public float glowSigma = 2.0f;
    public float glowStrength = 0.7f;
    public Color glowTint = new Color(1f,0.8f,0.4f,1f);

    // -------- Private runtime --------
    RenderTexture _A, _B;                    // state ping-pong
    RenderTexture _TrailA, _TrailB;          // trail ping-pong
    RenderTexture _GlowA, _GlowB;            // glow ping-pong
    int _kStep = -1;

    Material _dispMat, _trailMat, _blurMat;
    Texture2D _paletteTex;

    // ================== Unity lifecycle ==================
    void Awake()
    {
        EnsureDefaults();
        try { _kStep = leniaCS.FindKernel("Step"); }
        catch (System.Exception e){ Debug.LogError("Lenia kernel 'Step' not found: " + e.Message); enabled = false; return; }

        EnsureRTs();
        EnsureView();
        ReseedMuCentered();
        ApplyProfiles();
    }

    void OnEnable(){ UpdateTrailAndMaterial(); UpdateGlow(); }
    void OnValidate(){ ClampDisplayParams(); EnsureRTs(); ApplyProfiles(); }

    void Update()
    {
        if (!autoRun) return;
        for (int i=0;i<stepsPerFrame;i++) Step();
    }

    // ================== Core sim ==================
    public void ApplyProfiles(){ if (!EnsureKernel()) return;
        EnsureDefaults();
        EnsureRTs();

        if (leniaCS == null) { Debug.LogError("Lenia CS missing"); return; }

        var K = (kernelProfile != null) ? kernelProfile.GetOrBuildKernelTexture() : null;
        if (K == null){ Debug.LogError("Kernel texture is null"); return; }

        leniaCS.SetTexture(_kStep, "_KernelTex", K);
        leniaCS.SetInt("_KernelRadius", kernelProfile ? kernelProfile.radius : 24);

        leniaCS.SetFloat("_Mu",    growth ? growth.mu    : 0.15f);
        leniaCS.SetFloat("_Sigma", growth ? growth.sigma : 0.015f);
        leniaCS.SetFloat("_Dt",    growth ? growth.dt    : 0.07f);

        if (_A == null || _B == null) { Debug.LogWarning("RTs not ready"); return; }
        leniaCS.SetTexture(_kStep, "_StateIn",  _A);
        leniaCS.SetTexture(_kStep, "_StateOut", _B);
        leniaCS.SetInts("_Resolution", new int[]{ resolution.x, resolution.y });

        if (view) view.texture = _A;
        UpdateTrailAndMaterial();
        UpdateGlow();
    }

    public void Step(){ if (!EnsureKernel()) return;
        if (leniaCS == null || _kStep < 0) return;
        leniaCS.Dispatch(_kStep, Mathf.CeilToInt(resolution.x/8f), Mathf.CeilToInt(resolution.y/8f), 1);
        var t = _A; _A = _B; _B = t;  // swap
        leniaCS.SetTexture(_kStep, "_StateIn",  _A);
        leniaCS.SetTexture(_kStep, "_StateOut", _B);
        if (view) view.texture = _A;
        UpdateTrailAndMaterial();
        UpdateGlow();
    }

    // ================== Setup helpers ==================
    void EnsureDefaults()
    {
        if (leniaCS == null) leniaCS = Resources.Load<ComputeShader>("Lenia");

        if (kernelProfile == null){
            kernelProfile = ScriptableObject.CreateInstance<LeniaKernelProfile>();
            kernelProfile.radius = 28; kernelProfile.EnsureRingCount(2);
            kernelProfile.means   = new float[]{ 0.35f, 0.62f };
            kernelProfile.stddevs = new float[]{ 0.055f, 0.075f };
            kernelProfile.peaks   = new float[]{ 1.00f, 0.65f };
        }
        if (growth == null){
            growth = ScriptableObject.CreateInstance<LeniaGrowthProfile>();
            growth.mu = 0.15f; growth.sigma = 0.015f; growth.dt = 0.07f;
        }
    }

    void EnsureRTs()
    {
        CreateRT(ref _A);
        CreateRT(ref _B);
        EnsureTrailRTs();
        EnsureGlowRTs();
    }

    void CreateRT(ref RenderTexture rt)
    {
        if (rt != null && rt.IsCreated() && rt.width == resolution.x && rt.height == resolution.y) return;
        if (rt != null) rt.Release();
        rt = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat){
            enableRandomWrite = true, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point
        };
        rt.Create();
    }

    void EnsureView()
    {
        if (view == null)
        {
            var canvasGO = GameObject.Find("LeniaViewCanvas");
            if (!canvasGO)
            {
                canvasGO = new GameObject("LeniaViewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasGO.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var imgGO = new GameObject("LeniaView", typeof(RawImage));
            imgGO.transform.SetParent(canvasGO.transform, false);
            view = imgGO.GetComponent<RawImage>();
            imgGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1024,1024);
        }
        if (view && _A) view.texture = _A;
    }

    // ================== Seeding ==================
    public void Reseed(int? seed = null)
    {
        EnsureRTs();
        var prev = RenderTexture.active; RenderTexture.active = _A;
        var tmp = new Texture2D(resolution.x, resolution.y, TextureFormat.RFloat, false, true);
        var rnd = new System.Random(seed ?? (int)System.DateTime.Now.Ticks);
        var data = new Color[resolution.x * resolution.y];
        for (int i=0;i<data.Length;i++)
        {
            float v = (float)(rnd.NextDouble() < seedFill ? rnd.NextDouble() : 0.0);
            data[i] = new Color(v,0,0,0);
        }
        tmp.SetPixels(data); tmp.Apply(); Graphics.Blit(tmp, _A); DestroyImmediate(tmp);
        RenderTexture.active = prev;
        if (view) view.texture = _A;
    }

    public void ReseedMuCentered(int? seed=null, float noise=0.06f)
    {
        EnsureRTs();
        float target = growth ? Mathf.Clamp01(growth.mu) : 0.15f;
        var rnd = new System.Random(seed ?? (int)System.DateTime.Now.Ticks);

        var prev = RenderTexture.active; RenderTexture.active = _A;
        var tmp = new Texture2D(resolution.x, resolution.y, TextureFormat.RFloat, false, true);
        var px = new Color[resolution.x * resolution.y];
        for (int i=0;i<px.Length;i++){
            float n = (float)((rnd.NextDouble()+rnd.NextDouble()+rnd.NextDouble())/3.0);
            float v = Mathf.Clamp01(target + (n-0.5f)*2f*noise);
            px[i] = new Color(v,0,0,0);
        }
        tmp.SetPixels(px); tmp.Apply(); Graphics.Blit(tmp, _A); DestroyImmediate(tmp);
        RenderTexture.active = prev;
        if (view) view.texture = _A;
    }

    // ================== Display / Post ==================
    Texture2D BuildViridisPalette()
    {
        var t = new Texture2D(256,1, TextureFormat.RGBA32, false, true);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        var px = new Color[256];
        for (int i=0;i<256;i++){
            float x = i/255f;
            float r = Mathf.Clamp01(1.5f*x - 0.5f*x*x);
            float g = Mathf.Clamp01(1.2f*x*(1.0f-x)*3.2f + 0.1f + 0.6f*x);
            float b = Mathf.Clamp01(1.0f - x*0.7f + 0.2f*(1.0f-x));
            px[i] = new Color(r,g,b,1);
        }
        t.SetPixels(px); t.Apply(false, true);
        return t;
    }

    void EnsureTrailRTs()
    {
        bool ok = (_TrailA != null && _TrailA.IsCreated() && _TrailB != null && _TrailB.IsCreated()
                   && _TrailA.width == resolution.x && _TrailA.height == resolution.y
                   && _TrailB.width == resolution.x && _TrailB.height == resolution.y);
        if (ok) return;

        if (_TrailA != null) _TrailA.Release();
        if (_TrailB != null) _TrailB.Release();

        _TrailA = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat)
        { enableRandomWrite=false, wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Bilinear };
        _TrailB = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat)
        { enableRandomWrite=false, wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Bilinear };
        _TrailA.Create(); _TrailB.Create();
    }

    void EnsureGlowRTs()
    {
        bool ok = (_GlowA != null && _GlowA.IsCreated() && _GlowB != null && _GlowB.IsCreated()
                   && _GlowA.width == resolution.x && _GlowA.height == resolution.y
                   && _GlowB.width == resolution.x && _GlowB.height == resolution.y);
        if (ok) return;

        if (_GlowA != null) _GlowA.Release();
        if (_GlowB != null) _GlowB.Release();

        _GlowA = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat)
        { enableRandomWrite=false, wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
        _GlowB = new RenderTexture(resolution.x, resolution.y, 0, RenderTextureFormat.RFloat)
        { enableRandomWrite=false, wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
        _GlowA.Create(); _GlowB.Create();
    }

    void UpdateTrailAndMaterial()
    {
        if (_dispMat == null){ var sh = Shader.Find("Unlit/LeniaPalette"); if (sh) _dispMat = new Material(sh); }
        if (_trailMat== null){ var sh = Shader.Find("Hidden/LeniaTrailUpdate"); if (sh) _trailMat = new Material(sh); }
        if (_paletteTex == null) _paletteTex = BuildViridisPalette();
        if (view != null && _dispMat != null) view.material = _dispMat;

        // Trails: ping-pong TrailA/B (avoid read/write same RT)
        if (useTrail && _trailMat != null)
        {
            EnsureTrailRTs();
            _trailMat.SetFloat("_Decay", trailDecay);
            _trailMat.SetFloat("_Boost", trailBoost);
            _trailMat.SetTexture("_TrailTex", _TrailA); // read previous
            Graphics.Blit(_A, _TrailB, _trailMat);      // write next
            var tmp = _TrailA; _TrailA = _TrailB; _TrailB = tmp; // swap
        }

        // Bind to palette
        if (_dispMat != null)
        {
            _dispMat.SetTexture("_MainTex", _A);
            _dispMat.SetTexture("_TrailTex", _TrailA);
            _dispMat.SetTexture("_PaletteTex", _paletteTex);

            _dispMat.SetFloat("_Exposure", dispExposure);
            _dispMat.SetFloat("_Gamma", dispGamma);
            _dispMat.SetFloat("_PaletteOffset", paletteOffset);
            _dispMat.SetFloat("_PaletteScale",  paletteScale);

            _dispMat.SetFloat("_UseEdges", useEdges ? 1f : 0f);
            _dispMat.SetFloat("_EdgeStrength",  edgeStrength);
            _dispMat.SetFloat("_EdgeThreshold", edgeThreshold);

            _dispMat.SetFloat("_UseTrail", useTrail ? 1f : 0f);
            _dispMat.SetFloat("_TrailWeight", trailWeight);
            _dispMat.SetColor("_TrailTint", trailTint);
            _dispMat.SetFloat("_TrailMode", trailMode);
        }
    }

    void UpdateGlow()
    {
        if (!useGlow){ if (_dispMat) _dispMat.SetFloat("_UseGlow", 0f); return; }

        if (_blurMat == null){ var sh = Shader.Find("Hidden/LeniaBlurThreshold"); if (sh) _blurMat = new Material(sh); }
        if (_blurMat == null){ if (_dispMat) _dispMat.SetFloat("_UseGlow", 0f); return; }

        EnsureGlowRTs();

        _blurMat.SetFloat("_Threshold", glowThreshold);
        _blurMat.SetFloat("_Sigma", glowSigma);

        _blurMat.SetVector("_Direction", new Vector4(1,0,0,0));
        Graphics.Blit(_A, _GlowA, _blurMat);
        _blurMat.SetVector("_Direction", new Vector4(0,1,0,0));
        Graphics.Blit(_GlowA, _GlowB, _blurMat);

        if (_dispMat != null)
        {
            _dispMat.SetFloat("_UseGlow", 1f);
            _dispMat.SetTexture("_GlowTex", _GlowB);
            _dispMat.SetFloat("_GlowStrength", glowStrength);
            _dispMat.SetColor("_GlowTint", glowTint);
        }
    }

    void ClampDisplayParams()
    {
        dispExposure = Mathf.Max(0.01f, dispExposure);
        dispGamma    = Mathf.Max(0.01f, dispGamma);
        edgeThreshold = Mathf.Max(1e-4f, edgeThreshold);
        trailDecay   = Mathf.Clamp(trailDecay, 0.8f, 0.9999f);
        trailWeight  = Mathf.Clamp01(trailWeight);
        glowSigma    = Mathf.Max(0.25f, glowSigma);
        glowStrength = Mathf.Clamp01(glowStrength);
        paletteScale = Mathf.Max(0.001f, paletteScale);
    }

    // ================== Compatibility shims (UI/Presets) ==================
    public void SetStepsPerFrame(int v){ stepsPerFrame = Mathf.Max(0,v); }
    public void SetAutoRun(bool run){ autoRun = run; }
    public void SetSeedFill(float v){ seedFill = Mathf.Clamp01(v); }
    public void SetResolution(Vector2Int res){ if (res.x<8||res.y<8) return; resolution = res; EnsureRTs(); ApplyProfiles(); }
    public void SetResolutionWH(int w,int h){ SetResolution(new Vector2Int(Mathf.Max(8,w), Mathf.Max(8,h))); }
    public void SetKernelProfile(LeniaKernelProfile k){ kernelProfile = k; ApplyProfiles(); }
    public void SetGrowthProfile(LeniaGrowthProfile g){ growth = g; ApplyProfiles(); }
    public void SetMuSigma(float mu,float sigma){ if (growth!=null){ growth.mu=mu; growth.sigma=Mathf.Max(1e-4f,sigma);} leniaCS.SetFloat("_Mu", growth?growth.mu:mu); leniaCS.SetFloat("_Sigma", growth?growth.sigma:sigma); }
    public void SetDt(float dt){ if (growth!=null) growth.dt=Mathf.Max(1e-4f,dt); leniaCS.SetFloat("_Dt", growth?growth.dt:dt); }

    public RenderTexture CurrentTexture => _A;
    public RenderTexture CurrentState   => _A;
    public RenderTexture EnvTex         => _A;

    public int width  { get => resolution.x; set => SetResolutionWH(value, resolution.y); }
    public int height { get => resolution.y; set => SetResolutionWH(resolution.x, value); }

    private bool _useRingKernel = true;
    public bool useRingKernel { get => _useRingKernel; set { _useRingKernel = value; } }

    void EnsureRings(int n){ if (kernelProfile) kernelProfile.EnsureRingCount(n); }

    public bool useMultiRing
    {
        get => kernelProfile && kernelProfile.ringCount > 1;
        set { if (kernelProfile){ kernelProfile.EnsureRingCount(value ? 2 : 1); ApplyProfiles(); } }
    }
    public float ringCenter
    {
        get => (kernelProfile && kernelProfile.means!=null && kernelProfile.means.Length>0) ? kernelProfile.means[0] : 0.5f;
        set { if (!kernelProfile) return; EnsureRings(1); kernelProfile.means[0]=Mathf.Clamp01(value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ringWidth
    {
        get => (kernelProfile && kernelProfile.stddevs!=null && kernelProfile.stddevs.Length>0) ? kernelProfile.stddevs[0] : 0.08f;
        set { if (!kernelProfile) return; EnsureRings(1); kernelProfile.stddevs[0]=Mathf.Max(1e-4f,value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Center
    {
        get => (kernelProfile && kernelProfile.means!=null && kernelProfile.means.Length>1) ? kernelProfile.means[1] : 0.62f;
        set { if (!kernelProfile) return; EnsureRings(2); kernelProfile.means[1]=Mathf.Clamp01(value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Width
    {
        get => (kernelProfile && kernelProfile.stddevs!=null && kernelProfile.stddevs.Length>1) ? kernelProfile.stddevs[1] : 0.07f;
        set { if (!kernelProfile) return; EnsureRings(2); kernelProfile.stddevs[1]=Mathf.Max(1e-4f,value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }
    public float ring2Weight
    {
        get => (kernelProfile && kernelProfile.peaks!=null && kernelProfile.peaks.Length>1) ? kernelProfile.peaks[1] : 0.65f;
        set { if (!kernelProfile) return; EnsureRings(2); kernelProfile.peaks[1]=Mathf.Max(0f,value); kernelProfile.Invalidate(); ApplyProfiles(); }
    }

    public void ApplyPreset(){ ApplyProfiles(); }
    public void ApplyPreset(params object[] args)
    {
        if (args == null || args.Length == 0){ ApplyProfiles(); return; }
        if (kernelProfile == null) return;

        bool? useRing = null, useMulti = null;
        var vals = new System.Collections.Generic.List<float>();
        foreach (var a in args)
        {
            if (a is bool b){ if (useRing == null) useRing = b; else useMulti = b; }
            else if (a is float f) vals.Add(f);
            else if (a is double d) vals.Add((float)d);
            else if (a is int i)   vals.Add(i);
            else if (a != null){
                float p; if (float.TryParse(a.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p)) vals.Add(p);
            }
        }
        int rc = (useMulti.HasValue && useMulti.Value) ? 2 : 1;
        if (vals.Count >= 4) rc = 2;

        kernelProfile.EnsureRingCount(rc);

        if (rc >= 1 && vals.Count >= 2){ kernelProfile.means[0]=Mathf.Clamp01(vals[0]); kernelProfile.stddevs[0]=Mathf.Max(1e-4f, vals[1]); }
        if (rc >= 2 && vals.Count >= 4){ kernelProfile.means[1]=Mathf.Clamp01(vals[2]); kernelProfile.stddevs[1]=Mathf.Max(1e-4f, vals[3]); }
        if (rc >= 2 && vals.Count >= 5){ kernelProfile.peaks[1]=Mathf.Max(0f, vals[4]); }

        kernelProfile.Invalidate();
        ApplyProfiles();
    }

    bool EnsureKernel()
    {
        if (leniaCS == null) return false;
        if (_kStep < 0)
        {
            try { _kStep = leniaCS.FindKernel("Step"); }
            catch (System.Exception e) { Debug.LogError("Lenia kernel 'Step' not found: " + e.Message); return false; }
        }
        return true;
    }
}
