]using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LeniaParams", menuName = "Lenia/Params", order = 0)]
public class LeniaParams : ScriptableObject
{
    [Header("Field & Time")]
    [Min(8)] public int width = 1024;
    [Min(8)] public int height = 1024;
    [Range(1,4)] public int channels = 1;      // up to float4
    [Min(0.0001f)] public float dt = 0.1f;     // Δt
    [Min(1)] public int stepsPerFrame = 1;
    public bool wrap = true;                   // periodic vs clamp boundaries

    [Header("Growth (per channel)")]
    public Vector4 mu = new Vector4(0.15f, 0.15f, 0.15f, 0.15f);
    public Vector4 sigma = new Vector4(0.015f, 0.015f, 0.015f, 0.015f);

    [Header("Kernels")]
    [Tooltip("Base radius in pixels; per-kernel relative radii scale this.")]
    [Range(4, 256)] public int baseRadius = 25;
    public List<KernelSpec> kernels = new List<KernelSpec> {
        new KernelSpec { name="K1", relativeRadius=1f, weight=1f, beta=new float[]{1f, 0.66f, 0.33f}, rank=3 }
    };

    [Header("Channel coupling (rows: out c, cols: in c')")]
    public Matrix4x4 coupling = Matrix4x4.identity; // m_c = K * sum_{c'} C[c,c'] A_{c'}

    [Header("Seeding")]
    public bool randomSeed = false;
    [Range(0,1)] public float randomFill = 0.0f;   // 0 = none, up to light noise
    public int gaussianStamps = 1;                 // number of gaussian blobs
    [Range(2,128)] public int stampRadius = 24;
}

[Serializable]
public class KernelSpec
{
    public string name = "Kernel";
    [Range(0.1f, 4f)] public float relativeRadius = 1f; // r_k (relative to baseRadius)
    [Min(1)] public int rank = 3;                       // B
    public float[] beta = new float[] { 1f, 0.66f, 0.33f }; // β peaks, size = rank
    [Min(0)] public float weight = 1f;                  // h_k
    [Range(0.1f, 8f)] public float alpha = 4f;          // KC sharpness (paper uses 4)
}