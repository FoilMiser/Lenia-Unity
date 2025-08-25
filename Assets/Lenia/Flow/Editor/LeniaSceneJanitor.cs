#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeniaSceneJanitor
{
    [MenuItem("Tools/Lenia/Clean Scene (remove legacy)")]
    public static void CleanNow()
    {
        DoClean(log:true, renameFlow:true);
    }

    [InitializeOnLoadMethod]
    static void AutoOnce()
    {
        // Run gently on first reload; won’t spam if nothing to do.
        EditorApplication.delayCall += () => DoClean(log:false, renameFlow:false);
    }

    static void DoClean(bool log, bool renameFlow)
    {
        if (Application.isPlaying) return;

        string[] legacy = { "LeniaViewCanvas", "World Display", "Lenia" };
        foreach (var name in legacy)
        {
            var go = GameObject.Find(name);
            if (go)
            {
                Undo.DestroyObjectImmediate(go);
                if (log) Debug.Log($"[Janitor] Removed '{name}'.");
            }
        }

        if (renameFlow)
        {
            // Optional: make the Flow object obvious in Hierarchy
            var flow = Object.FindFirstObjectByType<FlowLeniaSimulation>();
            if (flow && flow.gameObject.name != "FlowLeniaSimulation")
            {
                Undo.RecordObject(flow.gameObject, "Rename Flow GO");
                flow.gameObject.name = "FlowLeniaSimulation";
                if (log) Debug.Log($"[Janitor] Renamed Flow GO to 'FlowLeniaSimulation'.");
            }
        }

        EditorSceneManager.MarkAllScenesDirty();
    }
}
#endif
