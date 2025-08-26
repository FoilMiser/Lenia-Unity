#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowUiDebug
{
    [MenuItem("Tools/Lenia/Make Debug UI Overlay")]
    public static void MakeOverlay()
    {
        // Create a brand new overlay canvas with very high sorting order
        var dbgCanvas = new GameObject("DEBUG_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = dbgCanvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 5000; // on top of everything
        dbgCanvas.layer = LayerMask.NameToLayer("UI");

        // Fullscreen magenta panel
        var go = new GameObject("DEBUG_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(dbgCanvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 0f, 1f, 0.4f); // translucent magenta

        EditorUtility.SetDirty(dbgCanvas);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(dbgCanvas.scene);
        Debug.Log("[UI-DEBUG] Created DEBUG_Canvas with fullscreen magenta panel.");
    }

    [MenuItem("Tools/Lenia/Fix Existing Canvas + Display")]
    public static void FixExisting()
    {
        var canvas = GameObject.Find("Canvas");
        if (!canvas)
        {
            Debug.LogWarning("[UI-DEBUG] No 'Canvas' found. Run 'Make Debug UI Overlay' or create one.");
            return;
        }

        // Force overlay mode & sane sort order
        var c = canvas.GetComponent<Canvas>() ?? canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 0;
        canvas.layer = LayerMask.NameToLayer("UI");

        // Ensure Display RawImage exists and fills screen
        var display = GameObject.Find("Display");
        if (!display)
        {
            display = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            display.transform.SetParent(canvas.transform, false);
        }
        display.layer = LayerMask.NameToLayer("UI");

        var rt = display.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var raw = display.GetComponent<RawImage>();
        raw.color = Color.white;

        // Make absolutely sure something visible is assigned
        var checker = display.GetComponent<AssignCheckerNow>() ?? display.AddComponent<AssignCheckerNow>();
        checker.target = raw;

        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(display);
        EditorUtility.SetDirty(raw);
        EditorSceneManager.MarkSceneDirty(canvas.scene);

        Debug.Log("[UI-DEBUG] Canvas set to Overlay. Display RawImage stretched + checker attached.");
    }
}
#endif
