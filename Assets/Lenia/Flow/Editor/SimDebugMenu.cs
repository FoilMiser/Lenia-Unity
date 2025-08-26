#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SimDebugMenu
{
    [MenuItem("Tools/Lenia/Attach CPU Debug Writer")]
    public static void Attach()
    {
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim){
            var go = new GameObject("FlowLeniaSimulation");
            sim = go.AddComponent<FlowLeniaSimulation>();
        }
        if (!sim.GetComponent<SimCpuDebugWriter>()) sim.gameObject.AddComponent<SimCpuDebugWriter>();
        Debug.Log("[SimDebug] CPU writer attached.");
    }
}
#endif
