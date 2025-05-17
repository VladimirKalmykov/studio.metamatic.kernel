#if UNITY_EDITOR
using Metamatic.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class SystemGizmoAnchorAutoCreator
{
    const string AnchorName = "__SystemGizmoAnchor__";

    static SystemGizmoAnchorAutoCreator()
    {
        EditorSceneManager.sceneOpened     += (_,__) => ScheduleEnsure();
        EditorApplication.hierarchyChanged += ScheduleEnsure;
        ScheduleEnsure();
    }

    static void ScheduleEnsure()
    {
        // вместо прямого вызова — отложим на next tick
        EditorApplication.delayCall += EnsureAnchorExists;
    }

    static void EnsureAnchorExists()
    {
        // сразу "отпишемся", чтобы не накапливать делегаты
        EditorApplication.delayCall -= EnsureAnchorExists;

        if (Object.FindFirstObjectByType<SystemGizmoAnchor>() != null)
            return;

        var go = new GameObject(AnchorName, typeof(SystemGizmoAnchor));
        go.tag       = "EditorOnly";
        Undo.RegisterCreatedObjectUndo(go, "Create SystemGizmoAnchor");
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
#endif