using UnityEngine;
using UnityEditor;

public static class LeniaAutoDefaults
{
    [InitializeOnLoadMethod]
    static void Ensure()
    {
        EditorApplication.delayCall += () =>
        {
            var sims = Object.FindObjectsOfType<LeniaSimulation>();
            if (sims == null || sims.Length == 0) return;

            // Ensure or create default assets
            if (!AssetDatabase.IsValidFolder("Assets/LeniaAssets"))
                AssetDatabase.CreateFolder("Assets", "LeniaAssets");

            string kernelPath = "Assets/LeniaAssets/Kernel_2ring.asset";
            string growthPath = "Assets/LeniaAssets/Growth_Standard.asset";

            var kernel = AssetDatabase.LoadAssetAtPath<LeniaKernelProfile>(kernelPath);
            if (kernel == null)
            {
                kernel = ScriptableObject.CreateInstance<LeniaKernelProfile>();
                kernel.radius = 28; kernel.ringCount = 2;
                kernel.means   = new float[] { 0.35f, 0.62f };
                kernel.peaks   = new float[] { 1.00f, 0.65f };
                kernel.stddevs = new float[] { 0.05f, 0.07f };
                AssetDatabase.CreateAsset(kernel, kernelPath);
            }

            var growth = AssetDatabase.LoadAssetAtPath<LeniaGrowthProfile>(growthPath);
            if (growth == null)
            {
                growth = ScriptableObject.CreateInstance<LeniaGrowthProfile>();
                growth.mu = 0.15f; growth.sigma = 0.014f; growth.dt = 0.11f;
                AssetDatabase.CreateAsset(growth, growthPath);
            }

            // Try both likely locations for the compute shader
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Resources/Lenia.compute");
            if (cs == null) cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shaders/Lenia.compute");

            foreach (var s in sims)
            {
                if (s.kernelProfile == null) s.kernelProfile = kernel;
                if (s.growth == null) s.growth = growth;
                if (s.leniaCS == null && cs != null) s.leniaCS = cs;

                // Force-bind everything
                try { s.ApplyProfiles(); } catch { }
                EditorUtility.SetDirty(s);
            }
            AssetDatabase.SaveAssets();
        };
    }
}
