using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
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
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; if(kb==null) return;
        bool k1 = (kb.digit1Key?.wasPressedThisFrame ?? false) || (kb.numpad1Key?.wasPressedThisFrame ?? false);
        bool k2 = (kb.digit2Key?.wasPressedThisFrame ?? false) || (kb.numpad2Key?.wasPressedThisFrame ?? false);
        bool k3 = (kb.digit3Key?.wasPressedThisFrame ?? false) || (kb.numpad3Key?.wasPressedThisFrame ?? false);
        bool k4 = (kb.digit4Key?.wasPressedThisFrame ?? false) || (kb.numpad4Key?.wasPressedThisFrame ?? false);
        if(k1) Preset1(); if(k2) Preset2(); if(k3) Preset3(); if(k4) Preset4();
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) Preset1();
        if (Input.GetKeyDown(KeyCode.Alpha2)) Preset2();
        if (Input.GetKeyDown(KeyCode.Alpha3)) Preset3();
        if (Input.GetKeyDown(KeyCode.Alpha4)) Preset4();
#endif
    }

    // 1) Donuts (stable)
    public void Preset1(){
        sim.useRingKernel = true; sim.useMultiRing = false;
        sim.ringCenter = 0.50f; sim.ringWidth = 0.15f;
        sim.ApplyPreset(0.15f, 0.016f, 24, 0.30f, 0.10f);
        sim.stepsPerFrame = 1;
        if(seeder){ seeder.Clear(0f); seeder.noiseDensity=0.25f; seeder.SeedNoise(); }
    }

    // 2) Swarmers
    public void Preset2(){
        sim.useRingKernel = true; sim.useMultiRing = false;
        sim.ringCenter = 0.52f; sim.ringWidth = 0.12f;
        sim.ApplyPreset(0.14f, 0.028f, 24, 0.35f, 0.08f);
        sim.stepsPerFrame = 1;
        if(seeder){ seeder.Clear(0f); seeder.noiseDensity=0.28f; seeder.SeedNoise(); }
    }

    // 3) Movers (2-ring + sparse blobs)
    public void Preset3(){
        sim.useRingKernel = true; sim.useMultiRing = true;
        sim.ringCenter = 0.55f; sim.ringWidth = 0.12f;
        sim.ring2Center = 0.34f; sim.ring2Width = 0.07f; sim.ring2Weight = 0.60f;
        sim.ApplyPreset(0.15f, 0.020f, 24, 0.42f, 0.10f);
        sim.stepsPerFrame = 2;  // a bit more time flow helps reveal drift
        if(seeder){ seeder.Clear(0f); seeder.SeedFewBlobs(120, 0.007f, 0.018f, 1f); }
    }

    // 4) Chaotic / edge-of-life
    public void Preset4(){
        sim.useRingKernel = true; sim.useMultiRing = true;
        sim.ringCenter = 0.58f; sim.ringWidth = 0.10f;
        sim.ring2Center = 0.30f; sim.ring2Width = 0.06f; sim.ring2Weight = 0.75f;
        sim.ApplyPreset(0.16f, 0.018f, 24, 0.50f, 0.08f);
        sim.stepsPerFrame = 2;
        if(seeder){ seeder.Clear(0f); seeder.SeedFewBlobs(90, 0.006f, 0.015f, 1f); }
    }
}
