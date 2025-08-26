#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FlowAutoWire
{
    static FlowAutoWire(){ EditorApplication.delayCall += Run; }

    [MenuItem("Tools/Lenia/Auto-Wire Display Now")]
    public static void Run()
    {
        if (Application.isPlaying) return;

        // Canvas
        var canvas = GameObject.Find("Canvas");
        if (!canvas){
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        } else {
            var c = canvas.GetComponent<Canvas>();
            if (c && c.renderMode != RenderMode.ScreenSpaceOverlay) c.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Display RawImage
        var display = GameObject.Find("Display");
        if (!display){
            display = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            display.transform.SetParent(canvas.transform, false);
        }
        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var raw = display.GetComponent<RawImage>();
        raw.color = Color.white;

        // Ensure checker + binder
        var checker = display.GetComponent<AssignCheckerNow>() ?? display.AddComponent<AssignCheckerNow>();
        checker.target = raw;
        var binder = display.GetComponent<ForceBindSimToDisplay>() ?? display.AddComponent<ForceBindSimToDisplay>();
        binder.target = raw;
        if (!binder.sim){
            var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
            if (!sim){ var go = new GameObject("FlowLeniaSimulation"); sim = go.AddComponent<FlowLeniaSimulation>(); }
            binder.sim = sim;
        }

        EditorUtility.SetDirty(raw);
        EditorUtility.SetDirty(checker);
        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(binder.sim);
        EditorSceneManager.MarkSceneDirty(display.scene);
        Debug.Log("[AutoWire] Canvas/Display ready, checker attached; sim binder will override in Play.");
    }
}
#endif
