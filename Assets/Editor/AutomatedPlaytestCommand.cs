using System;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AutomatedPlaytestCommand
{
    private const string FirstLevelPath = "Assets/Scenes/Level1_TheMines.unity";

    [MenuItem("Jump/Playtest/Run Virtual Controller")]
    public static void Run()
    {
        Environment.SetEnvironmentVariable("JUMP_AUTOMATED_PLAYTEST", "1");
        EditorSceneManager.OpenScene(FirstLevelPath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}
