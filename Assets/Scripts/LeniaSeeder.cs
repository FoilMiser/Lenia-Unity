using UnityEngine;
using System.Linq;
using System.Reflection;

public enum SeedMode { None, Noise, Clusters, Movers, Orbium }

[DisallowMultipleComponent]
public class LeniaSeeder : MonoBehaviour
{
    public LeniaSimulator sim;

    [Header("When to seed")]
    public bool autoSeedOnPlay = true;
    public bool clearBeforeSeed = true;

    [Header("Randomness")]
    public bool randomizeSeed = true;
    public int seed = 12345;

    [Header("Mode")]
    public SeedMode mode = SeedMode.Movers;

    [Header("Noise")]
    [Range(0f, 1f)] public float noiseDensity = 0.015f;
    [Range(0f, 1f)] public float noiseAmplitude = 0.8f;

    [Header("Clusters & Movers")]
    [Min(0)] public int   count     = 120;
    [Min(0f)] public float radius   = 12f;      // ≈ KernelRadius/2 when KernelRadius = 24
    [Range(0f, 1f)] public float amplitude = 0.7f;

    [Header("Orbium")]
    [Min(0f)] public float orbiumRadius = 18f;
    [Range(0f, 1f)] public float orbiumAmplitude = 0.9f;

    void Awake(){
#if UNITY_2023_1_OR_NEWER
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
#else
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
#endif
    }

    void Start(){
        if (autoSeedOnPlay) SeedOnce();
    }

    void InitRandom(){
        if (!randomizeSeed) Random.InitState(seed);
    }

    // Reflection helper so we never hard-crash if signatures differ
    bool Call(string name, params object[] args){
        if (sim == null) return false;
        var t = sim.GetType();
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .Where(m => m.Name == name);
        foreach (var m in methods){
            var ps = m.GetParameters();
            if (ps.Length != args.Length) continue;
            try { m.Invoke(sim, args); return true; } catch {}
        }
        return false;
    }

    public void SeedOnce(){
        if (!sim){
            Debug.LogWarning("LeniaSeeder: no simulator in scene.");
            return;
        }

        InitRandom();

        if (clearBeforeSeed){
            if (!Call("Clear")) Call("ClearState");
        }

        switch (mode){
            case SeedMode.Noise:
                if (!Call("SeedNoise", noiseDensity, noiseAmplitude))
                    if (!Call("SeedNoise", noiseDensity))
                        if (!Call("SeedNoise")) Call("SeedNoise", 0.02f);
                break;

            case SeedMode.Clusters:
                if (!Call("SeedClusters", count, radius, amplitude))
                    if (!Call("SeedClusters", count, radius))
                        if (!Call("SeedClusters")) Call("SeedNoise", noiseDensity);
                break;

            case SeedMode.Movers:
                if (!Call("SeedMovers", count, radius, amplitude))
                    if (!Call("SeedMovers", count, radius))
                        if (!Call("SeedMovers")) Call("SeedNoise", noiseDensity);
                break;

            case SeedMode.Orbium:
                if (!Call("SeedOrbium", orbiumRadius, orbiumAmplitude))
                    if (!Call("SeedOrbium", orbiumRadius))
                        if (!Call("SeedOrbium")) Call("SeedNoise", noiseDensity);
                break;

            case SeedMode.None:
            default:
                break;
        }
    }

    // ---- Compatibility shims for older code (LeniaPresets, etc.) ----
    public void Clear(){
        if (!sim){
#if UNITY_2023_1_OR_NEWER
            sim = FindFirstObjectByType<LeniaSimulator>();
#else
            sim = FindFirstObjectByType<LeniaSimulator>();
#endif
        }
        if (!Call("Clear")) Call("ClearState");
    }
    public void Clear(bool reseedNoise){
        Clear();
        if (reseedNoise){
            if (!Call("SeedNoise", noiseDensity, noiseAmplitude))
                if (!Call("SeedNoise", noiseDensity))
                    Call("SeedNoise", 0.02f);
        }
    }

    // Legacy: seed a few blobs (movers) with defaults from inspector
    public void SeedFewBlobs(){
        mode = SeedMode.Movers;
        if (count <= 0) count = 120;
        if (radius <= 0f) radius = 12f;
        if (amplitude <= 0f) amplitude = 0.7f;
        SeedOnce();
    }
    // Legacy 3-arg (count, radius, amplitude)
    public void SeedFewBlobs(int c, float r, float a){
        count = c; radius = r; amplitude = a;
        mode = SeedMode.Movers; SeedOnce();
    }
    // Legacy 4-arg variant: (count, radius, amplitude, int seed)
    public void SeedFewBlobs(int c, float r, float a, int s){
        count = c; radius = r; amplitude = a;
        randomizeSeed = false; seed = s;
        mode = SeedMode.Movers; SeedOnce();
    }
    // Legacy 4-arg variant: (count, radius, amplitude, bool clearBefore)
    public void SeedFewBlobs(int c, float r, float a, bool clear){
        count = c; radius = r; amplitude = a;
        clearBeforeSeed = clear;
        mode = SeedMode.Movers; SeedOnce();
    }
    // Legacy 4-arg variant: (count, radius, amplitude, float extra)
    public void SeedFewBlobs(int c, float r, float a, float extra){
        count = c; radius = r; amplitude = a;
        mode = SeedMode.Movers; SeedOnce();
    }
    // -----------------------------------------------------------------
    // Compatibility: some legacy presets pass numeric flags to Clear(...)
    public void Clear(float x){ Clear(x > 0f); }
    public void Clear(int   x){ Clear(x != 0); }
    // --- Convenience & compatibility wrappers so presets compile ---
    public void SeedNoise(){ mode = SeedMode.Noise; SeedOnce(); }
    public void SeedNoise(float density){ noiseDensity = density; mode = SeedMode.Noise; SeedOnce(); }
    public void SeedNoise(float density, float amp){ noiseDensity = density; noiseAmplitude = amp; mode = SeedMode.Noise; SeedOnce(); }

    public void SeedClusters(){ mode = SeedMode.Clusters; SeedOnce(); }
    public void SeedClusters(int c, float r){ count = c; radius = r; mode = SeedMode.Clusters; SeedOnce(); }
    public void SeedClusters(int c, float r, float a){ count = c; radius = r; amplitude = a; mode = SeedMode.Clusters; SeedOnce(); }

    public void SeedMovers(){ mode = SeedMode.Movers; SeedOnce(); }
    public void SeedMovers(int c, float r){ count = c; radius = r; mode = SeedMode.Movers; SeedOnce(); }
    public void SeedMovers(int c, float r, float a){ count = c; radius = r; amplitude = a; mode = SeedMode.Movers; SeedOnce(); }

    public void SeedOrbium(){ mode = SeedMode.Orbium; SeedOnce(); }
    public void SeedOrbium(float r){ orbiumRadius = r; mode = SeedMode.Orbium; SeedOnce(); }
    public void SeedOrbium(float r, float a){ orbiumRadius = r; orbiumAmplitude = a; mode = SeedMode.Orbium; SeedOnce(); }
}

