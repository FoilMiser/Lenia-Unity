// Assets/Lenia/Flow/FlowLeniaSimulation.cs
using UnityEngine;
using System.Collections.Generic;

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
    [Tooltip("Smoothstep lower/upper for mass weight M(m)")]
    public Vector2 massWeight = new Vector2(0.6f, 1.2f);
    [Tooltip("Fixed-point scaling for atomic adds")]
    public float massToFixed = 1_000_000f;

    [Header("Steps")]
    [Range(1, 20)] public int stepsPerFrame = 2;

    [Header("Assets")]
    public ComputeShader flowCS;

    // Public RT you can plug into your UI/Renderer
    public RenderTexture CurrentTexture => stateA;

    // Internals
    RenderTexture stateA, stateB, affinityRT;
    ComputeBuffer kernelBuf, accumBuf;
    int kAffinity, kAdvect, kCollect;

    int halfK => kernelSize/2;
    int stride => width * channels;

    void OnEnable() { Init(); }
    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }

    void Init()
    {
        if (flowCS == null) return;
        kAffinity = flowCS.FindKernel("Affinity");
        kAdvect   = flowCS.FindKernel("AdvectScatter");
        kCollect  = flowCS.FindKernel("Collect");

        MakeRT(ref stateA);
        MakeRT(ref stateB);
        MakeRT(ref affinityRT);

        MakeAccum();
        BuildKernel();
        SeedCenterBlob();
    }

    void MakeRT(ref RenderTexture rt)
    {
        if (rt != null && rt.width == width && rt.height == height) return;
        if (rt != null) rt.Release();

        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
    }

    void MakeAccum()
    {
        int len = width * height * channels;
        accumBuf?.Release();
        accumBuf = new ComputeBuffer(len, sizeof(uint));
        // zero-initialize
        var zeros = new uint[len];
        accumBuf.SetData(zeros);
    }

    void BuildKernel()
    {
        // 2D radial kernel: sum of Gaussian rings
        int N = kernelSize * kernelSize;
        float[] K = new float[N];
        Vector2 center = new Vector2((kernelSize-1)*0.5f, (kernelSize-1)*0.5f);

        float maxR = maxRadius * Mathf.Min(width, height); // scale to pixels
        int R = rings;
        List<float> mus = new List<float>();
        for (int i = 1; i <= R; i++)
            mus.Add(i * maxR / (R + 1));

        float sigmaPix = ringSigma * Mathf.Min(width, height);
        float sum = 0f;
        for (int y = 0; y < kernelSize; y++)
        for (int x = 0; x < kernelSize; x++)
        {
            Vector2 dx = new Vector2(x - center.x, y - center.y);
            float r = dx.magnitude;
            float v = 0f;
            foreach (var mu in mus)
            {
                float d = (r - mu);
                v += Mathf.Exp(-0.5f * (d*d) / (sigmaPix*sigmaPix));
            }
            K[y*kernelSize + x] = v;
            sum += v;
        }
        // Normalize kernel sum to 1.0
        for (int i = 0; i < N; i++) K[i] /= Mathf.Max(1e-6f, sum);

        kernelBuf?.Release();
        kernelBuf = new ComputeBuffer(N, sizeof(float));
        kernelBuf.SetData(K);
    }

    void SeedCenterBlob()
    {
        // simple centered patch
        var tmp = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
        var cols = new Color[width*height];
        int cx = width/2, cy = height/2;
        float rad = Mathf.Min(width, height) * 0.08f;
        for (int y=0;y<height;y++)
        for (int x=0;x<width;x++)
        {
            float d = Vector2.Distance(new Vector2(x,y), new Vector2(cx,cy));
            float v = Mathf.Clamp01(1f - d/rad);
            // slightly different per channel
            cols[y*width+x] = new Color(v, v*0.8f, v*0.6f, 0);
        }
        tmp.SetPixels(cols); tmp.Apply(false);

        Graphics.Blit(tmp, stateA);
        Graphics.Blit(tmp, stateB);
        tmp.Apply(false);
        DestroyImmediate(tmp);
    }

    void Release()
    {
        stateA?.Release(); stateA = null;
        stateB?.Release(); stateB = null;
        affinityRT?.Release(); affinityRT = null;
        kernelBuf?.Release(); kernelBuf = null;
        accumBuf?.Release(); accumBuf = null;
    }

    void Update()
    {
        if (flowCS == null) return;
        if (stateA == null || stateB == null || affinityRT == null || kernelBuf == null || accumBuf == null)
            Init();

        for (int s = 0; s < stepsPerFrame; s++)
            StepOnce();
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
        int gx = (width + 7)/8, gy = (height + 7)/8;

        // Affinity
        SetCommon(kAffinity);
        flowCS.SetTexture(kAffinity, "_StateRead", stateA);
        flowCS.SetTexture(kAffinity, "_Affinity", affinityRT);
        flowCS.SetBuffer(kAffinity, "_Kernel", kernelBuf);
        flowCS.Dispatch(kAffinity, gx, gy, 1);

        // Advect (scatter into _Accum)
        SetCommon(kAdvect);
        flowCS.SetTexture(kAdvect, "_StateRead", stateA);
        flowCS.SetTexture(kAdvect, "_Affinity", affinityRT);
        flowCS.SetBuffer(kAdvect, "_Accum", accumBuf);
        flowCS.Dispatch(kAdvect, gx, gy, 1);

        // Collect (write new stateB and zero _Accum)
        SetCommon(kCollect);
        flowCS.SetTexture(kCollect, "_StateWrite", stateB);
        flowCS.SetBuffer(kCollect, "_Accum", accumBuf);
        flowCS.Dispatch(kCollect, gx, gy, 1);

        // ping-pong
        (stateA, stateB) = (stateB, stateA);
    }
}

