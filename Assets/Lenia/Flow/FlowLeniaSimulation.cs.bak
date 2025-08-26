using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class FlowLeniaSimulation : MonoBehaviour
{
    [Header("Resolution & Channels")]
    public int width = 512, height = 512;
    [Range(1,3)] public int channels = 3;

    [Header("Lenia Kernel (rings)")]
    [Range(3,31)] public int kernelSize = 17; // odd
    [Range(1,6)]  public int rings = 3;
    [Range(0.02f, 0.8f)] public float maxRadius = 0.35f; // in grid fraction
    [Range(0.02f, 0.25f)] public float ringSigma = 0.06f;

    [Header("Growth (affinity)")]
    [Range(0,1)]   public float growthMu = 0.15f;
    [Range(0.005f,0.5f)] public float growthSigma = 0.035f;

    [Header("Flow & Mass")]
    [Range(0.1f, 2.5f)] public float dt = 0.9f;
    [Range(0.25f, 3.0f)] public float flowScale = 1.0f;
    public Vector2 massWeight = new Vector2(0.6f, 1.2f);
    public float massToFixed = 1_000_000f;

    [Header("Steps")]
    [Range(1, 20)] public int stepsPerFrame = 2;

    [Header("Assets")]
    public ComputeShader flowCS;

    public RenderTexture CurrentTexture => stateA;

    RenderTexture stateA, stateB, affinityRT;
    ComputeBuffer kernelBuf, accumBuf;
    int kAffinity, kAdvect, kCollect;

    void OnEnable() { EnsureComputeShader(); Init(); }
    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }

#if UNITY_EDITOR
    void EnsureComputeShader()
    {
        if (flowCS == null)
        {
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lenia/Flow/FlowLenia.compute");
            if (cs) flowCS = cs;
        }
    }
#else
    void EnsureComputeShader() {}
