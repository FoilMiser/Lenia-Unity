#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeniaRemoveMissing
{
    [MenuItem("Tools/Lenia/Remove Missing Scripts (Scene)")]
    public static void RemoveMissing()
    {
        if (Application.isPlaying) return;
        int removedTotal = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (before > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                removedTotal += before;
            }
        }
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[Lenia] Removed {removedTotal} missing script component(s).");
    }
}
#endif
