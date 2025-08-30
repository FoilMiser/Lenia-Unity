using UnityEngine;

[CreateAssetMenu(fileName = "FlowLeniaPreset", menuName = "Lenia/Flow/Preset")]
public class FlowLeniaPreset : ScriptableObject
{
    [Header("Dynamics (simulation)")]
    public float dt = 0.8f;
    public float alpha = 1f;
    public float beta = 1f;
    public float temperature = 1f;
    public float maxSpeed = 0f;                 // 0 = unlimited
    public BoundaryMode boundary = BoundaryMode.Wrap;
    public ScatterMode  scatterMode = ScatterMode.Auto;

    [Header("Display (optional)")]
    public Gradient palette;
    public float exposure = 1f;

    public void ApplyTo(FlowLeniaSimulation sim)
    {
        if (!sim) return;
        sim.dt          = dt;
        sim.alpha       = alpha;
        sim.beta        = beta;
        sim.temperature = temperature;
        sim.maxSpeed    = maxSpeed;
        sim.boundary    = boundary;
        sim.scatterMode = scatterMode;
    }

    public void ApplyTo(FlowLeniaSimulation sim, FlowLeniaDisplay view)
    {
        ApplyTo(sim);
        if (view && palette != null)
        {
            view.palette  = palette;
            view.exposure = exposure;
        }
    }
}
