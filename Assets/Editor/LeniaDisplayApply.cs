using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class LeniaDisplayApply
{
    [MenuItem("Lenia/Use Palette Display Now")]
    public static void UsePalette()
    {
        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim) { Debug.LogError("No LeniaSimulation found in scene."); return; }

        var sh = Shader.Find("Unlit/LeniaPalette");
        if (!sh) { Debug.LogError("Shader Unlit/LeniaPalette not found (reimport Assets/Shaders)."); return; }

        var mat = new Material(sh);
        sim.view.material = mat;
        sim.view.color = Color.white;

        // Nudge params for a good start
        sim.dispExposure = Mathf.Max(10f, sim.dispExposure);
        sim.dispGamma = 1.2f;
        sim.useEdges = true;  sim.edgeStrength = 0.8f; sim.edgeThreshold = 0.015f;
        sim.useTrail = true;  sim.trailDecay = 0.97f; sim.trailWeight = 0.6f;

        // Force an immediate update
        sim.ApplyPreset();
        Debug.Log("Lenia: Palette display applied. Tweak Display Settings on LeniaSimulation.");
    }
}
