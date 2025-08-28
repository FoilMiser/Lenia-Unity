using System.Collections.Generic;
using UnityEngine;

public enum BoundaryMode { Clamp=0, Wrap=1, Reflect=2 }
public enum ScatterMode  { Auto=0, Bilinear=1, Square=2 }

[ExecuteAlways]
public class FlowLeniaSimulation : MonoBehaviour
{
    [Header("Resolution & Channels")]
    public int width = 1024, height = 1024;
    [Range(1,3)] public int channels = 3;

    [Header("Model & Shader")]
    public FlowLeniaModel model;
    public ComputeShader cs;

    [Header("Dynamics")]
    public float dt = 0.8f;
    public float alpha = 1.0f, beta = 1.0f;
    public float temperature = 1.0f;
    public float maxSpeed = 0.0f;
    public BoundaryMode boundary = BoundaryMode.Wrap;
    public ScatterMode  scatterMode = ScatterMode.Auto;

    [Header("Tiled Dispatch (avoid TDR)")]
    public bool tiledDispatch = true;
    public int tileSize = 256;

    [Header("Parameter Localization (optional)")]
    public bool useMuMap = false;    public Texture2D muMap;
    public bool useSigmaMap = false; public Texture2D sigmaMap;
    public bool useTempMap = false;  public Texture2D tempMap;

    [Header("Diagnostics")]
    public bool computeMassEachFrame = false;
    public float[] lastMassPerChannel;

    const float FP = 65536f;

    RenderTexture[] _stateA, _stateB, _affinity, _next, _accumFP;
    RenderTexture[] _gradAff, _gradConc;
    bool _pong;

    int kClearAccum, kAffinity, kGradients, kAdvectScatter, kCollect, kMassSum;

    // NEW: precomputed 2D kernel texture (rebuilt per kernel struct)
    Texture2D _kernel2D;

    ComputeBuffer _massTiles;
    int _tilesX, _tilesY;

    static Texture2D s_Black1x1, s_One1x1;

