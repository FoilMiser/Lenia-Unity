#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MissingScriptCleaner
{
    [MenuItem("Lenia/Utilities/Remove Missing Scripts In Scene")]
    public static void RemoveAllMissing()
    {
        int total = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>()) {
            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
        Debug.Log($"[Lenia] Removed {total} missing script components from the scene.");
    }
}
#endif
