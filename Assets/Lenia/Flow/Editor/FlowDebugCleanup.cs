#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FlowDebugCleanup
{
    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        // Run once after scripts reload
        EditorApplication.delayCall += () => {
            if (SessionState.GetBool("FlowDebugCleanup_DidRun", false)) return;
            SessionState.SetBool("FlowDebugCleanup_DidRun", true);
            Run(true);
        };
    }

    [MenuItem("Tools/Lenia/Cleanup Debug Gradient (remove CPU writer, reset sim)")]
    public static void RunMenu() => Run(false);

    static void Run(bool silent)
    {
        // 1) Remove all SimCpuDebugWriter instances from the scene
        var writers = Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
        int removed = 0;
        foreach (var c in writers)
        {
            if (!c) continue;
            var t = c.GetType();
            if (t != null && t.Name == "SimCpuDebugWriter")
            {
                Object.DestroyImmediate(c, allowDestroyingAssets:false);
                removed++;
            }
        }
        if (!silent) Debug.Log($"[Cleanup] Removed {removed} SimCpuDebugWriter component(s).");

        // 2) Disable AssignCheckerNow so it won't overwrite a RenderTexture
        var checkers = Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
        int disabled = 0;
        foreach (var c in checkers)
        {
            if (!c) continue;
            var t = c.GetType();
            if (t != null && t.Name == "AssignCheckerNow")
            {
                var mb = c as Behaviour;
                if (mb && mb.enabled) { mb.enabled = false; disabled++; }
            }
        }
        if (!silent) Debug.Log($"[Cleanup] Disabled {disabled} AssignCheckerNow component(s).");

        // 3) Ensure binder is present & wired on Canvas/Display
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
            var rt = display.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        var raw = display.GetComponent<RawImage>();
        var binder = display.GetComponent("ForceBindSimToDisplay") as Behaviour;
        if (binder == null) binder = display.AddComponent(System.Type.GetType("ForceBindSimToDisplay, Assembly-CSharp")) as Behaviour;
        if (binder != null) binder.enabled = true;

        // 4) (Optional) Disable FlowLeniaViewer to avoid tug-of-war
        var viewer = display.GetComponent("FlowLeniaViewer") as Behaviour;
        if (viewer && viewer.enabled) viewer.enabled = false;

        // 5) Reset and re-seed the sim
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }
        sim.EnsureInitialized();
        // call the ResetState() method we added
        var m = typeof(FlowLeniaSimulation).GetMethod("ResetState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (m != null) m.Invoke(sim, null);

        EditorUtility.SetDirty(sim);
        EditorUtility.SetDirty(raw);
        EditorSceneManager.MarkSceneDirty(display.scene);

        if (!silent) Debug.Log("[Cleanup] Sim reset and binder wired. If you still see the gradient, there is another writer in the scene.");
        // 6) Delete the SimCpuDebugWriter.cs asset if present
        string writerPath = "Assets/Lenia/Flow/SimCpuDebugWriter.cs";
        if (System.IO.File.Exists(writerPath))
        {
            AssetDatabase.DeleteAsset(writerPath);
            if (!silent) Debug.Log("[Cleanup] Deleted Assets/Lenia/Flow/SimCpuDebugWriter.cs");
        }
    }
}
#endif
