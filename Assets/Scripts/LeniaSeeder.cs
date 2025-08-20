using UnityEngine;
using System.Linq;
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
    [Min(0)] public int count = 150;
    [Min(0f)] public float radius = 12f;
    [Range(0f, 1f)] public float amplitude = 0.7f;

    [Header("Orbium")]
    [Min(0f)] public float orbiumRadius = 18f;
    [Range(0f, 1f)] public float orbiumAmplitude = 0.9f;

    void Awake(){
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
    }
    void Start(){
        if (autoSeedOnPlay) SeedOnce();
    }

    void InitRandom(){
        if (!randomizeSeed) Random.InitState(seed);
    }

    // Reflection helper so we never hard-crash if a method signature differs.
    bool Call(string method, params object[] args){
        if (sim == null) return false;
        var t = sim.GetType();
        foreach (var m in t.GetMethods().Where(mm => mm.Name == method)){
            var ps = m.GetParameters();
            if (ps.Length != args.Length) continue;
            try { m.Invoke(sim, args); return true; } catch {}
        }
        return false;
    }

    public void SeedOnce(){
        if (!sim) { Debug.LogWarning("LeniaSeeder: no simulator."); return; }

        InitRandom();

        if (clearBeforeSeed){
            if (!Call("Clear")) { /* optional: sim state cleared elsewhere */ }
        }

        switch (mode){
            case SeedMode.Noise:
                // try (density, amplitude) then (density) then no-arg
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
    // Compatibility shim for older code calling seeder.SeedFewBlobs()
    public void SeedFewBlobs(){
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
        // sensible defaults if not set in Inspector
        if (count <= 0) count = 120;
        if (radius <= 0f) radius = 12f;   // ≈ KernelRadius/2 for Radius=24
        if (amplitude <= 0f) amplitude = 0.7f;
        mode = SeedMode.Movers;
        SeedOnce();
    }
    // --- Compatibility overloads for older code -----------------------------
    // Legacy: Seed a few blobs (movers) with defaults from the inspector.
    public void SeedFewBlobs() {
        if (!sim) sim = FindFirstObjectByType<LeniaSimulator>();
        if (count <= 0) count = 120;
        if (radius <= 0f) radius = 12f;
        if (amplitude <= 0f) amplitude = 0.7f;
        mode = SeedMode.Movers;
        SeedOnce();
    }
    // Legacy 3-arg: (count, radius, amplitude)
    public void SeedFewBlobs(int c, float r, float a) {
        count = c; radius = r; amplitude = a;
        mode = SeedMode.Movers;
        SeedOnce();
    }
    // Legacy 4-arg (variant A): (count, radius, amplitude, seed)
    public void SeedFewBlobs(int c, float r, float a, int s) {
        count = c; radius = r; amplitude = a;
        randomizeSeed = false; seed = s;
        mode = SeedMode.Movers;
        SeedOnce();
    }
    // Legacy 4-arg (variant B): (count, radius, amplitude, clearBeforeSeed)
    public void SeedFewBlobs(int c, float r, float a, bool clear) {
        count = c; radius = r; amplitude = a;
        clearBeforeSeed = clear;
        mode = SeedMode.Movers;
        SeedOnce();
    }
    // ------------------------------------------------------------------------
}
