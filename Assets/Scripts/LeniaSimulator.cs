using System;
using UnityEngine;

public class LeniaSimulator : MonoBehaviour
{
    [Header("Resolution")] public int width = 512, height = 512;

    [Header("Physics / Rule")]
    [Range(1,64)] public int kernelRadius = 24;
    [Range(0f,1f)] public float mu    = 0.15f;
    [Range(0.001f,0.25f)] public float sigma = 0.016f;
    [Range(0f,2f)] public float beta  = 0.30f;
    [Range(0f,1f)] public float dt    = 0.10f;

    [Header("Kernel Profile")]
    public bool useRingKernel = true;
    [Range(0f,1f)] public float ringCenter = 0.50f;   // where the ring sits (0..1 of R)
    [Range(0.02f,0.50f)] public float ringWidth = 0.15f; // ring thickness

    [Header("Environment")]
    [Range(0f,1f)] public float envScale = 0.0f;
    public bool hardWalls = false;

    [Header("Run control")]
    [Min(1)] public int stepsPerFrame = 1;
    public bool paused = false;

    [Header("Assets")]
    public ComputeShader leniaCS;

    RenderTexture _stateA, _stateB, _envTex;
    Texture2D _kernelTex;
    int _kernUpdate;
    bool _initialized;

    public RenderTexture CurrentState => _stateA;
    public RenderTexture EnvTex => _envTex;

    void Awake(){ Application.targetFrameRate = 120; }

    void OnEnable()
    {
        InitTextures();
        BuildKernel();
        _kernUpdate = leniaCS.FindKernel("Update");
        _initialized = true;
    }

    void OnDisable(){ ReleaseAll(); }

    void Update()
    {
        if (!_initialized || paused) return;
        if (_stateA.width != width || _stateA.height != height) { ReleaseAll(); InitTextures(); BuildKernel(); }
        for (int i=0;i<stepsPerFrame;i++) StepOnce();
    }

    public void TogglePause() => paused = !paused;
    public void SetStepsPerFrame(int spf){ stepsPerFrame = Mathf.Max(1, spf); }

    public void ReSeedNoise(int seed = -1, float density = 0.08f)
    {
        UnityEngine.Random.InitState(seed<0?Environment.TickCount:seed);
        var tmp = new Texture2D(width, height, TextureFormat.RFloat, false, true);
        var data = new Color[width*height];
        for (int y=0;y<height;y++)
            for (int x=0;x<width;x++)
                data[y*width+x] = new Color(UnityEngine.Random.value < density ? UnityEngine.Random.value : 0f,0,0,0);
        tmp.SetPixels(data); tmp.Apply(false);
        Graphics.Blit(tmp, _stateA); Graphics.Blit(_stateA, _stateB);
        Destroy(tmp);
    }

    public void LoadSeedTexture(Texture2D tex)
    {
        if (!tex) return;
        Graphics.Blit(tex, _stateA); Graphics.Blit(_stateA, _stateB);
    }

    public void LoadEnvironment(Texture2D envMask)
    {
        if (_envTex == null) _envTex = AllocRT();
        if (envMask) Graphics.Blit(envMask, _envTex);
    }

    public void ApplyPreset(float mu, float sigma, int R, float beta, float dt)
    {
        this.mu = mu; this.sigma = sigma; this.kernelRadius = R; this.beta = beta; this.dt = dt;
        BuildKernel();
    }

    void StepOnce()
    {
        int tw = _stateA.width, th = _stateA.height;

        leniaCS.SetTexture(_kernUpdate, "_StateIn",  _stateA);
        leniaCS.SetTexture(_kernUpdate, "_StateOut", _stateB);
        leniaCS.SetTexture(_kernUpdate, "_KernelTex", _kernelTex);
        if (_envTex != null) leniaCS.SetTexture(_kernUpdate, "_EnvTex", _envTex);

        leniaCS.SetInt("_Width",  tw);
        leniaCS.SetInt("_Height", th);
        leniaCS.SetInt("_KernelRadius", kernelRadius);
        leniaCS.SetFloat("_Mu", mu);
        leniaCS.SetFloat("_Sigma", Mathf.Max(1e-4f, sigma));
        leniaCS.SetFloat("_Beta", beta);
        leniaCS.SetFloat("_Dt", dt);
        leniaCS.SetFloat("_EnvScale", envScale);
        leniaCS.SetFloat("_HardWall", hardWalls?1f:0f);

        leniaCS.Dispatch(_kernUpdate, Mathf.CeilToInt(tw/8f), Mathf.CeilToInt(th/8f), 1);
        (_stateA, _stateB) = (_stateB, _stateA);
    }

    void InitTextures()
    {
        _stateA = AllocRT();
        _stateB = AllocRT();
        _envTex = AllocRT();
        ClearRT(_stateA, 0f);
        ClearRT(_stateB, 0f);
        ClearRT(_envTex, 1f); // neutral env
        ReSeedNoise();
    }

    RenderTexture AllocRT()
    {
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true; rt.autoGenerateMips = false; rt.Create();
        return rt;
    }

    void ClearRT(RenderTexture rt, float value)
    {
        var tmp = new Texture2D(1,1,TextureFormat.RFloat,false,true);
        tmp.SetPixel(0,0,new Color(value,0,0,0)); tmp.Apply();
        Graphics.Blit(tmp, rt); Destroy(tmp);
    }

    void BuildKernel()
    {
        int R = Mathf.Clamp(kernelRadius,1,64);
        int size = 2*R+1;
        if (_kernelTex != null && (_kernelTex.width!=size || _kernelTex.height!=size))
        { Destroy(_kernelTex); _kernelTex = null; }
        if (_kernelTex == null)
            _kernelTex = new Texture2D(size, size, TextureFormat.RFloat, false, true);

        float sum=0f; var data = new Color[size*size];
        for (int y=0;y<size;y++)
        {
            for (int x=0;x<size;x++)
            {
                float dx = x - R; float dy = y - R;
                float r = Mathf.Sqrt(dx*dx + dy*dy) / R; // 0..1
                float w;
                if (useRingKernel)
                {
                    float c = Mathf.Clamp01(ringCenter);
                    float s = Mathf.Max(0.01f, ringWidth);
                    w = Mathf.Exp(-0.5f * (r - c)*(r - c) / (s*s));
                }
                else
                {
                    w = Mathf.Exp(-4f * r*r); // center bump
                }
                if (r>1f) w = 0f;
                data[y*size + x] = new Color(w,0,0,0);
                sum += w;
            }
        }
        float inv = sum>1e-6f ? 1f/sum : 1f;
        for (int i=0;i<data.Length;i++){ data[i].r *= inv; }
        _kernelTex.SetPixels(data); _kernelTex.Apply(false);
    }

    void ReleaseAll()
    {
        if (_stateA) _stateA.Release();
        if (_stateB) _stateB.Release();
        if (_envTex) _envTex.Release();
        if (_kernelTex) Destroy(_kernelTex);
    }
}
