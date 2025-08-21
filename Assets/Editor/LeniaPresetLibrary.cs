using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class LeniaPresetDef {
    public string name;
    public int radius;
    public int ringCount;
    public float[] means;
    public float[] stddevs;
    public float[] peaks;
    public float mu, sigma, dt;
}

public class LeniaPresetLibrary : EditorWindow
{
    static readonly string PresetFolder = "Assets/LeniaPresets";

    // ---- Built-ins for quick life (you still have these) ----
    static readonly LeniaPresetDef[] PRESETS = new [] {
        new LeniaPresetDef{ name="1R_Orbiumish_A", radius=28, ringCount=1,
            means=new[]{0.35f}, stddevs=new[]{0.055f}, peaks=new[]{1.0f},
            mu=0.15f, sigma=0.015f, dt=0.07f },
        new LeniaPresetDef{ name="1R_ThinWorms", radius=28, ringCount=1,
            means=new[]{0.62f}, stddevs=new[]{0.038f}, peaks=new[]{1.0f},
            mu=0.10f, sigma=0.012f, dt=0.06f },

        new LeniaPresetDef{ name="2R_Mover_A", radius=28, ringCount=2,
            means=new[]{0.35f, 0.62f}, stddevs=new[]{0.055f, 0.075f}, peaks=new[]{1.0f, 0.65f},
            mu=0.15f, sigma=0.015f, dt=0.07f },
        new LeniaPresetDef{ name="2R_Mover_B", radius=30, ringCount=2,
            means=new[]{0.30f, 0.58f}, stddevs=new[]{0.050f, 0.070f}, peaks=new[]{1.0f, 0.70f},
            mu=0.14f, sigma=0.016f, dt=0.07f },
        new LeniaPresetDef{ name="2R_Rotatorish", radius=28, ringCount=2,
            means=new[]{0.32f, 0.60f}, stddevs=new[]{0.045f, 0.065f}, peaks=new[]{1.0f, 0.80f},
            mu=0.13f, sigma=0.013f, dt=0.07f },

        new LeniaPresetDef{ name="3R_Explorer_A", radius=30, ringCount=3,
            means=new[]{0.25f, 0.48f, 0.70f}, stddevs=new[]{0.040f, 0.060f, 0.070f}, peaks=new[]{1.0f, 0.70f, 0.55f},
            mu=0.15f, sigma=0.015f, dt=0.06f },
        new LeniaPresetDef{ name="3R_Explorer_B", radius=32, ringCount=3,
            means=new[]{0.28f, 0.52f, 0.74f}, stddevs=new[]{0.045f, 0.060f, 0.075f}, peaks=new[]{1.0f, 0.65f, 0.50f},
            mu=0.16f, sigma=0.016f, dt=0.06f },
    };

    class CustomPresetEntry {
        public string displayName;
        public string kernelPath;
        public string growthPath;
    }

    [MenuItem("Lenia/Preset Library")]
    public static void Open() { GetWindow<LeniaPresetLibrary>("Lenia Presets"); }

