using System.Reflection;
using UnityEngine;

[System.Serializable]
public class FlowLeniaPreset
{
    public string id = "custom";
    public string title = "Custom";

    // Kernel radius (size becomes 2*R+1 if kernelSize is present)
    public int R = 13;

    [Range(0, 1)] public float mu = 0.15f;
    [Min(1e-6f)]  public float sigma = 0.015f;
    public float h = 1.0f;

    [Header("Dynamics")]
    [Min(0.01f)] public float dt = 0.45f;
    public float alpha = 1.0f;
    public float beta  = 0.25f;
    [Min(0)] public float temperature = 0.9f;
    [Min(0)] public float maxSpeed    = 1.0f;
    public BoundaryMode boundary = BoundaryMode.Wrap;
    public ScatterMode  scatter  = ScatterMode.Bilinear;

    public void ApplyTo(FlowLeniaSimulation sim)
    {
        if (!sim) return;

        // Always apply dynamics that the sim exposes publicly
        sim.dt          = dt;
        sim.alpha       = alpha;
        sim.beta        = beta;
        sim.temperature = temperature;
        sim.maxSpeed    = maxSpeed;
        sim.boundary    = boundary;
        sim.scatterMode = scatter;

        // Prefer a 'model' object if present (works with original setup)
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var modelField = sim.GetType().GetField("model", flags);
        var model = modelField != null ? modelField.GetValue(sim) : null;

        if (model != null)
        {
            // Set radius
            var tModel = model.GetType();
            var fR = tModel.GetField("R", flags);
            if (fR != null) fR.SetValue(model, Mathf.Max(1, R));

            // Try to find first kernel in the model and set mu/sigma/h
            var fKernels = tModel.GetField("kernels", flags) ?? tModel.GetField("Kernels", flags);
            var kernels = fKernels != null ? fKernels.GetValue(model) as System.Collections.IList : null;
            if (kernels != null && kernels.Count > 0 && kernels[0] != null)
            {
                var k  = kernels[0];
                var tk = k.GetType();
                SetFloatFieldIfExists(k, "mu",    mu,    flags);
                SetFloatFieldIfExists(k, "sigma", Mathf.Max(1e-6f, sigma), flags);
                SetFloatFieldIfExists(k, "h",     h,     flags);
            }

            // Ask the sim to rebuild if those methods exist
            CallIfExists(sim, "CacheKernels");
            CallIfExists(sim, "EnsureKernelTexture");
            CallIfExists(sim, "AllocateIfNeeded", true);
            return;
        }

        // Fallback: try to set fields directly on the sim if they exist in this variant
        // (no compile-time references — reflection only)
        SetIntFieldIfExists(sim, "kernelSize", Mathf.Max(3, (2 * R + 1) | 1), flags);
        SetIntFieldIfExists(sim, "R",          Mathf.Max(1, R),               flags);
        SetFloatFieldIfExists(sim, "mu",       mu,                            flags);
        SetFloatFieldIfExists(sim, "sigma",    Mathf.Max(1e-6f, sigma),       flags);
        SetFloatFieldIfExists(sim, "h",        h,                             flags);

        // Rebuild if helpers exist
        CallIfExists(sim, "CacheKernels");
        CallIfExists(sim, "EnsureKernelTexture");
        CallIfExists(sim, "AllocateIfNeeded", true);
    }

    public void SetDtFromT(int T)
    {
        if (T <= 0) return;
        dt = Mathf.Clamp(0.45f * (10f / Mathf.Max(1, T)), 0.05f, 0.6f);
    }

    static void SetFloatFieldIfExists(object obj, string name, float value, BindingFlags flags)
    {
        var t = obj.GetType();
        var f = t.GetField(name, flags);
        if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(double)))
        {
            f.SetValue(obj, value);
            return;
        }
        var p = t.GetProperty(name, flags);
        if (p != null && p.CanWrite && (p.PropertyType == typeof(float) || p.PropertyType == typeof(double)))
            p.SetValue(obj, value, null);
    }

    static void SetIntFieldIfExists(object obj, string name, int value, BindingFlags flags)
    {
        var t = obj.GetType();
        var f = t.GetField(name, flags);
        if (f != null && (f.FieldType == typeof(int)))
        {
            f.SetValue(obj, value);
            return;
        }
        var p = t.GetProperty(name, flags);
        if (p != null && p.CanWrite && p.PropertyType == typeof(int))
            p.SetValue(obj, value, null);
    }

    static void CallIfExists(object obj, string method, params object[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var m = obj.GetType().GetMethod(method, flags);
        if (m != null) m.Invoke(obj, args);
    }
}
