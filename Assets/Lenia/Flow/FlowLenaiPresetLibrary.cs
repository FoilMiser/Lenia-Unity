using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lenia/Flow Lenia Preset Library", fileName = "FlowLeniaPresetLibrary")]
public class FlowLeniaPresetLibrary : ScriptableObject
{
    public List<FlowLeniaPreset> presets = new List<FlowLeniaPreset>();
    public int Count => presets != null ? presets.Count : 0;

    public FlowLeniaPreset Get(int index)
    {
        if (presets == null || index < 0 || index >= presets.Count) return null;
        return presets[index];
    }

    public void ApplyPreset(int index, FlowLeniaSimulation sim)
    {
        var p = Get(index);
        if (p == null) return;
        p.ApplyTo(sim);
    }

    // ---- Quick built-ins pulled from animals.json (codes & params) ----
    // Orbium unicaudatus:         R=13, T=10, m=0.15, s=0.015
    // Orbium unicaudatus ignis:   R=13, T=10, m=0.11, s=0.012
    // Orbium bicaudatus:          R=13, T=10, m=0.15, s=0.014
    // Orbium bicaudatus ignis:    R=13, T=10, m=0.10, s=0.008
    // Orbium phantasma:           R=13, T=40, m=0.13, s=0.009

    public void PopulateBuiltIns()
    {
        presets = new List<FlowLeniaPreset>
        {
            Make("O2u",  "Orbium unicaudatus",        13, 10, 0.15f, 0.015f),
            Make("O2ui", "Orbium unicaudatus ignis",  13, 10, 0.11f, 0.012f),
            Make("O2b",  "Orbium bicaudatus",         13, 10, 0.15f, 0.014f),
            Make("O2bi", "Orbium bicaudatus ignis",   13, 10, 0.10f, 0.008f),
            Make("O2p",  "Orbium phantasma",          13, 40, 0.13f, 0.009f),
        };
    }

    static FlowLeniaPreset Make(string id, string title, int R, int T, float m, float s)
    {
        var p = new FlowLeniaPreset { id = id, title = title, R = R, mu = m, sigma = s };
        p.SetDtFromT(T);
        p.alpha = 1.0f; p.beta = 0.25f; p.temperature = 0.9f; p.maxSpeed = 1.0f;
        p.boundary = BoundaryMode.Wrap; p.scatter = ScatterMode.Bilinear;
        return p;
    }
}
