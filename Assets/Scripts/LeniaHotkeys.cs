using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Linq;
using System.Reflection;

public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSimulator sim;        // prefer direct simulator calls (original behavior)
    public LeniaSeeder seeder;        // fallback only if sim lacks a method

    void Awake(){
#if UNITY_2023_1_OR_NEWER
        if (!sim)    sim    = FindFirstObjectByType<LeniaSimulator>();
        if (!seeder) seeder = FindFirstObjectByType<LeniaSeeder>();
#else
        if (!sim)    sim    = FindObjectOfType<LeniaSimulator>();
        if (!seeder) seeder = FindObjectOfType<LeniaSeeder>();
#endif
    }

    bool CallSim(string name, params object[] args){
        if (!sim) return false;
        var t = sim.GetType();
        var ms = t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(m=>m.Name==name);
        foreach (var m in ms){
            var ps = m.GetParameters();
            if (ps.Length != args.Length) continue;
            try { m.Invoke(sim, args); return true; } catch {}
        }
        return false;
    }

    void SeedNoise(){ if (!CallSim("SeedNoise")) { if (seeder){ seeder.mode = SeedMode.Noise; seeder.SeedOnce(); } } }
    void SeedClusters(){ if (!CallSim("SeedClusters")) { if (seeder){ seeder.mode = SeedMode.Clusters; seeder.SeedOnce(); } } }
    void SeedMovers(){ if (!CallSim("SeedMovers")) { if (seeder){ seeder.mode = SeedMode.Movers; seeder.SeedOnce(); } } }

    void Update(){
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return;
        if (k.rKey.wasPressedThisFrame || k.digit1Key.wasPressedThisFrame) SeedNoise();
        if (k.digit2Key.wasPressedThisFrame) SeedClusters();
        if (k.digit3Key.wasPressedThisFrame) SeedMovers();          // <- original path restored
        if (k.digit4Key.wasPressedThisFrame) SeedClusters();        // temp mapping (no SeedOrbium in sim)
#else
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1)) SeedNoise();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SeedClusters();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SeedMovers();          // <- original path restored
        if (Input.GetKeyDown(KeyCode.Alpha4)) SeedClusters();
#endif
    }
}
