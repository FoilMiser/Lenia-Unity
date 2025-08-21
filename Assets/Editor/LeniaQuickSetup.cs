using UnityEngine;
using UnityEditor;
using System.IO;

public class LeniaQuickSetup : MonoBehaviour
{
    [MenuItem("Lenia/Quick Setup (1–2 rings)")]
    public static void QuickSetup()
    {
        // Ensure folders
        string kernelsDir = "Assets/LeniaAssets";
        if (!AssetDatabase.IsValidFolder(kernelsDir)) AssetDatabase.CreateFolder("Assets", "LeniaAssets");

        // Create or load Kernel Profile asset
        var kernel = ScriptableObject.CreateInstance<LeniaKernelProfile>();
        kernel.radius = 28; kernel.ringCount = 2;
        kernel.means   = new float[] { 0.35f, 0.62f };
        kernel.peaks   = new float[] { 1.00f, 0.65f };
        kernel.stddevs = new float[] { 0.05f, 0.07f };
        string kernelPath = Path.Combine(kernelsDir, "Kernel_2ring.asset").Replace("\\","/");
        AssetDatabase.CreateAsset(kernel, kernelPath);

        // Create Growth Profile asset
        var growth = ScriptableObject.CreateInstance<LeniaGrowthProfile>();
        growth.mu = 0.15f; growth.sigma = 0.014f; growth.dt = 0.11f;
        string growthPath = Path.Combine(kernelsDir, "Growth_Standard.asset").Replace("\\","/");
        AssetDatabase.CreateAsset(growth, growthPath);

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        // Find or create a LeniaSimulation in the scene
        var sim = FindObjectOfType<LeniaSimulation>();
        if (sim == null)
        {
            var go = new GameObject("LeniaSimulation", typeof(LeniaSimulation));
            sim = go.GetComponent<LeniaSimulation>();
        }

        // Assign references
        sim.kernelProfile = AssetDatabase.LoadAssetAtPath<LeniaKernelProfile>(kernelPath);
        sim.growth = AssetDatabase.LoadAssetAtPath<LeniaGrowthProfile>(growthPath);
        var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/Lenia.compute");
        if (cs == null) Debug.LogError("Missing Assets/Shaders/Lenia.compute – create or move it there.");
        sim.leniaCS = cs;

        // Optional: tweak runtime defaults
        sim.autoRun = true; sim.stepsPerFrame = 1; sim.seedFill = 0.15f;

        EditorUtility.SetDirty(sim);
        Debug.Log("Lenia Quick Setup complete. Press Play, or call sim.Reseed() if blank.");
    }
}
