#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Простые команды редактора, чтобы добавить объект с RoundManager без ручной иерархии.
/// </summary>
public static class QuickArenaOps
{
    [MenuItem("FPS Shooter/Drop Empty Round Manager", priority = 11)]
    public static void DropRoundDirector()
    {
        var shell = new GameObject("RoundManager");
        Undo.RegisterCreatedObjectUndo(shell, "fps round nucleus");
        Undo.AddComponent<RoundManager>(shell);
        Selection.activeGameObject = shell;
        EditorUtility.DisplayDialog(
            "FPS Shooter",
            "Назначь в RoundManager HUD, массив Health и SpawnPoint объектов через инспектор.",
            "OK");
    }
}

#endif
