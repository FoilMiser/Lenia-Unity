#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowLeniaAutoWire
{
    [MenuItem("Tools/Lenia/Wire Viewer (Canvas/Display)")]
    public static void WireNow()
    {
        WireInternal(createSimIfMissing:true, logPrefix:"[WireNow]");
    }

    [InitializeOnLoadMethod]
    private static void AutoWireOnLoad()
    {
        // Run once after scripts reload; keep it gentle but effective.
        EditorApplication.delayCall += () => WireInternal(createSimIfMissing:true, logPrefix:"[AutoWire]");
    }

    private static void WireInternal(bool createSimIfMissing, string logPrefix)
    {
        if (Application.isPlaying) return;

        var display = GameObject.Find("Display");
        var canvas  = GameObject.Find("Canvas");
        if (display == null || canvas == null)
        {
            // Nothing to wire yet; stay quiet to avoid spam.
            return;
        }

        var viewer = display.GetComponent<FlowLeniaViewer>();
        if (!viewer) viewer = display.AddComponent<FlowLeniaViewer>();

        var raw = display.GetComponent<RawImage>();
        if (!raw) raw = display.AddComponent<RawImage>();

        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim && createSimIfMissing)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }

        if (sim)
        {
            viewer.sim = sim;
            viewer.target = raw;

            EditorUtility.SetDirty(viewer);
            EditorUtility.SetDirty(raw);
            EditorUtility.SetDirty(sim);
            EditorSceneManager.MarkSceneDirty(viewer.gameObject.scene);

            Debug.Log($"{logPrefix} Wired FlowLeniaViewer → sim='{sim.name}', target='{raw.name}'.");
        }
        else
        {
            Debug.LogWarning($"{logPrefix} No FlowLeniaSimulation found to wire.");
        }
    }
}
#endif
