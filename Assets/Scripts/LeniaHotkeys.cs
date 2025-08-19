using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(50)]
public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSimulator sim;

    void Start(){
        if(!sim){
            #if UNITY_2023_1_OR_NEWER
            sim = Object.FindFirstObjectByType<LeniaSimulator>();
            #else
            sim = Object.FindObjectOfType<LeniaSimulator>();
            #endif
        }
    }

    void Update(){
        if(!sim) return;

        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; if (kb==null) return;
        if (kb.spaceKey.wasPressedThisFrame) sim.TogglePause();
        if (kb.leftBracketKey.wasPressedThisFrame)  sim.SetStepsPerFrame(sim.stepsPerFrame - 1);
        if (kb.rightBracketKey.wasPressedThisFrame) sim.SetStepsPerFrame(sim.stepsPerFrame + 1);
        if (kb.rKey.wasPressedThisFrame) {
            #if UNITY_2023_1_OR_NEWER
            var seeder = Object.FindFirstObjectByType<LeniaSeeder>();
            #else
            var seeder = Object.FindObjectOfType<LeniaSeeder>();
            #endif
            if (seeder) seeder.SeedNoise();
        }
        #else
        if (Input.GetKeyDown(KeyCode.Space)) sim.TogglePause();
        if (Input.GetKeyDown(KeyCode.LeftBracket))  sim.SetStepsPerFrame(sim.stepsPerFrame - 1);
        if (Input.GetKeyDown(KeyCode.RightBracket)) sim.SetStepsPerFrame(sim.stepsPerFrame + 1);
        if (Input.GetKeyDown(KeyCode.R)) {
            #if UNITY_2023_1_OR_NEWER
            var seeder = Object.FindFirstObjectByType<LeniaSeeder>();
            #else
            var seeder = Object.FindObjectOfType<LeniaSeeder>();
            #endif
            if (seeder) seeder.SeedNoise();
        }
        #endif
    }
}
