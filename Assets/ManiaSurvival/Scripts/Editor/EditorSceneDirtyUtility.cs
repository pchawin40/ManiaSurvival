using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safe wrapper so editor setup tools never call MarkSceneDirty during Play Mode.
/// </summary>
public static class EditorSceneDirtyUtility
{
    public static void MarkActiveSceneDirtyIfEditing()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
    }

    public static void MarkSceneDirtyIfEditing(Scene scene)
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
#endif
    }
}
