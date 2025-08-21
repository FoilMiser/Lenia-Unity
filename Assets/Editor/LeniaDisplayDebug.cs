using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class LeniaDisplayDebug
{
    [MenuItem("Lenia/Diagnose Display Binding")]
    public static void Diagnose()
    {
        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim) { Debug.LogError("No LeniaSimulation in scene."); return; }
        var ri = sim.view;
        var rmat = ri ? ri.material : null;
        Debug.Log($"Lenia Diagnose:\n RawImage.material = {(rmat ? rmat.shader?.name : "NULL")}\n _dispMat (internal) = {(GetPrivateField<Material>(sim, \"_dispMat\")?.shader?.name ?? "NULL")}\n Shader.Find(\"Unlit/LeniaPalette\") = {(Shader.Find(\"Unlit/LeniaPalette\") ? "OK" : "NULL")}");
    }

    [MenuItem("Lenia/Fix: Force Palette Material")]
    public static void ForcePalette()
    {
        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim) { Debug.LogError("No LeniaSimulation in scene."); return; }
        var sh = Shader.Find("Unlit/LeniaPalette");
        if (!sh) { Debug.LogError("Shader Unlit/LeniaPalette not found. Reimport Assets/Shaders."); return; }
        var mat = new Material(sh);
        sim.view.material = mat;
        sim.view.color = Color.white;
        sim.ApplyPreset(); // rebind all textures/params
        Debug.Log("Applied LeniaPalette to RawImage.");
    }

    static T GetPrivateField<T>(object obj, string name) where T : class
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return f?.GetValue(obj) as T;
    }
}
