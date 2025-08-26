#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FlowViewerBindMenu
{
    [MenuItem("Tools/Lenia/Bind Viewer Now")]
    public static void Bind()
    {
        var v = Object.FindFirstObjectByType<FlowLeniaViewer>();
        if (v != null) v.BindNow();
        else Debug.LogWarning("[BindViewer] No FlowLeniaViewer in scene.");
    }
}
#endif
