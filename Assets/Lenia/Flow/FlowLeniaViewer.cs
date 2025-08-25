using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlowLeniaViewer : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public RawImage target;

    void Awake()
    {
        if (!target) target = GetComponent<RawImage>();
        if (!sim) sim = FindObjectOfType<FlowLeniaSimulation>();
    }

    void OnValidate()
    {
        if (!target) target = GetComponent<RawImage>();
        if (!sim) sim = FindObjectOfType<FlowLeniaSimulation>();
    }

    void Update()
    {
        if (!sim || !target) return;
        var tex = sim.CurrentTexture;
        if (tex && target.texture != tex) target.texture = tex;
    }
}
