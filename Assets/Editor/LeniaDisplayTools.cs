using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class LeniaDisplayTools
{
    [MenuItem("Lenia/Use Grayscale Display")]
    public static void UseGrayscale()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/LeniaDisplay.mat");
        if (!mat) { Debug.LogError("Missing Assets/Shaders/LeniaDisplay.mat"); return; }

        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim || !sim.view) { Debug.LogError("No LeniaSimulation with a RawImage view in scene."); return; }

        sim.view.material = mat;
        sim.view.color = Color.white; // ensure tint isn't dark
        EditorUtility.SetDirty(sim.view);
        Debug.Log("Assigned LeniaDisplay material. Adjust _Exposure on the material if too dark/bright.");
    }
}
