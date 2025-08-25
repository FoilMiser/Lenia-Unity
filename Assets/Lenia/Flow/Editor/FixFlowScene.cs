#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FixFlowScene
{
    [MenuItem("Tools/Lenia/Fix Sim & Viewer (Display)")]
    public static void FixNow(){ Run(true); }

    [InitializeOnLoadMethod]
    private static void Auto(){ EditorApplication.delayCall += () => Run(false); }

    private static void Run(bool log)
    {
        if (Application.isPlaying) return;

        // Kill any stray runtime-created viewer
        var stray = GameObject.Find("LeniaViewCanvas");
        if (stray){ Object.DestroyImmediate(stray); if (log) Debug.Log("[Fix] Removed stray 'LeniaViewCanvas'."); }

        // Ensure we have Display + RawImage + Viewer
        var display = GameObject.Find("Display");
        if (!display){ if (log) Debug.LogWarning("[Fix] No GameObject named 'Display' in scene."); return; }
        var raw = display.GetComponent<RawImage>() ?? display.AddComponent<RawImage>();
        var viewer = display.GetComponent<FlowLeniaViewer>() ?? display.AddComponent<FlowLeniaViewer>();

        // Find/create a dedicated FlowLeniaSimulation
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        var simOnDisplay = display.GetComponent<FlowLeniaSimulation>();
        if (!sim && simOnDisplay) sim = simOnDisplay;
        if (!sim){
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }

        // If the sim is on Display, migrate it to its own GO
        if (simOnDisplay && simOnDisplay.gameObject == display){
            var dstGo = new GameObject("FlowLeniaSimulation");
            var dst = dstGo.AddComponent<FlowLeniaSimulation>();
            EditorUtility.CopySerialized(simOnDisplay, dst);
            Object.DestroyImmediate(simOnDisplay, allowDestroyingAssets:false);
            sim = dst;
            if (log) Debug.Log("[Fix] Moved FlowLeniaSimulation off 'Display' to its own GameObject.");
        }

        // Wire viewer
        viewer.sim = sim;
        viewer.target = raw;

        EditorUtility.SetDirty(viewer);
        EditorUtility.SetDirty(raw);
        EditorUtility.SetDirty(sim);
        EditorSceneManager.MarkSceneDirty(viewer.gameObject.scene);

        if (log) Debug.Log($"[Fix] Wired: sim='{sim.name}', target='{raw.name}'.");
    }
}
#endif
