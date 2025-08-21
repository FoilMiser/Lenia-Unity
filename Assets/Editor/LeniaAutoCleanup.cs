using UnityEngine;
using UnityEditor;

public static class LeniaAutoCleanup
{
    [InitializeOnLoadMethod]
    static void AutoClean()
    {
        EditorApplication.delayCall += () =>
        {
            // Clean the open scene
            int removedScene = 0;
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                removedScene += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            // Clean all prefabs in Assets
            int removedPrefabs = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;
                int r = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                if (r > 0) { removedPrefabs += r; EditorUtility.SetDirty(prefab); }
            }
            if (removedPrefabs > 0) AssetDatabase.SaveAssets();

            Debug.Log($"LeniaAutoCleanup: removed {removedScene} missing comps in scene, {removedPrefabs} in prefabs.");
        };
    }
}
