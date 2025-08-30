using UnityEngine;

[CreateAssetMenu(fileName = "FlowLeniaPresetLibrary", menuName = "Lenia/Flow/Preset Library")]
public class FlowLeniaPresetLibrary : ScriptableObject
{
    public FlowLeniaPreset[] presets;

    public int Count => presets != null ? presets.Length : 0;

    public FlowLeniaPreset Get(int index)
    {
        if (presets == null || presets.Length == 0) return null;
        return presets[Mathf.Clamp(index, 0, presets.Length - 1)];
    }

    public void ApplyPreset(int index, FlowLeniaSimulation sim, FlowLeniaDisplay view)
    {
        var p = Get(index);
        if (p == null) return;
        p.ApplyTo(sim, view);
    }

    // Legacy-friendly overload (not used by the controller after step 2A)
    public void ApplyPreset(int index, FlowLeniaSimulation sim, FlowLeniaDisplay view, bool _reseed)
        => ApplyPreset(index, sim, view);
}
