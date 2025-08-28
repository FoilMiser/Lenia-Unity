using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="FlowLeniaModel", menuName="Lenia/Flow Lenia Model")]
public class FlowLeniaModel : ScriptableObject
{
    [Header("Model (free-kernel schema)")]
    [Tooltip("Kernel radius (R)")]
    public int R = 7;
    public int T = 1;   // informational parity
    public int P = 1;   // informational parity
    public int kn = 1;  // kernel count
    public int gn = 1;  // growth count (unused)

    [Header("Kernels (self & cross)")]
    public List<FlowKernel> kernels = new List<FlowKernel>();
}

[Serializable]
public class FlowKernel
{
    [Range(0,2)] public int c0 = 0;   // source channel
    [Range(0,2)] public int c1 = 0;   // target channel

    [Header("Growth (μ, σ, gain h)")]
    public float mu = 0.15f;
    public float sigma = 0.015f;
    public float h = 1.0f;

    [Header("Rings (free-kernel)")]
    public List<FlowRing> rings = new List<FlowRing>(); // {r (outer radius), w (width), b[] bins}
}

[Serializable]
public class FlowRing
{
    [Tooltip("Outer radius of ring (<= R)")]
    public int r = 1;
    [Tooltip("Ring width (px)")]
    public int w = 1;
    [Tooltip("Piecewise-constant weights across the ring span")]
    public List<float> b = new List<float>(){1f};
}

// ---- JSON DTOs for importer ----
[Serializable] public class FlowLeniaJsonRoot  { public FlowLeniaJsonModel model; public FlowLeniaJsonParams[] @params; }
[Serializable] public class FlowLeniaJsonModel { public int R, T, P, kn, gn; }
[Serializable] public class FlowLeniaJsonParams {
    public float m, s, h;
    public int c0, c1;
    public FlowRing[] rings;
    public int c; // legacy single 'c' -> upgrade to c0=c1=c
}
