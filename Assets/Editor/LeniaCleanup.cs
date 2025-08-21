using UnityEngine;
using UnityEditor;

public static class LeniaCleanup
{
    [MenuItem("Lenia/Cleanup Missing Scripts/In Scene")]
    public static void CleanScene()
    {
        int total = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>())
            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        Debug.Log($"Removed {total} missing script component(s) from open scene.");
    }

    [MenuItem("Lenia/Cleanup Missing Scripts/Across Prefabs")]
    public static void CleanPrefabs()
    {
        int total = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
            if (removed > 0) { total += removed; EditorUtility.SetDirty(prefab); }
        }
        if (total > 0) AssetDatabase.SaveAssets();
        Debug.Log($"Removed {total} missing script component(s) from prefabs.");
    }
}