    [MenuItem("Lenia/Save Current As Preset...")]
    public static void SaveCurrentAsPresetMenu()
    {
        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim) { EditorUtility.DisplayDialog("Lenia", "Add a LeniaSimulation to the scene first.", "OK"); return; }
        SaveCurrentAsPreset(sim);
    }

    void OnGUI()
    {
        var sim = FindObjectOfType<LeniaSimulation>();
        if (!sim)
        {
            EditorGUILayout.HelpBox("Add a LeniaSimulation to the scene first.", MessageType.Info);
            if (GUILayout.Button("Create LeniaSimulation")) CreateSim();
            return;
        }

        if (!Directory.Exists(PresetFolder)) Directory.CreateDirectory(PresetFolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Built-in Presets", EditorStyles.boldLabel);
        foreach (var p in PRESETS)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(p.name, GUILayout.Width(200));
                if (GUILayout.Button("Apply")) CreateOrApply(sim, p);
                if (GUILayout.Button("Reseed μ-centered", GUILayout.Width(150))) sim.ReseedMuCentered();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Presets (from Assets/LeniaPresets)", EditorStyles.boldLabel);
        var customs = GetCustomPresets();
        if (customs.Count == 0) EditorGUILayout.HelpBox("None yet. Use 'Save Current As Preset...' to capture your current settings.", MessageType.None);
        foreach (var cp in customs)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(cp.displayName, GUILayout.Width(300));
                if (GUILayout.Button("Apply")) ApplyCustom(sim, cp);
                if (GUILayout.Button("Reveal", GUILayout.Width(80))) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(cp.kernelPath));
                if (GUILayout.Button("Reseed μ-centered", GUILayout.Width(150))) sim.ReseedMuCentered();
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Current As Preset...")) SaveCurrentAsPreset(sim);
            if (GUILayout.Button("Reseed μ-centered")) sim.ReseedMuCentered();
        }

        EditorGUILayout.Space();
        sim.resolution = EditorGUILayout.Vector2IntField("Resolution", sim.resolution);
        sim.stepsPerFrame = EditorGUILayout.IntSlider("Steps/Frame", sim.stepsPerFrame, 1, 8);
        sim.autoRun = EditorGUILayout.Toggle("Auto Run", sim.autoRun);
        if (GUILayout.Button("Rebind Profiles")) sim.ApplyPreset();
    }

    void CreateSim()
    {
        var go = new GameObject("LeniaSimulation", typeof(LeniaSimulation));
        var sim = go.GetComponent<LeniaSimulation>();
        sim.resolution = new Vector2Int(512,512);
        Selection.activeObject = go;
    }

    // Create OR update assets for a built-in and assign them
    static void CreateOrApply(LeniaSimulation sim, LeniaPresetDef p)
    {
        if (!AssetDatabase.IsValidFolder(PresetFolder)) AssetDatabase.CreateFolder("Assets", "LeniaPresets");

        string kPath = Path.Combine(PresetFolder, p.name + "_Kernel.asset").Replace("\\","/");
        var K = AssetDatabase.LoadAssetAtPath<LeniaKernelProfile>(kPath);
        if (!K) {
            K = ScriptableObject.CreateInstance<LeniaKernelProfile>();
            AssetDatabase.CreateAsset(K, kPath);
        }
        K.radius = p.radius; K.ringCount = p.ringCount;
        K.means   = (float[])p.means.Clone();
        K.stddevs = (float[])p.stddevs.Clone();
        K.peaks   = (float[])p.peaks.Clone();
        K.Invalidate();
        EditorUtility.SetDirty(K);

        string gPath = Path.Combine(PresetFolder, p.name + "_Growth.asset").Replace("\\","/");
        var G = AssetDatabase.LoadAssetAtPath<LeniaGrowthProfile>(gPath);
        if (!G) {
            G = ScriptableObject.CreateInstance<LeniaGrowthProfile>();
            AssetDatabase.CreateAsset(G, gPath);
        }
        G.mu = p.mu; G.sigma = p.sigma; G.dt = p.dt;
        EditorUtility.SetDirty(G);

        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        sim.kernelProfile = K; sim.growth = G; sim.ApplyPreset();
    }

    // Capture whatever is currently assigned on the sim into new assets
    static void SaveCurrentAsPreset(LeniaSimulation sim)
    {
        if (!sim.kernelProfile || !sim.growth)
        {
            EditorUtility.DisplayDialog("Lenia", "Assign a Kernel Profile and Growth Profile on the LeniaSimulation first.", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder(PresetFolder)) AssetDatabase.CreateFolder("Assets", "LeniaPresets");

        string defaultBase = "CUSTOM_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string kernelPath = EditorUtility.SaveFilePanelInProject("Save Kernel Preset", defaultBase + "_Kernel", "asset", "Choose save location for the Kernel asset", PresetFolder);
        if (string.IsNullOrEmpty(kernelPath)) return;
        string prefix = kernelPath.EndsWith("_Kernel.asset") ? kernelPath.Substring(0, kernelPath.Length - "_Kernel.asset".Length) : kernelPath;
        string growthPath = prefix + "_Growth.asset";

        // Clone current values
        var k = ScriptableObject.CreateInstance<LeniaKernelProfile>();
        k.radius = sim.kernelProfile.radius; k.ringCount = sim.kernelProfile.ringCount;
        k.means   = (float[])sim.kernelProfile.means.Clone();
        k.stddevs = (float[])sim.kernelProfile.stddevs.Clone();
        k.peaks   = (float[])sim.kernelProfile.peaks.Clone();
        k.Invalidate();

        var g = ScriptableObject.CreateInstance<LeniaGrowthProfile>();
        g.mu = sim.growth.mu; g.sigma = sim.growth.sigma; g.dt = sim.growth.dt;

        AssetDatabase.CreateAsset(k, kernelPath);
        AssetDatabase.CreateAsset(g, growthPath);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Lenia", "Saved:\n" + kernelPath + "\n" + growthPath, "OK");
    }

    static List<CustomPresetEntry> GetCustomPresets()
    {
        var list = new List<CustomPresetEntry>();
        if (!Directory.Exists(PresetFolder)) return list;

        var kernels = Directory.GetFiles(PresetFolder, "*_Kernel.asset");
        foreach (var k in kernels)
        {
            string prefix = k.Replace("\\","/").Replace("_Kernel.asset", "");
            string g = prefix + "_Growth.asset";
            if (File.Exists(g))
            {
                var entry = new CustomPresetEntry {
                    displayName = Path.GetFileName(prefix),
                    kernelPath = k.Replace("\\","/"),
                    growthPath = g.Replace("\\","/")
                };
                list.Add(entry);
            }
        }
        list.Sort((a,b)=>a.displayName.CompareTo(b.displayName));
        return list;
    }

    static void ApplyCustom(LeniaSimulation sim, CustomPresetEntry cp)
    {
        var K = AssetDatabase.LoadAssetAtPath<LeniaKernelProfile>(cp.kernelPath);
        var G = AssetDatabase.LoadAssetAtPath<LeniaGrowthProfile>(cp.growthPath);
        if (!K || !G) { Debug.LogError("Custom preset assets not found."); return; }
        sim.kernelProfile = K; sim.growth = G; sim.ApplyPreset();
    }
}
