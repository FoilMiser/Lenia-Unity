#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class FlowLeniaEditor : EditorWindow
{
    [MenuItem("Lenia/Import Free-Kernel JSON")]
    public static void ImportJsonMenu()
    {
        string path = EditorUtility.OpenFilePanel("Select Lenia JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        FlowLeniaModel modelAsset;
        try {
            modelAsset = FlowLeniaImporter.ImportJson(json);
        } catch (System.Exception ex) {
            EditorUtility.DisplayDialog("Lenia", "Import failed:\n" + ex.Message, "OK");
            return;
        }

        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Save FlowLeniaModel", "FlowLeniaModel", "asset",
            "Choose where to save the model asset");
        if (string.IsNullOrEmpty(assetPath)) return;

        AssetDatabase.CreateAsset(modelAsset, assetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Lenia", "Model imported:\n" + assetPath, "OK");
        Selection.activeObject = modelAsset;
    }

    [MenuItem("Lenia/Create/Empty FlowLeniaModel")]
    public static void CreateEmptyModel()
    {
        var m = ScriptableObject.CreateInstance<FlowLeniaModel>();
        m.R = 7; m.kn = 1; m.gn = 1;
        m.kernels.Add(new FlowKernel{
            c0 = 0, c1 = 0,
            mu = 0.15f, sigma = 0.015f, h = 1f,
            rings = { new FlowRing{ r=3, w=1, b = new System.Collections.Generic.List<float>{1f} } }
        });

        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Save Empty Model", "FlowLeniaModel", "asset", "Save asset");
        if (string.IsNullOrEmpty(assetPath)) return;

        AssetDatabase.CreateAsset(m, assetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = m;
    }
}
#endif
