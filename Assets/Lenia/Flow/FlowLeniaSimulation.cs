using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Flow-Lenia GPU simulation (Affinity → ClearAccum → AdvectScatter → Collect)
/// Stable path for Unity 6.2 (DX11):
/// - Dispatch only in Play Mode by default
/// - No runtime clone; dispatch directly on the asset
/// - STRICT kernel names: "Affinity", "ClearAccum", "AdvectScatter", "Collect"
/// - Cache kernel IDs once; stop after first dispatch failure to avoid spam
public class FlowLeniaSimulation : MonoBehaviour
{
    [Header("Resolution & Channels")]
    public int width = 512, height = 512;
    [Range(1, 3)] public int channels = 3;

    [Header("Lenia Kernel (rings)")]
    [Range(3, 31)] public int kernelSize = 17; // odd only
    [Range(1, 6)]  public int rings = 3;
    [Range(0.02f, 0.8f)]  public float maxRadius = 0.35f;
    [Range(0.02f, 0.25f)] public float ringSigma = 0.06f;

    [Header("Growth (affinity)")]
    [Range(0, 1)] public float growthMu = 0.15f;
    [Range(0.005f, 0.5f)] public float growthSigma = 0.035f;

    [Header("Flow & Mass")]
    [Range(0.1f, 2.5f)] public float dt = 0.9f;
    [Range(0.25f, 3.0f)] public float flowScale = 1.0f;
    [Range(0.5f, 2.0f)]  public float temperature = 1.0f; // extra global scale
    [Range(0.0f, 3.0f)] public float Mcrit = 0.45f;
    [Range(1.0f, 16.0f)] public float kappa = 6.0f;
    public float massToFixed = 1_048_576f; // 2^20

    [Header("Reaction")]
    [Range(0f, 2f)] public float reactionGain = 0.30f; // scales affinity term in Collect

    [Header("Steps")]
    [Range(1, 20)] public int stepsPerFrame = 1;

    [Header("Assets")]
    public ComputeShader flowCS; // Assign: Assets/Lenia/Flow/FlowLenia.compute

#if UNITY_EDITOR
    [Header("Editor (advanced)")]
    [FormerlySerializedAs("runInEditMode")]
    [Tooltip("Run the GPU simulation while in Edit Mode. Off by default.")]
    public bool allowEditModeRun = false;
    [Tooltip("Skip GPU work while scripts are compiling/reimporting.")]
    public bool skipWhileCompiling = true;
#endif

    [Header("Error Handling")]
    [Tooltip("If a dispatch fails, pause the sim to prevent log spam.")]
    public bool autoDisableOnError = true;

    public RenderTexture CurrentTexture => stateA;

    // Runtime state
    RenderTexture stateA, stateB, affinityRT;
    ComputeBuffer kernelBuf; // float kernel (kernelSize^2)
    ComputeBuffer accumBuf;  // uint (width*height*channels)
    int groupsX, groupsY;

    // Strict kernel IDs (cached)
    int kAffinity = -1, kClear = -1, kAdvect = -1, kCollect = -1;
    bool kernelsReady = false;
    bool pausedDueToDispatchError = false;

    // ---------- Public for viewer/binder ----------
    /// Ensure RTs/CBs exist so a viewer can bind.
    public void EnsureInitialized()
    {
        EnsureComputeShader();
        if (!ResourcesReady())
            InitResourcesOnly();
    }

    // ---------- Unity lifecycle ----------
    void OnEnable()
    {
        EnsureComputeShader();
        InitResourcesOnly();
        SeedCenterBlob(); // visible in Edit Mode (no dispatch)
        kernelsReady = false; // will cache on first Update
    }

    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }

#if UNITY_EDITOR
    void OnValidate()
    {
        width = Mathf.Max(8, width);
        height = Mathf.Max(8, height);
        kernelSize = Mathf.Max(3, kernelSize | 1); // force odd

        EnsureComputeShader();
        InitResourcesOnly();
        kernelsReady = false;
    }

    void EnsureComputeShader()
    {
        if (!flowCS)
        {
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lenia/Flow/FlowLenia.compute");
            if (cs) flowCS = cs;
        }
    }
#else
    void EnsureComputeShader() {}
