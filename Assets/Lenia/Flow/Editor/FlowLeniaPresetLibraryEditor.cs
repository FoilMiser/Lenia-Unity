using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlowLeniaPresetLibrary))]
public class FlowLeniaPresetLibraryEditor : Editor
{
    SerializedProperty _presetsProp;

    void OnEnable()
    {
        _presetsProp = serializedObject.FindProperty("presets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Flow-Lenia Preset Library\n" +
            "• Click “Populate Built-ins” to auto-fill Orbium variants.\n" +
            "• Click “Capture From Current Scene” to snapshot the first FlowLeniaSimulation you have in the scene.\n" +
            "• Edit the list below directly. Assign this asset to FlowLeniaControl.presetLibrary.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Built-ins (from animals.json parameters we mirrored)
        if (GUILayout.Button("Populate Built-ins (Orbium variants)"))
        {
            var lib = (FlowLeniaPresetLibrary)target;
            Undo.RecordObject(lib, "Populate Built-ins");
            lib.PopulateBuiltIns();
            EditorUtility.SetDirty(lib);
        }

        // Capture a preset from current scene
        if (GUILayout.Button("Capture Preset From Current Scene"))
        {
            var lib = (FlowLeniaPresetLibrary)target;

#if UNITY_2023_1_OR_NEWER
            var sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
            if (sim == null) sim = Object.FindAnyObjectByType<FlowLeniaSimulation>();
#else
#pragma warning disable 618
            var sim = Object.FindObjectOfType<FlowLeniaSimulation>();
#pragma warning restore 618
#endif
            if (sim == null)
            {
                EditorUtility.DisplayDialog(
                    "No Simulation Found",
                    "Couldn't find a FlowLeniaSimulation in the open scene.",
                    "OK"
                );
            }
            else
            {
                Undo.RecordObject(lib, "Capture Preset");
                lib.presets.Add(BuildPresetFromSim(sim));
                EditorUtility.SetDirty(lib);
            }
        }

        EditorGUILayout.Space();

        // Show and edit the list
        EditorGUILayout.PropertyField(_presetsProp, includeChildren: true);

        serializedObject.ApplyModifiedProperties();
    }

    // Snapshot the current sim into a FlowLeniaPreset
    static FlowLeniaPreset BuildPresetFromSim(FlowLeniaSimulation sim)
    {
        var p = new FlowLeniaPreset
        {
            id    = "captured",
            title = "Captured",
            dt          = sim.dt,
            alpha       = sim.alpha,
            beta        = sim.beta,
            temperature = sim.temperature,
            maxSpeed    = sim.maxSpeed,
            boundary    = sim.boundary,
            scatter     = sim.scatterMode
        };

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Prefer model → R, mu, sigma, h
        var modelField = sim.GetType().GetField("model", flags);
        var model = modelField != null ? modelField.GetValue(sim) : null;
        if (model != null)
        {
            var tModel = model.GetType();
            var fR = tModel.GetField("R", flags);
            if (fR != null) p.R = Mathf.Max(1, (int)fR.GetValue(model));

            var fKernels = tModel.GetField("kernels", flags) ?? tModel.GetField("Kernels", flags);
            var kernels = fKernels != null ? fKernels.GetValue(model) as System.Collections.IList : null;
            if (kernels != null && kernels.Count > 0 && kernels[0] != null)
            {
                var k  = kernels[0];
                var tk = k.GetType();
                var fmu    = tk.GetField("mu", flags);    if (fmu != null)    p.mu    = (float)fmu.GetValue(k);
                var fsigma = tk.GetField("sigma", flags); if (fsigma != null) p.sigma = (float)fsigma.GetValue(k);
                var fh     = tk.GetField("h", flags);     if (fh != null)     p.h     = (float)fh.GetValue(k);
            }
        }
        else
        {
            // Fallback: read direct fields if this sim variant exposes them
            p.R     = ReadInt(sim, "R", 13, flags);
            int ksz = ReadInt(sim, "kernelSize", 2 * p.R + 1, flags);
            if (ksz >= 3) p.R = Mathf.Max(1, (ksz - 1) / 2);

            p.mu    = ReadFloat(sim, "mu",    p.mu,    flags);
            p.sigma = ReadFloat(sim, "sigma", p.sigma, flags);
            p.h     = ReadFloat(sim, "h",     p.h,     flags);
        }

        return p;
    }

    static int ReadInt(object o, string name, int def, BindingFlags flags)
    {
        var f = o.GetType().GetField(name, flags);
        if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(o);
        var p = o.GetType().GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(o, null);
        return def;
    }

    static float ReadFloat(object o, string name, float def, BindingFlags flags)
    {
        var f = o.GetType().GetField(name, flags);
        if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(o);
        var p = o.GetType().GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(float)) return (float)p.GetValue(o, null);
        return def;
    }
}
