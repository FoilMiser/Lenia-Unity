#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowHardWireMenu
{
    [MenuItem("Tools/Lenia/0) Hard-Wire Display (Checker + Sim Binder)")]
    public static void HardWire()
    {
        // Ensure Canvas (Screen Space Overlay)
        var canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Ensure Display RawImage
        var display = GameObject.Find("Display");
        if (!display)
        {
            display = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            display.transform.SetParent(canvas.transform, false);
        }

        // Full-screen stretch
        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var raw = display.GetComponent<RawImage>();
        raw.color = Color.white; // ensure visible

        // Attach AssignCheckerNow (always shows a texture)
        var checker = display.GetComponent<AssignCheckerNow>() ?? display.AddComponent<AssignCheckerNow>();
        checker.target = raw;

        // Attach ForceBindSimToDisplay (binds sim RT if available)
        var binder = display.GetComponent<ForceBindSimToDisplay>() ?? display.AddComponent<ForceBindSimToDisplay>();
        binder.target = raw;
        binder.sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();

        // Make sure a FlowLeniaSimulation exists
        if (!binder.sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            binder.sim = go.AddComponent<FlowLeniaSimulation>();
        }

        // Save
        EditorUtility.SetDirty(raw);
        EditorUtility.SetDirty(checker);
        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(binder.sim);
        EditorSceneManager.MarkSceneDirty(display.scene);

        Debug.Log("[HardWire] Checker + Sim binder attached to Canvas/Display.");
    }
}
#endif
