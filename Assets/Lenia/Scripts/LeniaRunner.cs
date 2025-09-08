using UnityEngine;

[ExecuteAlways]
public class LeniaRunner : MonoBehaviour
{
    public LeniaParams parameters;
    public ComputeShader shader;
    public Material displayMat;  // set to PaletteBlit or BlitRFloat
    public bool autoRebuildKernels = true;

    RenderTexture[] buffers; // ping-pong
    Texture2DArray kernelArray;
    ComputeBuffer wBuf, cBuf;

    int kMain;
    int radiusMax, kernelSize;
    float[] wNorm;
    bool initialized;

    void OnEnable() { Init(); }
    void OnDisable() { ReleaseAll(); }

    void Init()
    {
        if (initialized) return;
        if (!parameters || !shader) return;

        kMain = shader.FindKernel("ConvolveGrowUpdate");

        // Allocate field (float / float4)
        buffers = new RenderTexture[2];
        var fmt = (parameters.channels == 1) ? RenderTextureFormat.RFloat : RenderTextureFormat.ARGBFloat;

        for (int i = 0; i < 2; i++)
        {
            buffers[i] = new RenderTexture(parameters.width, parameters.height, 0, fmt)
            { enableRandomWrite = true, wrapMode = parameters.wrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            buffers[i].Create();
        }

        if (autoRebuildKernels || kernelArray == null)
            kernelArray = KernelBuilder.BuildKernelArray(parameters, out radiusMax, out kernelSize, out wNorm);

        UploadParams();
        SeedField();
        if (displayMat) displayMat.SetTexture("_MainTex", buffers[0]);
        initialized = true;
    }

    void UploadParams()
    {
        // weights (normalized)
        wBuf?.Dispose();
        wBuf = new ComputeBuffer(parameters.kernels.Count, sizeof(float));
        wBuf.SetData(wNorm);

        // coupling 4 rows
        var rows = new Vector4[4] { parameters.coupling.GetRow(0), parameters.coupling.GetRow(1), parameters.coupling.GetRow(2), parameters.coupling.GetRow(3) };
        cBuf?.Dispose();
        cBuf = new ComputeBuffer(4, sizeof(float) * 4);
        cBuf.SetData(rows);

        // bind
        shader.SetTexture(kMain, "_KernelTex", kernelArray);
        shader.SetBuffer(kMain, "_KernelWeights", wBuf);
        shader.SetBuffer(kMain, "_CoupleRows", cBuf);

        shader.SetInt("_Width",  parameters.width);
        shader.SetInt("_Height", parameters.height);
        shader.SetInt("_RadiusMax", radiusMax);
        shader.SetInt("_KernelSize", kernelSize);
        shader.SetInt("_NumKernels", Mathf.Max(1, parameters.kernels.Count));
        shader.SetInt("_Channels", parameters.channels);
        shader.SetInt("_Wrap", parameters.wrap ? 1 : 0);
        shader.SetFloat("_Dt", parameters.dt);
        shader.SetVector("_Mu", parameters.mu);
        shader.SetVector("_Sigma", parameters.sigma);
    }

    void SeedField()
    {
        // start clear
        Graphics.Blit(Texture2D.blackTexture, buffers[0]);
        Graphics.Blit(Texture2D.blackTexture, buffers[1]);

        if (parameters.randomSeed && parameters.randomFill > 0f)
        {
            // cheap noise
            var tmp = new Texture2D(parameters.width, parameters.height, TextureFormat.RGBAFloat, false, true);
            var cols = new Color[parameters.width * parameters.height];
            for (int i = 0; i < cols.Length; i++)
            {
                float v = (Random.value < parameters.randomFill) ? Random.value : 0f;
                cols[i] = (parameters.channels == 1) ? new Color(v,0,0,1) : new Color(v,v,v,1);
            }
            tmp.SetPixels(cols); tmp.Apply();
            Graphics.Blit(tmp, buffers[0]);
            Graphics.Blit(tmp, buffers[1]);
            DestroyImmediate(tmp);
        }

        // gaussian stamps
        if (parameters.gaussianStamps > 0)
        {
            var stamp = MakeGaussianStamp(parameters.stampRadius, parameters.channels);
            var mat = new Material(Shader.Find("Hidden/BlitRFloat"));
            for (int s = 0; s < parameters.gaussianStamps; s++)
            {
                Graphics.Blit(stamp, buffers[0], mat);
                Graphics.Blit(stamp, buffers[1], mat);
            }
            DestroyImmediate(stamp);
            DestroyImmediate(mat);
        }
    }

    static Texture2D MakeGaussianStamp(int r, int channels)
    {
        int sz = Mathf.Max(8, 2*r+1);
        var tex = new Texture2D(sz, sz, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Bilinear };
        int cx = sz/2, cy = sz/2;
        for (int y=0; y<sz; y++)
        for (int x=0; x<sz; x++)
        {
            float dx = (x - cx), dy = (y - cy);
            float d = Mathf.Sqrt(dx*dx+dy*dy);
            float v = Mathf.Exp(-0.5f * (d*d) / (r*r));
            var c = (channels==1) ? new Color(v,0,0,1) : new Color(v,v,v,1);
            tex.SetPixel(x,y,c);
        }
        tex.Apply();
        return tex;
    }

    void ReleaseAll()
    {
        if (buffers != null) foreach (var rt in buffers) if (rt) rt.Release();
        if (kernelArray) DestroyImmediate(kernelArray);
        wBuf?.Dispose(); cBuf?.Dispose();
        initialized = false;
    }

    void Update()
    {
        if (!initialized) { Init(); if (!initialized) return; }

        // push dynamic params
        shader.SetInt("_Wrap", parameters.wrap ? 1 : 0);
        shader.SetFloat("_Dt", parameters.dt);
        shader.SetVector("_Mu", parameters.mu);
        shader.SetVector("_Sigma", parameters.sigma);

        for (int step = 0; step < parameters.stepsPerFrame; step++)
        {
            int src = step & 1, dst = 1 - src;

            shader.SetTexture(kMain, "_Src", buffers[src]);
            shader.SetTexture(kMain, "_Dst", buffers[dst]);

            uint tx, ty, tz; shader.GetKernelThreadGroupSizes(kMain, out tx, out ty, out tz);
            int gx = Mathf.CeilToInt(parameters.width  / (float)tx);
            int gy = Mathf.CeilToInt(parameters.height / (float)ty);
            shader.Dispatch(kMain, gx, gy, 1);

            if (displayMat) displayMat.SetTexture("_MainTex", buffers[dst]);

            // swap ping-pong
            var tmp = buffers[src]; buffers[src] = buffers[dst]; buffers[dst] = tmp;
        }
    }
}