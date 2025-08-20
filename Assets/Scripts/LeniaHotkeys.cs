using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSeeder seeder;

    void Awake(){
        if (!seeder) seeder = FindFirstObjectByType<LeniaSeeder>();
    }

    void Reseed(SeedMode mode){
        if (!seeder){ Debug.LogWarning("LeniaHotkeys: No LeniaSeeder found in scene."); return; }
        seeder.mode = mode;
        seeder.SeedOnce();
    }

    void Update(){
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return;
        if (k.rKey.wasPressedThisFrame || k.digit1Key.wasPressedThisFrame) Reseed(SeedMode.Noise);
        if (k.digit2Key.wasPressedThisFrame) Reseed(SeedMode.Clusters);
        if (k.digit3Key.wasPressedThisFrame) Reseed(SeedMode.Movers);
        // No SeedOrbium in your simulator yet; map 4 to Clusters for now.
        if (k.digit4Key.wasPressedThisFrame) Reseed(SeedMode.Clusters);
#else
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1)) Reseed(SeedMode.Noise);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Reseed(SeedMode.Clusters);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Reseed(SeedMode.Movers);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Reseed(SeedMode.Clusters);
#endif
    }
}
