#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FlowDebugMenu
{
    [MenuItem("Tools/Lenia/Dump Flow Status")]
    public static void Dump()
    {
        var sim     = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        var display = GameObject.Find("Display");
        var raw     = display ? display.GetComponent<RawImage>() : null;
        var viewer  = display ? display.GetComponent<FlowLeniaViewer>() : null;

        Debug.Log("[FlowDebug] SIM=" + (sim ? sim.name : "null"));
        if (sim != null)
        {
            var rt = sim.CurrentTexture;
            var rtInfo = (rt != null) ? (rt.width + "x" + rt.height) : "null";
            var csName = sim.flowCS ? sim.flowCS.name : "null";
            Debug.Log("[FlowDebug] Sim: RT=" + rtInfo + ", flowCS=" + csName + ", w=" + sim.width + ", h=" + sim.height + ", channels=" + sim.channels);
        }

        Debug.Log("[FlowDebug] Display=" + (display ? display.name : "null")
            + ", Raw=" + (raw ? "ok" : "null") + ", Viewer=" + (viewer ? "ok" : "null"));

        if (raw != null)
        {
            var tex = raw.texture;
            var tname = tex ? tex.name : "null";
            var ttype = tex ? tex.GetType().Name : "-";
            Debug.Log("[FlowDebug] Raw.texture=" + tname + " type=" + ttype);
        }
    }

    [MenuItem("Tools/Lenia/Set Display To Test Card")]
    public static void SetTestCard()
    {
        var display = GameObject.Find("Display");
        if (!display) { Debug.LogWarning("[FlowDebug] No 'Display' GameObject."); return; }

        var raw = display.GetComponent<RawImage>();
        if (!raw) raw = display.AddComponent<RawImage>();

        const int N = 8;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false, true);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool a = (((x / 2) + (y / 2)) % 2) == 0;
                tex.SetPixel(x, y, a ? new Color(0.15f,0.15f,0.15f,1f) : new Color(0.85f,0.85f,0.85f,1f));
            }
        tex.Apply(false);
        raw.texture = tex;
        Debug.Log("[FlowDebug] Assigned test-card texture to RawImage.");
    }

    [MenuItem("Tools/Lenia/Force Init Sim (RT only)")]
    public static void ForceInitSim()
    {
        var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim) { Debug.LogWarning("[FlowDebug] No FlowLeniaSimulation in scene."); return; }
        sim.EnsureInitialized();
        var rt = sim.CurrentTexture;
        Debug.Log("[FlowDebug] EnsureInitialized called. RT now " + (rt ? (rt.width + "x" + rt.height) : "null"));
    }
}
#endif