    static void EnsureFallbackTextures()
    {
        if (s_Black1x1 == null) {
            s_Black1x1 = new Texture2D(1,1,TextureFormat.RFloat,false,true){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            s_Black1x1.SetPixel(0,0,new Color(0,0,0,1)); s_Black1x1.Apply(false,true);
        }
        if (s_One1x1 == null) {
            s_One1x1 = new Texture2D(1,1,TextureFormat.RFloat,false,true){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            s_One1x1.SetPixel(0,0,new Color(1,0,0,1)); s_One1x1.Apply(false,true);
        }
    }

    void OnEnable()
    {
        if (!SystemInfo.supportsComputeShaders) { Debug.LogError("[FlowLenia] Compute shaders not supported."); enabled = false; return; }

        if (cs != null) {
            kClearAccum   = cs.FindKernel("ClearAccum");
            kAffinity     = cs.FindKernel("Affinity");
            kGradients    = cs.FindKernel("Gradients");
            kAdvectScatter= cs.FindKernel("AdvectScatter");
            kCollect      = cs.FindKernel("Collect");
            kMassSum      = cs.FindKernel("MassSum");
        } else {
            Debug.LogWarning("[FlowLenia] Assign FlowLenia.compute.");
        }

        EnsureFallbackTextures();
        Allocate();
    }

    void OnDisable(){ Release(); }
    void OnDestroy(){ Release(); }

    void OnValidate()
    {
        width = Mathf.Max(8, width);
        height = Mathf.Max(8, height);
        tileSize = Mathf.Clamp(tileSize, 64, 1024);
        channels = Mathf.Clamp(channels, 1, 3);
        if (isActiveAndEnabled) Allocate();
    }

    public bool IsReady()
      => _stateA != null && _stateB != null && _stateA.Length >= channels && _stateA[0] != null;

    public RenderTexture CurrentTexture(int ch = 0)
    {
        if (!IsReady()) return null;
        ch = Mathf.Clamp(ch, 0, channels - 1);
        return _pong ? _stateB[ch] : _stateA[ch];
    }

    void Update()
    {
        if (cs == null || model == null) return;
        Step();
        if (computeMassEachFrame) ComputeMassSums();
    }

    // ---- allocate / release ----
    void Allocate()
    {
        Release();

        _stateA   = new RenderTexture[channels];
        _stateB   = new RenderTexture[channels];
        _affinity = new RenderTexture[channels];
        _next     = new RenderTexture[channels];
        _accumFP  = new RenderTexture[channels];
        _gradAff  = new RenderTexture[channels];
        _gradConc = new RenderTexture[channels];

        for (int c = 0; c < channels; c++) {
            _stateA[c]   = MakeRT(RenderTextureFormat.RFloat);
            _stateB[c]   = MakeRT(RenderTextureFormat.RFloat);
            _affinity[c] = MakeRT(RenderTextureFormat.RFloat);
            _next[c]     = MakeRT(RenderTextureFormat.RFloat);
            _gradAff[c]  = MakeRT(RenderTextureFormat.RGFloat);
            _gradConc[c] = MakeRT(RenderTextureFormat.RGFloat);

            _accumFP[c]  = new RenderTexture(width, height, 0, RenderTextureFormat.RInt) {
                enableRandomWrite = true, filterMode = FilterMode.Point, wrapMode = ToWrapMode(boundary)
            };
            _accumFP[c].Create();
        }

        lastMassPerChannel = new float[channels];
        _pong = false;

        SeedGaussian(_stateA[0], 0.25f, 0.25f, 16f, 1f);

        _tilesX = (width  + 15) / 16;
        _tilesY = (height + 15) / 16;
        _massTiles = new ComputeBuffer(_tilesX * _tilesY, sizeof(int));
    }

    RenderTexture MakeRT(RenderTextureFormat fmt)
    {
        var rt = new RenderTexture(width, height, 0, fmt) {
            enableRandomWrite = true, filterMode = FilterMode.Point, wrapMode = ToWrapMode(boundary)
        };
        rt.Create();
        return rt;
    }

    void Release()
    {
        void Kill(RenderTexture rt) { if (rt != null) rt.Release(); }
        if (_stateA != null) foreach (var x in _stateA) Kill(x);
        if (_stateB != null) foreach (var x in _stateB) Kill(x);
        if (_affinity!= null) foreach (var x in _affinity) Kill(x);
        if (_next    != null) foreach (var x in _next) Kill(x);
        if (_accumFP != null) foreach (var x in _accumFP) Kill(x);
        if (_gradAff != null) foreach (var x in _gradAff) Kill(x);
        if (_gradConc!= null) foreach (var x in _gradConc) Kill(x);
        _kernel2D = null;
        _massTiles?.Dispose(); _massTiles = null;
    }

    // ---- main step (tiled; precomputed 2D kernel) ----
    public void Step()
    {
        if (cs == null || model?.kernels == null || model.kernels.Count == 0 || !IsReady()) return;

        // 0) clear affinity
        for (int c = 0; c < channels; c++) Graphics.Blit(Texture2D.blackTexture, _affinity[c]);

        // 1) Affinity for each kernel, tiled
        foreach (var kp in model.kernels)
        {
            if (kp == null || kp.c0 >= channels || kp.c1 >= channels) continue;

            var ktex = BuildKernel2DTexture(model.R, kp.rings); // small K×K texture
            ForEachTile((x0,y0,w,h) => {
                SetCommon(kAffinity, CurrentTexture(kp.c0), _affinity[kp.c1], null, null, null, null, x0,y0);
                cs.SetInt("_KernelSize", 2 * model.R + 1);
                cs.SetFloat("_Mu", kp.mu);
                cs.SetFloat("_Sigma", Mathf.Max(1e-5f, kp.sigma));
                cs.SetFloat("_H", kp.h);
                BindParamMaps(kAffinity);
                cs.SetTexture(kAffinity, "_Kernel2D", ktex);
                DispatchTile(kAffinity, w, h);
            });
        }

        // 2) Gradients per channel
        for (int c = 0; c < channels; c++)
        {
            ForEachTile((x0,y0,w,h) => {
                SetCommon(kGradients, CurrentTexture(c), _affinity[c], _gradAff[c], _gradConc[c], null, null, x0,y0);
                DispatchTile(kGradients, w, h);
            });
        }

        // 3) Advect+Scatter + Collect
        for (int c = 0; c < channels; c++)
        {
            // clear accum
            ForEachTile((x0,y0,w,h) => {
                cs.SetInt("_Width",  width);
                cs.SetInt("_Height", height);
                cs.SetInt("_Boundary", (int)boundary);
                cs.SetInt("_OffsetX", x0);
                cs.SetInt("_OffsetY", y0);
                cs.SetTexture(kClearAccum, "_Accum", _accumFP[c]);
                DispatchTile(kClearAccum, w, h);
            });

            // advect
            ForEachTile((x0,y0,w,h) => {
                SetCommon(kAdvectScatter, CurrentTexture(c), _affinity[c], _gradAff[c], _gradConc[c], null, _accumFP[c], x0,y0);
                cs.SetFloat("_Dt", dt);
                cs.SetFloat("_Alpha", alpha);
                cs.SetFloat("_Beta", beta);
                cs.SetFloat("_Temperature", temperature);
                cs.SetFloat("_MaxSpeed", maxSpeed);
                cs.SetInt("_ScatterMode", (int)scatterMode);
                BindParamMaps(kAdvectScatter);
                DispatchTile(kAdvectScatter, w, h);
            });

            // collect
            var next = _pong ? _stateA[c] : _stateB[c];
            ForEachTile((x0,y0,w,h) => {
                cs.SetInt("_Width",  width);
                cs.SetInt("_Height", height);
                cs.SetInt("_Boundary", (int)boundary);
                cs.SetInt("_OffsetX", x0);
                cs.SetInt("_OffsetY", y0);
                cs.SetTexture(kCollect, "_Next",  next);
                cs.SetTexture(kCollect, "_Accum", _accumFP[c]);
                DispatchTile(kCollect, w, h);
            });
        }

        _pong = !_pong;
    }

    // ---- diagnostics ----
    public void ComputeMassSums()
    {
        if (_massTiles == null || !IsReady()) return;
        int gx = (width  + 15) / 16;
        int gy = (height + 15) / 16;

        cs.SetInt("_TilesX", gx);
        cs.SetInt("_TilesY", gy);
        cs.SetInt("_Width",  width);
        cs.SetInt("_Height", height);
        cs.SetInt("_Boundary", (int)boundary);
        cs.SetInt("_OffsetX", 0);
        cs.SetInt("_OffsetY", 0);

        for (int c = 0; c < channels; c++)
        {
            cs.SetTexture(kMassSum, "_State", CurrentTexture(c));
            cs.SetBuffer(kMassSum, "_MassTiles", _massTiles);
            cs.Dispatch(kMassSum, gx, gy, 1);

            var tmp = new int[gx * gy];
            _massTiles.GetData(tmp);
            long sumFP = 0;
            for (int i = 0; i < tmp.Length; i++) sumFP += tmp[i];
            lastMassPerChannel[c] = sumFP / FP;
        }
    }

    // ---- helpers ----
    void ForEachTile(System.Action<int,int,int,int> fn)
    {
        if (!tiledDispatch) { fn(0,0,width,height); return; }
        int TS = Mathf.Clamp(tileSize, 64, 2048);
        for (int y0 = 0; y0 < height; y0 += TS) {
            int h = Mathf.Min(TS, height - y0);
            for (int x0 = 0; x0 < width; x0 += TS) {
                int w = Mathf.Min(TS, width - x0);
                fn(x0, y0, w, h);
            }
        }
    }

    void DispatchTile(int kernel, int w, int h)
    {
        int gx = (w + 15) / 16;
        int gy = (h + 15) / 16;
        cs.Dispatch(kernel, gx, gy, 1);
    }

    void SetCommon(int kernel,
                   RenderTexture state, RenderTexture affinity,
                   RenderTexture gradAff, RenderTexture gradConc,
                   RenderTexture next, RenderTexture accum,
                   int offsetX, int offsetY)
    {
        cs.SetInt("_Width",  width);
        cs.SetInt("_Height", height);
        cs.SetInt("_Boundary", (int)boundary);
        cs.SetInt("_OffsetX", offsetX);
        cs.SetInt("_OffsetY", offsetY);

        if (state   != null) cs.SetTexture(kernel, "_State",    state);
        if (affinity!= null) cs.SetTexture(kernel, "_Affinity", affinity);
        if (gradAff != null) cs.SetTexture(kernel, "_GradAff",  gradAff);
        if (gradConc!= null) cs.SetTexture(kernel, "_GradConc", gradConc);
        if (next    != null) cs.SetTexture(kernel, "_Next",     next);
        if (accum   != null) cs.SetTexture(kernel, "_Accum",    accum);
    }

    void BindParamMaps(int kernel)
    {
        EnsureFallbackTextures();
        cs.SetInt("_UseMuMap",    (useMuMap    && muMap    != null) ? 1 : 0);
        cs.SetInt("_UseSigmaMap", (useSigmaMap && sigmaMap != null) ? 1 : 0);
        cs.SetInt("_UseTempMap",  (useTempMap  && tempMap  != null) ? 1 : 0);

        cs.SetTexture(kernel, "_MuMap",    muMap    != null ? muMap    : s_Black1x1);
        cs.SetTexture(kernel, "_SigmaMap", sigmaMap != null ? sigmaMap : s_One1x1);
        cs.SetTexture(kernel, "_TempMap",  tempMap  != null ? tempMap  : s_Black1x1);
    }

    // Build small K×K kernel texture (weights zeroed outside ring support, normalized by area)
Texture2D BuildKernel2DTexture(int R, List<FlowRing> rings)
{
    int K = 2 * R + 1;

    // (Re)create if size changed OR if Unity marked it non-readable earlier
    if (_kernel2D == null || _kernel2D.width != K || _kernel2D.height != K || !_kernel2D.isReadable)
    {
        if (_kernel2D != null) DestroyImmediate(_kernel2D);
        _kernel2D = new Texture2D(K, K, TextureFormat.RFloat, false, true)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags  = HideFlags.HideAndDontSave
        };
    }

    // Fill weights directly into the readable CPU buffer
    var data = _kernel2D.GetRawTextureData<float>();
    for (int i = 0; i < data.Length; i++) data[i] = 0f;

    float sum = 0f;
    for (int j = 0; j < K; j++)
    {
        for (int i = 0; i < K; i++)
        {
            int dx = i - R, dy = j - R;
            int r2 = dx*dx + dy*dy;
            if (r2 > R*R) continue;

            float wsum = 0f;
            if (rings != null && rings.Count > 0)
            {
                foreach (var ring in rings)
                {
                    if (ring == null) continue;
                    int outer = Mathf.Clamp(ring.r, 0, R);
                    int inner = Mathf.Clamp(ring.r - ring.w, 0, R);
                    int rr = Mathf.RoundToInt(Mathf.Sqrt(r2));
                    if (rr < inner || rr > outer) continue;

                    var b = (ring.b != null && ring.b.Count > 0) ? ring.b : new List<float>{1f};
                    int bins = b.Count;
                    float t = (outer == inner) ? 0f : (rr - inner) / (float)(outer - inner);
                    int bi = Mathf.Clamp(Mathf.FloorToInt(t * bins), 0, bins - 1);
                    wsum += Mathf.Max(0f, b[bi]);
                }
            }
            else
            {
                // default: single tap at center
                wsum = (dx == 0 && dy == 0) ? 1f : 0f;
            }

            data[j*K + i] = wsum;
            sum += wsum;
        }
    }

    // Normalize so weights sum to 1
    if (sum > 1e-8f)
    {
        float inv = 1f / sum;
        for (int idx = 0; idx < data.Length; idx++) data[idx] *= inv;
    }

    // IMPORTANT: keep it readable (second arg = false)
    _kernel2D.Apply(false, false);
    return _kernel2D;
}
    void SeedGaussian(RenderTexture rt, float nx, float ny, float sigmaPx, float amp)
    {
        var tmp = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
        var arr = tex.GetRawTextureData<float>();
        for (int y = 0; y < rt.height; y++)
            for (int x = 0; x < rt.width; x++) {
                float cx = nx * rt.width, cy = ny * rt.height;
                float dx = x - cx, dy = y - cy;
                arr[y * rt.width + x] = amp * Mathf.Exp(-(dx*dx + dy*dy) / (2f * sigmaPx * sigmaPx));
            }
        tex.LoadRawTextureData(arr); tex.Apply(false);
        Graphics.Blit(tex, rt);
        Object.DestroyImmediate(tex);
        RenderTexture.active = tmp;
    }

    static TextureWrapMode ToWrapMode(BoundaryMode mode)
      => (mode == BoundaryMode.Wrap) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
}
