#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowKickstart
{
    [MenuItem("Tools/Lenia/Kickstart Sim and Bind")]
    public static void Kick()
    {
        // Ensure a FlowLeniaSimulation exists
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }

        // Make sure RTs exist & are seeded
        sim.EnsureInitialized();

        // Ensure a Canvas + Display RawImage
        var canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var display = GameObject.Find("Display");
        if (!display)
        {
            display = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            display.transform.SetParent(canvas.transform, false);
        }

        // Stretch full-screen
        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var raw = display.GetComponent<RawImage>();
        var viewer = display.GetComponent<FlowLeniaViewer>() ?? display.AddComponent<FlowLeniaViewer>();
        viewer.sim = sim;
        viewer.target = raw;

        // Bind the texture immediately
        if (sim.CurrentTexture != null)
            raw.texture = sim.CurrentTexture;

        EditorUtility.SetDirty(sim);
        EditorUtility.SetDirty(raw);
        EditorUtility.SetDirty(viewer);
        EditorSceneManager.MarkSceneDirty(display.scene);

        Debug.Log("[Kickstart] Flow sim initialized and bound to Display.");
    }
}
#endif
