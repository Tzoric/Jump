using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MineLevelValidator
{
    private const string ScenePath = "Assets/Scenes/Level1_TheMines.unity";

    [MenuItem("Jump/Level Tools/Validate Level 1 - The Mines")]
    public static void Validate()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        HeroMovement hero = UnityEngine.Object.FindFirstObjectByType<HeroMovement>();
        Require(hero != null, "HeroMovement is missing.");
        Require(hero.GetComponent<PlayerHealth>() != null, "PlayerHealth is missing from the hero.");
        Require(hero.GetComponent<PlayerWeight>() != null, "PlayerWeight is missing from the hero.");

        Require(UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).Length >= 3,
            "Expected at least three moving platforms.");
        Require(UnityEngine.Object.FindObjectsByType<WeightedBreakablePlatform>(FindObjectsSortMode.None).Length >= 3,
            "Expected at least three weight-sensitive platforms.");
        Require(UnityEngine.Object.FindObjectsByType<FallingSpike>(FindObjectsSortMode.None).Length >= 4,
            "Expected at least four falling ceiling spikes.");
        Require(UnityEngine.Object.FindObjectsByType<DamageZone>(FindObjectsSortMode.None).Length >= 4,
            "Expected floor spikes and a mine-pit damage zone.");

        bool sceneInBuild = EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == ScenePath);
        Require(sceneInBuild, "The Mines scene is not enabled in Build Settings.");

        Debug.Log("MINE LEVEL VALIDATION PASSED: required player systems, platforms, hazards, HUD, and build scene exist.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"MINE LEVEL VALIDATION FAILED: {message}");
        }
    }
}
