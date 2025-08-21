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
        var rmat = (ri != null) ? ri.material : null;
        var dispMat = GetPrivateField<Material>(sim, "_dispMat");

        string msg =
            "Lenia Diagnose:\n" +
            " RawImage.material = " + ((rmat != null) ? ((rmat.shader != null) ? rmat.shader.name : "NULL_SHADER") : "NULL") + "\n" +
            " _dispMat (internal) = " + ((dispMat != null) ? ((dispMat.shader != null) ? dispMat.shader.name : "NULL_SHADER") : "NULL") + "\n" +
            " Shader.Find(Unlit/LeniaPalette) = " + ((Shader.Find("Unlit/LeniaPalette") != null) ? "OK" : "NULL");
        Debug.Log(msg);
    }

    [MenuItem("Lenia/Fix: Force Palette Material")]
    public static void ForcePalette()
    {
        var sim = Object.FindObjectOfType<LeniaSimulation>();
        if (!sim) { Debug.LogError("No LeniaSimulation in scene."); return; }

        var sh = Shader.Find("Unlit/LeniaPalette");
        if (sh == null) { Debug.LogError("Shader Unlit/LeniaPalette not found. Reimport Assets/Shaders."); return; }

        var mat = new Material(sh);
        if (sim.view != null) {
            sim.view.material = mat;
            sim.view.color = Color.white;
        }
        sim.ApplyPreset(); // rebind textures/params
        Debug.Log("Applied LeniaPalette to RawImage.");
    }

    static T GetPrivateField<T>(object obj, string name) where T : class
    {
        var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (f != null) ? (f.GetValue(obj) as T) : null;
    }
}
