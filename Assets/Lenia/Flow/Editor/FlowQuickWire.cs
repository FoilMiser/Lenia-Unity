#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowQuickWire
{
    [MenuItem("Tools/Lenia/Quick Wire Display")]
    public static void Wire()
    {
        if (Application.isPlaying) return;

        // Ensure Canvas/Display
        var canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            canvas = new GameObject("Canvas", typeof(Canvas));
            var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        var display = GameObject.Find("Display");
        if (!display)
        {
            display = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            display.transform.SetParent(canvas.transform, false);
        }

        // Stretch full screen
        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var raw = display.GetComponent<RawImage>();
        var viewer = display.GetComponent<FlowLeniaViewer>() ?? display.AddComponent<FlowLeniaViewer>();

        // Ensure a dedicated FlowLeniaSimulation exists
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }

        // Assign compute shader if missing
        if (sim.flowCS == null)
        {
            var cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Lenia/Flow/FlowLenia.compute");
            if (cs != null) sim.flowCS = cs;
        }

        // Wire viewer
        viewer.sim = sim;
        viewer.target = raw;

        EditorUtility.SetDirty(sim);
        EditorUtility.SetDirty(viewer);
        EditorUtility.SetDirty(raw);
        EditorSceneManager.MarkSceneDirty(display.scene);

        Debug.Log("[Wire] Display bound to FlowLeniaSimulation.");
    }
}
#endif
