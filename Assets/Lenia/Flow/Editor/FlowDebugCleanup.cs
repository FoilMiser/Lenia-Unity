#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public static class FlowDebugCleanup
{
    [InitializeOnLoadMethod]
    static void AutoRunOnce()
    {
        EditorApplication.delayCall += () => {
            if (SessionState.GetBool("FlowDebugCleanup_DidRun", false)) return;
            SessionState.SetBool("FlowDebugCleanup_DidRun", true);
            Run(true);
        };
    }

    [MenuItem("Tools/Lenia/Cleanup Debug Gradient (remove CPU writer, reset sim)")]
    public static void RunMenu() { Run(false); }

    static Component[] FindAllSceneComponents()
    {
        // Works across Unity versions: includes inactive, excludes assets/prefabs
        return Resources.FindObjectsOfTypeAll<Component>()
            .Where(c => c != null && c.gameObject != null && c.gameObject.scene.IsValid())
            .ToArray();
    }

    static T FindSceneObject<T>() where T : Object
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var o in all)
        {
            var comp = o as Component;
            if (comp != null && comp.gameObject.scene.IsValid()) return o;
        }
        return null;
    }

    static void Run(bool silent)
    {
        // 1) Remove all SimCpuDebugWriter instances from the scene
        var writers = FindAllSceneComponents();
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
        if (!silent) Debug.Log("[Cleanup] Removed " + removed + " SimCpuDebugWriter component(s).");

        // 2) Disable AssignCheckerNow so it won't overwrite a RenderTexture
        int disabled = 0;
        foreach (var c in writers)
        {
            if (!c) continue;
            var t = c.GetType();
            if (t != null && t.Name == "AssignCheckerNow")
            {
                var mb = c as Behaviour;
                if (mb && mb.enabled) { mb.enabled = false; disabled++; }
            }
        }
        if (!silent) Debug.Log("[Cleanup] Disabled " + disabled + " AssignCheckerNow component(s).");

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
        if (binder == null)
        {
            var t = System.Type.GetType("ForceBindSimToDisplay, Assembly-CSharp");
            if (t != null) binder = display.AddComponent(t) as Behaviour;
        }
        if (binder != null) binder.enabled = true;

        // 4) Disable FlowLeniaViewer to avoid tug-of-war
        var viewer = display.GetComponent("FlowLeniaViewer") as Behaviour;
        if (viewer && viewer.enabled) viewer.enabled = false;

        // 5) Reset and re-seed the sim
        var sim = FindSceneObject<FlowLeniaSimulation>();
        if (!sim)
        {
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }
        sim.EnsureInitialized();
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
            if (!silent) Debug.Log("[Cleanup] Deleted " + writerPath);
        }
    }
}
#endif
