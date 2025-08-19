using UnityEngine;
[DefaultExecutionOrder(60)]
public class LeniaPresets : MonoBehaviour
{
    public LeniaSimulator sim; public LeniaSeeder seeder;
    void Start(){
        if(!sim){
#if UNITY_2023_1_OR_NEWER
            sim = Object.FindFirstObjectByType<LeniaSimulator>();
#else
            sim = Object.FindObjectOfType<LeniaSimulator>();
#endif
        }
        if(!seeder){
#if UNITY_2023_1_OR_NEWER
            seeder = Object.FindFirstObjectByType<LeniaSeeder>();
#else
            seeder = Object.FindObjectOfType<LeniaSeeder>();
#endif
        }
    }

    void Update(){
        if(!sim) return;

        // 1) Donuts (stable)
        if (Input.GetKeyDown(KeyCode.Alpha1)){
            sim.useRingKernel = true; sim.useMultiRing = false;
            sim.ringCenter = 0.50f; sim.ringWidth = 0.15f;
            sim.ApplyPreset(0.15f, 0.016f, 24, 0.30f, 0.10f);
            if(seeder){ seeder.noiseDensity=0.25f; seeder.SeedNoise(); }
        }

        // 2) Swarmers
        if (Input.GetKeyDown(KeyCode.Alpha2)){
            sim.useRingKernel = true; sim.useMultiRing = false;
            sim.ringCenter = 0.52f; sim.ringWidth = 0.12f;
            sim.ApplyPreset(0.14f, 0.028f, 24, 0.35f, 0.08f);
            if(seeder){ seeder.noiseDensity=0.35f; seeder.SeedNoise(); }
        }

        // 3) Movers (2-ring)
        if (Input.GetKeyDown(KeyCode.Alpha3)){
            sim.useRingKernel = true; sim.useMultiRing = true;
            sim.ringCenter = 0.55f; sim.ringWidth = 0.12f;
            sim.ring2Center = 0.34f; sim.ring2Width = 0.07f; sim.ring2Weight = 0.60f;
            sim.ApplyPreset(0.15f, 0.020f, 24, 0.40f, 0.08f);
            if(seeder){ seeder.noiseDensity=0.28f; seeder.SeedNoise(); }
        }

        // 4) Chaotic/edge-of-life
        if (Input.GetKeyDown(KeyCode.Alpha4)){
            sim.useRingKernel = true; sim.useMultiRing = true;
            sim.ringCenter = 0.58f; sim.ringWidth = 0.10f;
            sim.ring2Center = 0.30f; sim.ring2Width = 0.06f; sim.ring2Weight = 0.75f;
            sim.ApplyPreset(0.16f, 0.018f, 24, 0.50f, 0.08f);
            if(seeder){ seeder.noiseDensity=0.22f; seeder.SeedNoise(); }
        }
    }
}
