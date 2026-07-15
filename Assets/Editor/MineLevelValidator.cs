using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MineLevelValidator
{
    private const string Overview = "Assets/Scenes/DungeonOverview.unity";
    private const string Level1 = "Assets/Scenes/Level1_TheMines.unity";
    private const string Level2 = "Assets/Scenes/Level2_SlidingAscent.unity";

    [MenuItem("Jump/Level Tools/Validate Mines Levels")]
    public static void Validate()
    {
        ValidateOverview();
        ValidateLevel(Level1, 11, false, false);
        ValidateLevel(Level2, 6, true, true);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Require(scenes.SequenceEqual(new[] { Overview, Level1, Level2 }), "Build Settings must contain overview, Level 1, and Level 2 in order.");
        EditorSceneManager.OpenScene(Overview, OpenSceneMode.Single);
        Debug.Log("MINES VALIDATION PASSED: camera, five mineshaft nodes, shop, two levels, doors, hazards, crystals, health, lives, and miner presentation are ready.");
    }

    private static void ValidateOverview()
    {
        EditorSceneManager.OpenScene(Overview, OpenSceneMode.Single);
        Require(UnityEngine.Object.FindFirstObjectByType<Camera>() != null, "Overview has no camera.");
        Require(UnityEngine.Object.FindFirstObjectByType<MineShopController>() != null, "Overview shop is missing.");
        Require(UnityEngine.Object.FindObjectsByType<SceneLoadButton>(FindObjectsSortMode.None).Length == 2, "Overview needs playable Level 1 and Level 2 nodes.");
        for (int i = 1; i <= 5; i++) Require(GameObject.Find($"Mineshaft {i}") != null, $"Mineshaft {i} level node is missing.");
    }

    private static void ValidateLevel(string path, int waypointCount, bool needsCrystals, bool needsSpikes)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        HeroMovement hero = UnityEngine.Object.FindFirstObjectByType<HeroMovement>();
        Require(hero != null, $"{path} has no hero.");
        Require(hero.GetComponent<PlayerHealth>() != null && hero.GetComponent<MinerOutfitVisual>() != null, $"{path} hero lacks health or miner outfit.");
        Require(UnityEngine.Object.FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None).Length == waypointCount, $"{path} route waypoint count is wrong.");
        LevelExitDoor door = UnityEngine.Object.FindFirstObjectByType<LevelExitDoor>();
        Require(door != null && door.DestinationScene == "DungeonOverview", $"{path} exit is missing or misconfigured.");
        Require(GameObject.Find("Exit Door Foundation (Required)") != null, $"{path} exit door has no platform foundation.");
        int crystals = UnityEngine.Object.FindObjectsByType<GreenCrystalCollectible>(FindObjectsSortMode.None).Length;
        int hazards = UnityEngine.Object.FindObjectsByType<DamageZone>(FindObjectsSortMode.None).Count(zone => zone.name.Contains("Spike"));
        Require((crystals > 0) == needsCrystals, $"{path} crystal rule is incorrect.");
        Require((hazards > 0) == needsSpikes, $"{path} spike rule is incorrect.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"MINES VALIDATION FAILED: {message}");
    }
}
