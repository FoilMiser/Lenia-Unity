using UnityEngine;
using System;
using System.Collections.Generic;

public static class KernelBuilder
{
    // Build a Texture2DArray with size = (2*Rmax+1)^2, slices = nk, each slice normalized to sum=1
    public static Texture2DArray BuildKernelArray(LeniaParams lp, out int radiusMax, out int kernelSize, out float[] normWeights)
    {
        radiusMax = 0;
        foreach (var k in lp.kernels)
            radiusMax = Mathf.Max(radiusMax, Mathf.RoundToInt(lp.baseRadius * k.relativeRadius));
        kernelSize = 2 * radiusMax + 1;

        int nk = Mathf.Max(1, lp.kernels.Count);
        var texArray = new Texture2DArray(kernelSize, kernelSize, nk, TextureFormat.RFloat, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        // normalize kernel weights h_k
        normWeights = new float[nk];
        float hsum = 0f; for (int i = 0; i < nk; i++) hsum += Mathf.Max(0f, lp.kernels[i].weight);
        for (int i = 0; i < nk; i++) normWeights[i] = (hsum > 0f) ? Mathf.Max(0f, lp.kernels[i].weight) / hsum : 1f / nk;

        // build each kernel slice
        for (int s = 0; s < nk; s++)
        {
            var spec = lp.kernels[s];
            int Rk = Mathf.RoundToInt(lp.baseRadius * spec.relativeRadius);
            if (Rk < 1) Rk = 1;

            float sum = 0f;
            float[,] buf = new float[kernelSize, kernelSize];
            for (int y = -radiusMax; y <= radiusMax; y++)
            for (int x = -radiusMax; x <= radiusMax; x++)
            {
                // r normalized to [0,1] by this kernel's radius
                float r = Mathf.Sqrt(x * x + y * y) / (float)Rk;
                float v = KernelShell(r, spec.beta, spec.alpha);
                buf[x + radiusMax, y + radiusMax] = v;
                sum += v;
            }

            // normalize so sum ~ 1
            for (int y = 0; y < kernelSize; y++)
            for (int x = 0; x < kernelSize; x++)
            {
                float v = (sum > 0f) ? buf[x, y] / sum : 0f;
                texArray.SetPixel(x, y, s, new Color(v, 0, 0, 1));
            }
        }

        texArray.Apply(false, true);
        return texArray;
    }

    // Kernel shell KS(r; β) = β[floor(B r)] * KC(B r mod 1), with exponential KC
    static float KernelShell(float r, float[] beta, float alpha)
    {
        if (r <= 0f || r >= 1f || beta == null || beta.Length == 0) return 0f;
        int B = Mathf.Max(1, beta.Length);
        float Br = B * r;
        int idx = Mathf.Clamp(Mathf.FloorToInt(Br), 0, B - 1);
        float frac = Br - Mathf.Floor(Br);
        float kc = KernelCoreExp(frac, alpha);
        return beta[idx] * kc;
    }

    // Exponential "bump" kernel core (paper default). Safe-guards at edges.
    static float KernelCoreExp(float r, float alpha)
    {
        if (r <= 0f || r >= 1f) return 0f;
        float denom = 4f * r * (1f - r);
        float t = alpha - (alpha / Mathf.Max(denom, 1e-6f));
        return Mathf.Exp(t);
    }
}