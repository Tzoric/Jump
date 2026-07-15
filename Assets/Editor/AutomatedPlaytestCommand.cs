using System;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AutomatedPlaytestCommand
{
    private const string DefaultLevelPath = "Assets/Scenes/Level1_TheMines.unity";

    [MenuItem("Jump/Playtest/Run Virtual Controller")]
    public static void Run()
    {
        string scenePath = GetOptionalArgument("-playtestScene") ?? DefaultLevelPath;
        scenePath = scenePath.Replace('\\', '/');
        if (!scenePath.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase) ||
            !scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"-playtestScene must name a Unity scene under Assets/Scenes. Received '{scenePath}'.");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            throw new ArgumentException($"The requested playtest scene does not exist: '{scenePath}'.");
        }

        GameProgress.RestartAfterGameOver();
        Environment.SetEnvironmentVariable("JUMP_AUTOMATED_PLAYTEST", "1");
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static string GetOptionalArgument(string argumentName)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length; index++)
        {
            if (!string.Equals(arguments[index], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException($"{argumentName} requires a scene path value.");
            }

            return arguments[index + 1];
        }

        return null;
    }
}
