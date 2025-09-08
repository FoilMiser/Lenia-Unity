using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LeniaParams))]
public class LeniaParamsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var p = (LeniaParams)target;
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Kernels (β editing)", EditorStyles.boldLabel);
        foreach (var k in p.kernels)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(k.name);
            k.relativeRadius = EditorGUILayout.Slider("Relative Radius", k.relativeRadius, 0.1f, 4f);
            k.rank = Mathf.Max(1, EditorGUILayout.IntField("Rank (B)", k.rank));
            k.alpha = EditorGUILayout.Slider("Alpha (KC)", k.alpha, 0.1f, 8f);
            k.weight = EditorGUILayout.FloatField("Weight (h_k)", k.weight);

            if (k.beta == null || k.beta.Length != k.rank) k.beta = new float[k.rank];
            for (int i = 0; i < k.rank; i++)
                k.beta[i] = EditorGUILayout.Slider($"β[{i+1}]", k.beta[i], 0f, 1f);

            EditorGUILayout.EndVertical();
        }
        if (GUI.changed) EditorUtility.SetDirty(target);
    }
}