#endif

    [ContextMenu("Reset State (Clear + Reseed)")]
    public void ResetState()
    {
        InitResourcesOnly();
        ClearRT(stateA, new Color(0, 0, 0, 1));
        ClearRT(stateB, new Color(0, 0, 0, 1));
        SeedCenterBlob();
    }

    [ContextMenu("Resume Simulation (after error)")]
    public void ResumeSimulationAfterError()
    {
        pausedDueToDispatchError = false;
        kernelsReady = false; // recache on next frame
        Debug.Log("[FlowLenia] Resumed after previous dispatch error.");
    }

    void Update()
    {
#if UNITY_EDITOR
        if (skipWhileCompiling && EditorApplication.isCompiling) return;
        if (!Application.isPlaying && !allowEditModeRun) return;  // no Edit-mode dispatch by default
#endif
        if (pausedDueToDispatchError && autoDisableOnError) return;

        EnsureComputeShader();
        if (!ResourcesReady())
            InitResourcesOnly();
        if (!flowCS) return;

        if (!kernelsReady)
        {
            if (!CacheKernelIDs()) return; // don’t spam logs; wait until next frame
        }

        ComputeGroups();
        if (groupsX <= 0 || groupsY <= 0) return;

        for (int s = 0; s < stepsPerFrame; s++)
            StepOnce();
    }

    // ---------- Init / Release ----------
    void InitResourcesOnly()
    {
        ResizeIfNeeded();
        MakeAccum();
        BuildKernel();
        ComputeGroups();
    }

    void Release()
    {
        SafeReleaseRT(ref stateA);
        SafeReleaseRT(ref stateB);
        SafeReleaseRT(ref affinityRT);

        if (kernelBuf != null) { kernelBuf.Release(); kernelBuf = null; }
        if (accumBuf  != null) { accumBuf.Release();  accumBuf  = null; }
    }

    bool ResourcesReady() => stateA && stateB && affinityRT;

    // ---------- Allocation helpers ----------
    void ResizeIfNeeded()
    {
        MakeRT(ref stateA);
        MakeRT(ref stateB);
        MakeRT(ref affinityRT);
        ComputeGroups();
    }

    void MakeRT(ref RenderTexture rt)
    {
        if (rt && rt.width == width && rt.height == height) return;

        if (rt)
        {
            if (RenderTexture.active == rt) RenderTexture.active = null;
            rt.Release();
        }

        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
    }

    void MakeAccum()
    {
        int len = Mathf.Max(1, width * height * Mathf.Max(1, channels));
        if (accumBuf != null && accumBuf.count == len) return;

        if (accumBuf != null) { accumBuf.Release(); accumBuf = null; }
        accumBuf = new ComputeBuffer(len, 4, ComputeBufferType.Structured); // uint stride = 4

        var zeros = new uint[len];
        accumBuf.SetData(zeros);
    }

    void SafeReleaseRT(ref RenderTexture rt)
    {
        if (!rt) return;
        if (RenderTexture.active == rt) RenderTexture.active = null;
        rt.Release();
        rt = null;
    }

    // ---------- Kernel (ringed convolution) ----------
    void BuildKernel()
    {
        int N = Mathf.Max(1, kernelSize * kernelSize);
        float[] K = new float[N];
        Vector2 center = new Vector2((kernelSize - 1) * 0.5f, (kernelSize - 1) * 0.5f);

        float maxPix = maxRadius * Mathf.Min(width, height);
        List<float> mus = new List<float>();
        for (int i = 1; i <= rings; i++) mus.Add(i * maxPix / (rings + 1));

        float sigmaPix = Mathf.Max(1e-4f, ringSigma * Mathf.Min(width, height));
        float sum = 0f;
        for (int y = 0; y < kernelSize; y++)
        for (int x = 0; x < kernelSize; x++)
        {
            Vector2 d = new Vector2(x - center.x, y - center.y);
            float r = d.magnitude;
            float v = 0f;
            for (int i = 0; i < mus.Count; i++)
            {
                float dif = r - mus[i];
                v += Mathf.Exp(-0.5f * (dif * dif) / (sigmaPix * sigmaPix));
            }
            int idx = y * kernelSize + x;
            K[idx] = v; sum += v;
        }
        if (sum > 1e-9f) { for (int i = 0; i < N; i++) K[i] /= sum; } else { System.Array.Clear(K, 0, N); }

        if (kernelBuf != null) { kernelBuf.Release(); kernelBuf = null; }
        kernelBuf = new ComputeBuffer(N, 4, ComputeBufferType.Structured); // float stride = 4
        kernelBuf.SetData(K);
    }

    // ---------- Seeding / Clear ----------
    void SeedCenterBlob()
    {
        var tmp = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
        var cols = new Color[width * height];
        int cx = width / 2, cy = height / 2;
        float rad = Mathf.Max(2f, Mathf.Min(width, height) * 0.08f);

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            float v = Mathf.Clamp01(1f - d / rad);
            cols[y * width + x] = new Color(v, v * 0.8f, v * 0.6f, 1f); // alpha=1
        }
        tmp.SetPixels(cols); tmp.Apply(false);
        Graphics.Blit(tmp, stateA);
        Graphics.Blit(tmp, stateB);
#if UNITY_EDITOR
        Object.DestroyImmediate(tmp);
