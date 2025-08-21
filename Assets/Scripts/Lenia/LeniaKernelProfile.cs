using UnityEngine;

[CreateAssetMenu(menuName="Lenia/Kernel Profile")]
public class LeniaKernelProfile : ScriptableObject
{
    [Range(4, 64)] public int radius = 24;
    [Min(1)] public int ringCount = 1;

    [Tooltip("Means (0..1) per ring, 0=center, 1=outer edge")]
    public float[] means = new float[] { 0.5f };

    [Tooltip("Peak weights per ring (will be normalized)")]
    public float[] peaks = new float[] { 1f };

    [Tooltip("Stddev per ring (suggest 0.03..0.2)")]
    public float[] stddevs = new float[] { 0.08f };

    Texture2D _kernelTex;

    void OnValidate()
    {
        if (means == null || means.Length != ringCount) System.Array.Resize(ref means, ringCount);
        if (peaks == null || peaks.Length != ringCount) System.Array.Resize(ref peaks, ringCount);
        if (stddevs == null || stddevs.Length != ringCount) System.Array.Resize(ref stddevs, ringCount);
    }

    public Texture2D GetOrBuildKernelTexture()
    {
        if (_kernelTex != null) return _kernelTex;

        int size = radius * 2 + 1;
        _kernelTex = new Texture2D(size, size, TextureFormat.RFloat, false, true);
        _kernelTex.wrapMode = TextureWrapMode.Clamp;
        _kernelTex.filterMode = FilterMode.Point;

        var data = new float[size * size];
        double sum = 0.0;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - radius, dy = y - radius;
            float r01 = Mathf.Sqrt(dx*dx + dy*dy) / Mathf.Max(1, radius); // 0..1
            double val = 0.0;
            for (int i = 0; i < ringCount; i++)
            {
                float m = Mathf.Clamp01(means[i]);
                float s = Mathf.Max(1e-4f, stddevs[i]);
                float p = Mathf.Max(0f, peaks[i]);
                double g = System.Math.Exp(-0.5 * ((r01 - m) * (r01 - m)) / (s * s));
                val += p * g;
            }
            data[y * size + x] = (float)val;
            sum += val;
        }
        // normalize kernel so sum(K) == number of pixels (keeps growth scale stable)
        float norm = (float)sum;
        if (norm > 1e-8f) { for (int i = 0; i < data.Length; i++) data[i] /= norm; }
else { for (int i = 0; i < data.Length; i++) data[i] = 0f; data[data.Length/2] = 1f; }

        _kernelTex.SetPixelData(data, 0);
        _kernelTex.Apply(false, true);
        return _kernelTex;
    }

    public void EnsureRingCount(int n)
    {
        ringCount = Mathf.Max(1, n);
        System.Array.Resize(ref means,   ringCount);
        System.Array.Resize(ref peaks,   ringCount);
        System.Array.Resize(ref stddevs, ringCount);
        Invalidate();
    }

    public void Invalidate() { _kernelTex = null; }
}


