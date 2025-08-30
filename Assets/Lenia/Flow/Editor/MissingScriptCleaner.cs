using UnityEditor;
using UnityEngine;

public static class MissingScriptCleaner
{
    [MenuItem("Lenia/Tools/Remove Missing Scripts (Scene)")]
    public static void RemoveMissingFromScene()
    {
        int total = 0;
#if UNITY_2023_1_OR_NEWER
        var gos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
#pragma warning disable 618
        var gos = Object.FindObjectsOfType<GameObject>();
#pragma warning restore 618
#endif
        foreach (var go in gos)
        {
            int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (n > 0) total += n;
        }
        Debug.Log($"[Lenia] Removed {total} missing scripts from the open scene(s).");
    }

    [MenuItem("Lenia/Tools/Delete ScriptableObject Assets With Missing Script")]
    public static void DeleteSOAssetsWithMissingScript()
    {
        var guids = AssetDatabase.FindAssets("t:ScriptableObject");
        int deleted = 0, scanned = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (!obj) continue;

            // If the SO’s m_Script is null, it points to a missing class (like old FlowLeniaPreset assets)
            var so = new SerializedObject(obj);
            var mScript = so.FindProperty("m_Script");
            scanned++;

            if (mScript != null && mScript.objectReferenceValue == null)
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Delete ScriptableObject with missing script?",
                    $"Asset has a missing script:\n{path}\n\nDelete it?",
                    "Delete", "Skip"
                );
                if (ok)
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Lenia] Scanned {scanned} SO assets, deleted {deleted} with missing script.");
    }
}
