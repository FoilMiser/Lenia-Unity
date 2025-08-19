using UnityEngine;\n#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER\nusing UnityEngine.InputSystem;\n#endif\n[DefaultExecutionOrder(50)]
public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSimulator sim;
    void Start(){ if(!sim) sim = FindObjectOfType<LeniaSimulator>(); }
    void Update(){\n#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER\n    if(sim==null) return;\n    if (Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame) sim.TogglePause();\n    if (Keyboard.current!=null && Keyboard.current.leftBracketKey.wasPressedThisFrame)  sim.SetStepsPerFrame(sim.stepsPerFrame-1);\n    if (Keyboard.current!=null && Keyboard.current.rightBracketKey.wasPressedThisFrame) sim.SetStepsPerFrame(sim.stepsPerFrame+1);\n    if (Keyboard.current!=null && Keyboard.current.rKey.wasPressedThisFrame) { var seeder = Object.FindObjectOfType<LeniaSeeder>(); if (seeder) seeder.SeedNoise(); }\n    return;\n#endif\n
        if(!sim) return;
        if (Input.GetKeyDown(KeyCode.Space)) sim.TogglePause();
        if (Input.GetKeyDown(KeyCode.LeftBracket))  sim.SetStepsPerFrame(sim.stepsPerFrame-1);
        if (Input.GetKeyDown(KeyCode.RightBracket)) sim.SetStepsPerFrame(sim.stepsPerFrame+1);
    }
}

