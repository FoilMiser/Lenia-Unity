using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSimulator sim;
    public LeniaSeeder seeder;

    void Awake(){
        if (!sim)    sim    = FindFirstObjectByType<LeniaSimulator>();
        if (!seeder) seeder = FindFirstObjectByType<LeniaSeeder>();
    }

    void ReseedWith(SeedMode mode){
        if (seeder){
            seeder.mode = mode;
            seeder.SeedOnce();
        } else if (sim){
            // Fallbacks if seeder is missing; edit if your signatures differ.
            switch(mode){
                case SeedMode.Noise:   sim.SeedNoise(); break;
                case SeedMode.Clusters: sim.SeedClusters(); break;
                case SeedMode.Movers:  sim.SeedMovers(); break;
                case SeedMode.Orbium:  sim.SeedOrbium(); break;
            }
        }
    }

    void Update(){
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return;
        if (k.rKey.wasPressedThisFrame)      ReseedWith(SeedMode.Noise);
        if (k.digit1Key.wasPressedThisFrame) ReseedWith(SeedMode.Noise);
        if (k.digit2Key.wasPressedThisFrame) ReseedWith(SeedMode.Clusters);
        if (k.digit3Key.wasPressedThisFrame) ReseedWith(SeedMode.Movers);
        if (k.digit4Key.wasPressedThisFrame) ReseedWith(SeedMode.Orbium);
#else
        if (Input.GetKeyDown(KeyCode.R))        ReseedWith(SeedMode.Noise);
        if (Input.GetKeyDown(KeyCode.Alpha1))   ReseedWith(SeedMode.Noise);
        if (Input.GetKeyDown(KeyCode.Alpha2))   ReseedWith(SeedMode.Clusters);
        if (Input.GetKeyDown(KeyCode.Alpha3))   ReseedWith(SeedMode.Movers);
        if (Input.GetKeyDown(KeyCode.Alpha4))   ReseedWith(SeedMode.Orbium);
#endif
    }
}