#endif

    // Exposed so viewer can demand-init if texture is null
    public void EnsureInitialized()
    {
        if (stateA == null || stateB == null || affinityRT == null) Init();
    }

    void Init()
    {
        // Always create the RenderTextures and seed something visible.
        MakeRT(ref stateA);
        MakeRT(ref stateB);
        MakeRT(ref affinityRT);

        MakeAccum();
        BuildKernel();
        SeedCenterBlob();

        // Only fetch kernels if we have a compute shader
        if (flowCS != null)
        {
            kAffinity = flowCS.FindKernel("Affinity");
            kAdvect   = flowCS.FindKernel("AdvectScatter");
            kCollect  = flowCS.FindKernel("Collect");
        }
    }

    void MakeRT(ref RenderTexture rt)
    {
        if (rt != null && rt.width == width && rt.height == height) return;
        if (rt != null) { if (RenderTexture.active == rt) RenderTexture.active = null; rt.Release(); }
        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true; rt.filterMode = FilterMode.Point; rt.wrapMode = TextureWrapMode.Clamp; rt.Create();
    }

    void MakeAccum()
    {
        int len = Mathf.Max(1, width * height * Mathf.Max(1,channels));
        accumBuf?.Release();
        accumBuf = new ComputeBuffer(len, sizeof(uint));
        var zeros = new uint[len];
        accumBuf.SetData(zeros);
    }

    void BuildKernel()
    {
        int N = Mathf.Max(1, kernelSize * kernelSize);
        float[] K = new float[N];
        Vector2 center = new Vector2((kernelSize-1)*0.5f, (kernelSize-1)*0.5f);

        float maxR = maxRadius * Mathf.Min(width, height);
        List<float> mus = new List<float>();
        for (int i = 1; i <= rings; i++) mus.Add(i * maxR / (rings + 1));

        float sigmaPix = Mathf.Max(1e-4f, ringSigma * Mathf.Min(width, height));
        float sum = 0f;
        for (int y = 0; y < kernelSize; y++)
        for (int x = 0; x < kernelSize; x++)
        {
            Vector2 dx = new Vector2(x - center.x, y - center.y);
            float r = dx.magnitude;
            float v = 0f;
            foreach (var mu in mus) { float d = (r - mu); v += Mathf.Exp(-0.5f * (d*d) / (sigmaPix*sigmaPix)); }
            K[y*kernelSize + x] = v; sum += v;
        }
        for (int i = 0; i < N; i++) K[i] = (sum>1e-9f)? K[i]/sum : 0f;

        kernelBuf?.Release();
        kernelBuf = new ComputeBuffer(N, sizeof(float));
        kernelBuf.SetData(K);
    }

    void SeedCenterBlob()
    {
        var tmp = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
        var cols = new Color[width*height];
        int cx = width/2, cy = height/2;
        float rad = Mathf.Max(2f, Mathf.Min(width, height) * 0.08f);
        for (int y=0;y<height;y++)
        for (int x=0;x<width;x++)
        {
            float d = Vector2.Distance(new Vector2(x,y), new Vector2(cx,cy));
            float v = Mathf.Clamp01(1f - d/rad);
            cols[y*width+x] = new Color(v, v*0.8f, v*0.6f, 0);
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

    void SafeReleaseRT(ref RenderTexture rt)
    {
        if (rt != null)
        {
            if (RenderTexture.active == rt) RenderTexture.active = null;
            rt.Release();
            rt = null;
        }
    }

    void Release()
    {
        SafeReleaseRT(ref stateA);
        SafeReleaseRT(ref stateB);
        SafeReleaseRT(ref affinityRT);
        kernelBuf?.Release(); kernelBuf = null;
        accumBuf?.Release();  accumBuf  = null;
    }

    void Update()
    {
        EnsureComputeShader();
        if (stateA == null || stateB == null || affinityRT == null || kernelBuf == null || accumBuf == null)
            Init();

        if (flowCS == null) return; // show the seeded texture even without stepping

        for (int s = 0; s < stepsPerFrame; s++) StepOnce();
    }

    void SetCommon(int k)
    {
        flowCS.SetInt("_Width", width);
        flowCS.SetInt("_Height", height);
        flowCS.SetInt("_Channels", channels);
        flowCS.SetInt("_KernelSize", kernelSize);
        flowCS.SetInt("_HalfK", kernelSize/2);
        flowCS.SetInt("_Stride", width * channels);

        flowCS.SetFloat("_GrowthMu", growthMu);
        flowCS.SetFloat("_GrowthSigma", growthSigma);

        flowCS.SetFloat("_Dt", dt);
        flowCS.SetFloat("_FlowScale", flowScale);
        flowCS.SetFloat("_MassToFix", massToFixed);
        flowCS.SetFloat("_M0", massWeight.x);
        flowCS.SetFloat("_M1", massWeight.y);
    }

    void StepOnce()
    {
        if (flowCS == null) return;
        int gx = (width + 7)/8, gy = (height + 7)/8;

        SetCommon(kAffinity);
        flowCS.SetTexture(kAffinity, "_StateRead", stateA);
        flowCS.SetTexture(kAffinity, "_Affinity", affinityRT);
        flowCS.SetBuffer(kAffinity, "_Kernel", kernelBuf);
        flowCS.Dispatch(kAffinity, gx, gy, 1);

        SetCommon(kAdvect);
        flowCS.SetTexture(kAdvect, "_StateRead", stateA);
        flowCS.SetTexture(kAdvect, "_Affinity", affinityRT);
        flowCS.SetBuffer(kAdvect, "_Accum", accumBuf);
        flowCS.Dispatch(kAdvect, gx, gy, 1);

        SetCommon(kCollect);
        flowCS.SetTexture(kCollect, "_StateWrite", stateB);
        flowCS.SetBuffer(kCollect, "_Accum", accumBuf);
        flowCS.Dispatch(kCollect, gx, gy, 1);

        (stateA, stateB) = (stateB, stateA);
    }
}
