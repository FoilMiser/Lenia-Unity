using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ForceBindSimToDisplay : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public RawImage target;
    public bool logEverySecond = false;

    float tLog;

    void OnEnable()
    {
        if (!target) target = GetComponent<RawImage>();
        if (!sim)    sim    = Object.FindObjectOfType<FlowLeniaSimulation>();
    }

    void Update()
    {
        if (!target) target = GetComponent<RawImage>();
        if (!sim)    sim    = Object.FindObjectOfType<FlowLeniaSimulation>();
        if (!sim || !target) return;

        sim.EnsureInitialized();
        var rt = sim.CurrentTexture;
        if (rt != null && target.texture != rt) target.texture = rt;

        if (logEverySecond)
        {
            tLog += Time.unscaledDeltaTime;
            if (tLog > 1f)
            {
                Debug.Log("[Binder] sim RT = " + (rt ? (rt.width + "x" + rt.height) : "null"));
                tLog = 0f;
            }
        }
    }
}
