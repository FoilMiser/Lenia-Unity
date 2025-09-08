using UnityEditor;

[CustomEditor(typeof(LeniaParams))]
public class LeniaParamsEditor : Editor
{
    public override void OnInspectorGUI() { DrawDefaultInspector(); }
}
