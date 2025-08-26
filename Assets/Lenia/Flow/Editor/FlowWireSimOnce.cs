#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FlowWireSimOnce
{
    static FlowWireSimOnce()
    {
        // Auto-run after compile. You can also run it manually from the menu.
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Lenia/Ensure Dedicated Flow Sim")]
    public static void Run()
    {
        if (Application.isPlaying) return;

        // Find Display (RawImage) + FlowLeniaViewer
        var display = GameObject.Find("Display");
        if (!display) { Debug.LogWarning("[FlowWire] No GameObject named 'Display' found."); return; }

        var raw = display.GetComponent<RawImage>() ?? display.AddComponent<RawImage>();
        var viewer = display.GetComponent<FlowLeniaViewer>() ?? display.AddComponent<FlowLeniaViewer>();

        // Find any FlowLeniaSimulation in scene
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        var simOnDisplay = display.GetComponent<FlowLeniaSimulation>();
        if (!sim && simOnDisplay) sim = simOnDisplay;

        // If the sim is on Display, migrate it to its own GO
        if (simOnDisplay && simOnDisplay.gameObject == display)
        {
            var go = new GameObject("FlowLeniaSimulation");
            var dst = go.AddComponent<FlowLeniaSimulation>();
            EditorUtility.CopySerialized(simOnDisplay, dst);
            Object.DestroyImmediate(simOnDisplay, allowDestroyingAssets:false);
            sim = dst;
            Debug.Log("[FlowWire] Moved FlowLeniaSimulation off 'Display' to separate GameObject.");
        }

        // If still none, create one
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
            Debug.Log("[FlowWire] Created new 'FlowLeniaSimulation' GameObject.");
        }

        // Wire the viewer
        viewer.sim = sim;
        viewer.target = raw;

        EditorUtility.SetDirty(viewer);
        EditorUtility.SetDirty(sim);
        EditorUtility.SetDirty(raw);
        EditorSceneManager.MarkSceneDirty(viewer.gameObject.scene);

        Debug.Log($"[FlowWire] Wired viewer on '{display.name}' to sim '{sim.name}'.");
    }
}
#endif