#else
        Destroy(tmp);
#endif
    }

    void ClearRT(RenderTexture rt, Color c)
    {
        if (!rt) return;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, c);
        RenderTexture.active = prev;
    }

    // ---------- Kernel ID cache ----------
    bool CacheKernelIDs()
    {
        if (!flowCS) return false;

        try
        {
            // STRICT names matching FlowLenia.compute
            kAffinity = flowCS.FindKernel("Affinity");
            kClear    = flowCS.FindKernel("ClearAccum");
            kAdvect   = flowCS.FindKernel("AdvectScatter");
            kCollect  = flowCS.FindKernel("Collect");

            Debug.Log($"[FlowLenia] Kernel IDs — Affinity:{kAffinity} ClearAccum:{kClear} AdvectScatter:{kAdvect} Collect:{kCollect} (shader={flowCS.name})");
            kernelsReady = true;
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[FlowLenia] CacheKernelIDs failed on {flowCS.name}: {ex.Message}");
            kernelsReady = false;
            return false;
        }
    }

    // ---------- Binding ----------
    void BindCommon(int k)
    {
        flowCS.SetInt("_Width", width);
        flowCS.SetInt("_Height", height);
        flowCS.SetInt("_Channels", channels);
        flowCS.SetInt("_KernelSize", kernelSize);

        flowCS.SetFloat("_Dt", dt);
        flowCS.SetFloat("_FlowScale", flowScale);
        flowCS.SetFloat("_Temperature", temperature);

        flowCS.SetFloat("_Mcrit", Mcrit);
        flowCS.SetFloat("_Kappa", kappa);
        flowCS.SetFloat("_FixedScale", massToFixed);

        // NEW: bind reaction gain for Collect
        flowCS.SetFloat("_ReactionGain", reactionGain);
    }

    void BindAffinity(int k)
    {
        BindCommon(k);
        flowCS.SetTexture(k, "_State", stateA);
        flowCS.SetTexture(k, "_Affinity", affinityRT);
        if (kernelBuf != null) flowCS.SetBuffer(k, "_Kernel", kernelBuf);
        flowCS.SetFloat("_GrowthMu", growthMu);
        flowCS.SetFloat("_GrowthSigma", growthSigma);
    }

    void BindClearAccum(int k)
    {
        BindCommon(k);
        if (accumBuf != null) flowCS.SetBuffer(k, "_Accum", accumBuf);
    }

    void BindAdvectScatter(int k)
    {
        BindCommon(k);
        flowCS.SetTexture(k, "_State", stateA);
        flowCS.SetTexture(k, "_Affinity", affinityRT);
        if (accumBuf != null) flowCS.SetBuffer(k, "_Accum", accumBuf);
    }

    void BindCollect(int k)
    {
        BindCommon(k);
        if (accumBuf != null) flowCS.SetBuffer(k, "_Accum", accumBuf);
        flowCS.SetTexture(k, "_Next", stateB);
        flowCS.SetTexture(k, "_Affinity", affinityRT);   // <-- REQUIRED for reaction term
    }

    bool DispatchKernel(int kernelId, System.Action<int> binder, string friendlyName, bool required)
    {
        if (!flowCS)
        {
            if (required) Debug.LogError("[FlowLenia] No ComputeShader assigned.");
            return false;
        }
        if (kernelId < 0)
        {
            if (required) Debug.LogError($"[FlowLenia] Kernel '{friendlyName}' id<0; cache failed?");
            return false;
        }

        try
        {
            binder?.Invoke(kernelId);
            flowCS.Dispatch(kernelId, groupsX, groupsY, 1);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FlowLenia] Dispatch failed for '{friendlyName}' (id={kernelId}) groups=({groupsX},{groupsY}) on shader={flowCS.name}. Pausing. Reason: {ex.Message}");
            if (autoDisableOnError) pausedDueToDispatchError = true;
            kernelsReady = false; // recache next time if you resume
            return false;
        }
    }

    void ComputeGroups() { groupsX = (width + 15) / 16; groupsY = (height + 15) / 16; }

    // ---------- One simulation step ----------
    void StepOnce()
    {
        if (!flowCS) return;

        if (!stateA || stateA.width != width || stateA.height != height)
            InitResourcesOnly();

        // Dispatch the 4 strict kernels
        if (!DispatchKernel(kAffinity, BindAffinity, "Affinity", required: true)) return;
        DispatchKernel(kClear,        BindClearAccum, "ClearAccum", required: false);
        if (!DispatchKernel(kAdvect,  BindAdvectScatter, "AdvectScatter", required: true)) return;
        if (!DispatchKernel(kCollect, BindCollect, "Collect", required: true)) return;

        var tmp = stateA; stateA = stateB; stateB = tmp;
    }
}
