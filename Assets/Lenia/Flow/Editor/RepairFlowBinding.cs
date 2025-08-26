#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RepairFlowBinding
{
    [MenuItem("Tools/Lenia/Repair Viewer Binding")]
    public static void Repair()
    {
        if (Application.isPlaying) return;

        // 1) Get or make Display + RawImage + FlowLeniaViewer
        var display = GameObject.Find("Display");
        if (!display)
        {
            var canvas = GameObject.Find("Canvas") ?? new GameObject("Canvas", typeof(Canvas));
            var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            display = new GameObject("Display", typeof(RectTransform));
            display.transform.SetParent(canvas.transform, false);
            display.AddComponent<CanvasRenderer>();
        }
        var raw = display.GetComponent<RawImage>() ?? display.AddComponent<RawImage>();
        var viewer = display.GetComponent<FlowLeniaViewer>() ?? display.AddComponent<FlowLeniaViewer>();

        // Stretch full-screen
        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // 2) Ensure a dedicated FlowLeniaSimulation GO exists (not on Display)
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }
        var simOnDisplay = display.GetComponent<FlowLeniaSimulation>();
        if (simOnDisplay)
        {
            var dstGO = new GameObject("FlowLeniaSimulation");
            var dst = dstGO.AddComponent<FlowLeniaSimulation>();
            EditorUtility.CopySerialized(simOnDisplay, dst);
            Object.DestroyImmediate(simOnDisplay, allowDestroyingAssets:false);
            sim = dst;
        }

        // 3) Assign the compute shader if missing
        if (sim.flowCS == null)
        {
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lenia/Flow/FlowLenia.compute");
            if (cs) sim.flowCS = cs;
        }

        // 4) Wire viewer fields
        viewer.sim = sim;
        viewer.target = raw;

        // 5) Save dirty
        EditorUtility.SetDirty(sim);
        EditorUtility.SetDirty(viewer);
        EditorUtility.SetDirty(raw);
        EditorSceneManager.MarkSceneDirty(viewer.gameObject.scene);

        Debug.Log("[RepairFlow] Wired Display → FlowLeniaViewer(target) and FlowLeniaSimulation(sim).");
    }
}
#endif
