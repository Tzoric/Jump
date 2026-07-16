using UnityEditor;
using UnityEditor.SceneManagement;

public static class MineMechanicsSmokeTestCommand
{
    [MenuItem("Jump/Playtest/Run Mine Mechanics Smoke Test")]
    public static void Run()
    {
        GameProgress.RestartAfterGameOver();
        EditorSceneManager.OpenScene("Assets/Scenes/Level1_TheMines.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}
