using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MineLevelValidator
{
    private const string LevelScenePath = "Assets/Scenes/Level1_TheMines.unity";
    private const string OverviewScenePath = "Assets/Scenes/DungeonOverview.unity";

    [MenuItem("Jump/Level Tools/Validate Level 1 - The Mines")]
    public static void Validate()
    {
        ValidateLevelScene();
        ValidateOverviewScene();
        ValidateBuildSettings();
        EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        Debug.Log("MINE LEVEL VALIDATION PASSED: vertical route, exit door, dungeon overview, HUD, and build scenes are ready.");
    }

    private static void ValidateLevelScene()
    {
        EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

        HeroMovement hero = UnityEngine.Object.FindFirstObjectByType<HeroMovement>();
        Require(hero != null, "HeroMovement is missing.");
        Require(hero.GetComponent<PlayerHealth>() != null, "PlayerHealth is missing from the hero.");
        Require(hero.GetComponent<PlayerWeight>() != null, "PlayerWeight is missing from the hero.");

        LevelExitDoor exitDoor = UnityEngine.Object.FindFirstObjectByType<LevelExitDoor>();
        Require(exitDoor != null, "The level exit door is missing.");
        Require(exitDoor.DestinationScene == "DungeonOverview", "The exit door does not lead to DungeonOverview.");
        Require(exitDoor.GetComponent<Collider2D>().isTrigger, "The exit door collider must be a trigger.");

        AutomatedPlaytestWaypoint[] waypoints =
            UnityEngine.Object.FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None);
        Require(waypoints.Length == 11, $"Expected 11 authored route waypoints but found {waypoints.Length}.");
        Require(waypoints.Select(waypoint => waypoint.Order).Distinct().Count() == waypoints.Length,
            "Every automated playtest waypoint needs a unique order.");

        Require(UnityEngine.Object.FindFirstObjectByType<VerticalCameraFollow>() != null,
            "The vertical camera follow component is missing.");
        Require(GameObject.Find("Bronze Mine Shaft Backdrop") != null,
            "The Level 1 bronze mine background is missing.");
        Require(GameObject.Find("Beginner Vertical Route") != null,
            "The beginner vertical platform route is missing.");
        Require(GameObject.Find("Level HUD") != null, "The Level 1 HUD is missing.");
        Require(GameObject.FindGameObjectsWithTag("BlueCrystal").Length == 0 &&
                GameObject.FindGameObjectsWithTag("BlackBigCrystal").Length == 0,
            "Level 1 should use the exit door, not required crystals, as its completion condition.");
    }

    private static void ValidateOverviewScene()
    {
        Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(OverviewScenePath) != null,
            "DungeonOverview.unity does not exist.");
        EditorSceneManager.OpenScene(OverviewScenePath, OpenSceneMode.Single);

        SceneLoadButton levelButton = UnityEngine.Object.FindFirstObjectByType<SceneLoadButton>();
        Require(levelButton != null, "The dungeon overview has no Level 1 button.");
        Require(levelButton.TargetScene == "Level1_TheMines", "The Level 1 button points to the wrong scene.");
        Require(levelButton.GetComponent<Button>() != null, "The Level 1 selector is missing its Button component.");
        Require(GameObject.Find("Mine Overview Background") != null, "The Mines overview background is missing.");
        Require(GameObject.Find("Mine Material Progression") != null, "The material progression display is missing.");
    }

    private static void ValidateBuildSettings()
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Require(enabledScenes.Contains(OverviewScenePath), "DungeonOverview is not enabled in Build Settings.");
        Require(enabledScenes.Contains(LevelScenePath), "Level 1 is not enabled in Build Settings.");
        Require(enabledScenes[0] == OverviewScenePath, "DungeonOverview should be the first startup scene.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"MINE LEVEL VALIDATION FAILED: {message}");
        }
    }
}
