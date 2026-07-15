using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MineLevelValidator
{
    private const string Overview = "Assets/Scenes/DungeonOverview.unity";
    private const string GameOver = "Assets/Scenes/GameOver.unity";
    private const string PlatformArt = "Assets/Art/Generated/MineRockBronzePlatform.png";
    private const string MinerArt = "Assets/Art/Generated/MinerCharacterV2.png";
    private const string MinerAnimationArt = "Assets/Art/Generated/MinerAnimationSheet.png";
    private const string PickArt = "Assets/Art/Generated/MinerPickaxe.png";

    private enum ShaftDirection
    {
        Vertical,
        Angled,
        Horizontal
    }

    private readonly struct LevelExpectation
    {
        public readonly int Number;
        public readonly string SceneName;
        public readonly ShaftDirection Direction;
        public readonly int MinimumWaypoints;

        public string Path => $"Assets/Scenes/{SceneName}.unity";

        public LevelExpectation(int number, string sceneName, ShaftDirection direction, int minimumWaypoints)
        {
            Number = number;
            SceneName = sceneName;
            Direction = direction;
            MinimumWaypoints = minimumWaypoints;
        }
    }

    private static readonly LevelExpectation[] Levels =
    {
        new(1, "Level1_TheMines", ShaftDirection.Vertical, 11),
        new(2, "Level2_SlidingAscent", ShaftDirection.Angled, 6),
        new(3, "Level3_ChasmRun", ShaftDirection.Horizontal, 9),
        new(4, "Level4_CopperColumn", ShaftDirection.Vertical, 16),
        new(5, "Level5_CrookedIncline", ShaftDirection.Angled, 10),
        new(6, "Level6_BrokenRail", ShaftDirection.Horizontal, 12),
        new(7, "Level7_FurnaceRise", ShaftDirection.Vertical, 21),
        new(8, "Level8_RazorAscent", ShaftDirection.Angled, 14),
        new(9, "Level9_AbyssRun", ShaftDirection.Horizontal, 16),
        new(10, "Level10_KeyVault", ShaftDirection.Vertical, 26),
        new(11, "Level11_TreasureVein", ShaftDirection.Angled, 18)
    };

    [MenuItem("Jump/Level Tools/Validate Bronze Mines Levels 1-11")]
    public static void Validate()
    {
        ValidateEconomyRules();
        ValidatePlatformArtwork();
        ValidateAnimationArtwork();
        ValidateOverview();
        ValidateGameOver();

        var routeLengths = new Dictionary<int, float>();
        foreach (LevelExpectation level in Levels)
        {
            routeLengths[level.Number] = ValidateLevel(level);
        }

        ValidateIncreasingLengths(routeLengths);
        ValidateBuildSettings();

        EditorSceneManager.OpenScene(Overview, OpenSceneMode.Single);
        Debug.Log("MINES VALIDATION PASSED: overview, Game Over restart flow, eleven levels, route headroom, Level 2 recovery geometry, safe bottomless pits, progression, economy, and miner presentation are ready.");
    }

    private static void ValidateEconomyRules()
    {
        Require(GameProgress.BaseHearts == 5, "A level must begin with five base hearts.");
        Require(GameProgress.StartingLives == 3, "A new game must begin with three lives.");
        Require(GameProgress.HealthPotionPrice == 3, "A health potion must cost three green gems.");
        Require(GameProgress.ExtraLifePrice == 25, "An extra life must cost 25 green gems.");
        Require(Mathf.Approximately(RewardChest.BlueGemChance, .50f), "The chest blue-gem chance must be 50%.");
        Require(Mathf.Approximately(RewardChest.PotionChance, .45f), "The chest health-potion chance must be 45%.");
        Require(Mathf.Approximately(RewardChest.ExtraLifeChance, .05f), "The chest extra-life chance must be 5%.");
        Require(Mathf.Approximately(
            RewardChest.BlueGemChance + RewardChest.PotionChance + RewardChest.ExtraLifeChance, 1f),
            "Chest reward chances must total 100%.");
    }

    private static void ValidatePlatformArtwork()
    {
        Require(File.Exists(ProjectFilePath(PlatformArt)), $"Generated platform artwork is missing at {PlatformArt}.");
        byte[] png = File.ReadAllBytes(ProjectFilePath(PlatformArt));
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Require(texture.LoadImage(png, false), "The bronze-veined rock platform PNG cannot be decoded.");
            Color32[] pixels = texture.GetPixels32();
            int opaque = pixels.Count(pixel => pixel.a >= 128);
            int transparent = pixels.Length - opaque;
            int darkRock = pixels.Count(pixel => pixel.a >= 128 && pixel.r < 120 && pixel.g < 125 && pixel.b < 135);
            int bronze = pixels.Count(pixel => pixel.a >= 128 && pixel.r >= 120 && pixel.g >= 55 && pixel.g <= 175 && pixel.b < 115 && pixel.r > pixel.b + 45);
            int interiorBronze = 0;
            for (int y = 2; y < texture.height - 2; y++)
            {
                for (int x = 2; x < texture.width - 2; x++)
                {
                    Color32 pixel = pixels[y * texture.width + x];
                    if (pixel.a >= 128 && pixel.r >= 120 && pixel.g >= 55 && pixel.g <= 175 && pixel.b < 115 && pixel.r > pixel.b + 45)
                    {
                        interiorBronze++;
                    }
                }
            }

            Require(opaque > pixels.Length / 3, "Platform art does not contain enough visible rock.");
            Require(transparent > pixels.Length / 25, "Platform art needs an irregular, transparent-edged rock silhouette.");
            Require(darkRock > opaque * .35f, "Platform art must read primarily as dark mine rock.");
            Require(bronze > opaque * .01f && bronze < opaque * .30f, "Platform art needs restrained, visible bronze veins.");
            Require(interiorBronze > bronze * .25f, "Bronze must vein through the rocks rather than only frame the platform edge.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void ValidateAnimationArtwork()
    {
        Require(File.Exists(ProjectFilePath(MinerAnimationArt)),
            $"Miner animation sheet is missing at {MinerAnimationArt}.");
        byte[] png = File.ReadAllBytes(ProjectFilePath(MinerAnimationArt));
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Require(texture.LoadImage(png, false), "Miner animation sheet cannot be decoded.");
            Require(texture.width >= 600 && texture.height >= 500,
                "Miner animation sheet is too small for a readable 6-by-5 frame grid.");
            Color32[] pixels = texture.GetPixels32();
            for (int row = 0; row < 5; row++)
            for (int column = 0; column < 6; column++)
            {
                int x0 = Mathf.RoundToInt(column * texture.width / 6f);
                int x1 = Mathf.RoundToInt((column + 1) * texture.width / 6f);
                int y0 = Mathf.RoundToInt(row * texture.height / 5f);
                int y1 = Mathf.RoundToInt((row + 1) * texture.height / 5f);
                int visible = 0;
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (pixels[y * texture.width + x].a >= 64) visible++;
                Require(visible >= 500,
                    $"Miner animation grid cell row {row + 1}, column {column + 1} is empty.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void ValidateOverview()
    {
        OpenScene(Overview);
        Camera[] cameras = SceneComponents<Camera>();
        Require(cameras.Length == 1, "Overview must have exactly one rendering camera.");
        Camera camera = cameras[0];
        Require(camera.enabled && camera.gameObject.activeInHierarchy, "Overview camera is disabled.");
        Require(camera.CompareTag("MainCamera"), "Overview camera must use the MainCamera tag.");
        Require(camera.gameObject.GetComponent<AudioListener>() != null, "Overview camera needs its AudioListener.");
        Require(camera.targetTexture == null && camera.targetDisplay == 0, "Overview camera must render to Display 1.");
        Require(SceneComponents<MineShopController>().Length == 1, "Overview shop is missing or duplicated.");

        MineLevelSelectButton[] nodes = SceneComponents<MineLevelSelectButton>();
        Require(nodes.Length == Levels.Length, "Overview must contain exactly 11 playable mineshaft nodes.");
        foreach (LevelExpectation level in Levels)
        {
            MineLevelSelectButton[] matches = nodes.Where(node => node.LevelNumber == level.Number).ToArray();
            Require(matches.Length == 1, $"Overview needs one and only one Level {level.Number} node.");
            Require(matches[0].TargetScene == level.SceneName, $"Mineshaft {level.Number} targets the wrong scene.");
            Require(SceneTransforms().Count(transform => transform.name == $"Mineshaft {level.Number}") == 1,
                $"Mineshaft {level.Number} artwork/button object is missing or duplicated.");
        }
    }

    private static void ValidateGameOver()
    {
        OpenScene(GameOver);
        Camera[] cameras = SceneComponents<Camera>();
        Require(cameras.Length == 1, "Game Over scene must have exactly one rendering camera.");
        Camera camera = cameras[0];
        Require(camera.enabled && camera.gameObject.activeInHierarchy && camera.CompareTag("MainCamera"),
            "Game Over camera is disabled or not tagged MainCamera.");
        Require(camera.targetTexture == null && camera.targetDisplay == 0,
            "Game Over camera must render to Display 1.");

        GameOverController[] controllers = SceneComponents<GameOverController>();
        Require(controllers.Length == 1, "Game Over scene needs exactly one GameOverController.");
        Require(controllers[0].RestartScene == "DungeonOverview",
            "Game Over Restart must return to DungeonOverview.");
        Require(SceneComponents<Canvas>().Length >= 1,
            "Game Over scene needs a visible UI canvas.");
        UnityEngine.UI.Button[] restartButtons = SceneComponents<UnityEngine.UI.Button>().Where(button =>
            button.name.IndexOf("restart", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(restartButtons.Length == 1,
            "Game Over scene needs exactly one visible Restart button.");
        UnityEngine.UI.Button restart = restartButtons[0];
        Require(restart.onClick.GetPersistentEventCount() == 1 &&
                restart.onClick.GetPersistentTarget(0) == controllers[0] &&
                restart.onClick.GetPersistentMethodName(0) == nameof(GameOverController.RestartGame),
            "Game Over Restart button must have one persistent listener targeting GameOverController.RestartGame.");
    }

    private static float ValidateLevel(LevelExpectation level)
    {
        OpenScene(level.Path);
        ValidateSceneCamera(level);

        HeroMovement[] heroes = SceneComponents<HeroMovement>();
        Require(heroes.Length == 1, $"{level.SceneName} must contain exactly one hero.");
        HeroMovement hero = heroes[0];
        PlayerHealth health = hero.GetComponent<PlayerHealth>();
        MinerOutfitVisual outfit = hero.GetComponent<MinerOutfitVisual>();
        MineRunInventory inventory = hero.GetComponent<MineRunInventory>();
        Require(health != null && outfit != null && inventory != null,
            $"{level.SceneName} hero lacks health, run inventory, or miner presentation.");
        Require(inventory.LevelNumber == level.Number, $"{level.SceneName} run inventory has the wrong level number.");
        ValidateHeroPresentationAndTuning(level, hero, health, outfit);

        AutomatedPlaytestWaypoint[] waypoints = SceneComponents<AutomatedPlaytestWaypoint>();
        Require(waypoints.Length >= level.MinimumWaypoints,
            $"{level.SceneName} needs at least {level.MinimumWaypoints} ordered route waypoints.");
        int[] orders = waypoints.Select(waypoint => waypoint.Order).OrderBy(order => order).ToArray();
        Require(orders.Distinct().Count() == orders.Length && orders.First() == 1,
            $"{level.SceneName} route waypoint order must be unique and begin at 1.");
        for (int index = 0; index < orders.Length; index++)
        {
            Require(orders[index] == index + 1, $"{level.SceneName} route waypoint order has a gap at {index + 1}.");
        }

        ValidateDoor(level);
        ValidateKeysAndChest(level);
        ValidateSpikes(level);
        ValidatePlatformMaterialsAndThickness(level);
        ValidateOrdinaryHeadroom(level, hero);

        float routeLength = level.Number == 2
            ? ValidateLevel2Route(hero)
            : ValidateDirection(level, hero);

        ValidateLevel11Treasure(level);
        return routeLength;
    }

    private static void ValidateSceneCamera(LevelExpectation level)
    {
        Camera[] cameras = SceneComponents<Camera>();
        Require(cameras.Length == 1, $"{level.SceneName} must have exactly one rendering camera.");
        Camera camera = cameras[0];
        Require(camera.enabled && camera.gameObject.activeInHierarchy && camera.CompareTag("MainCamera"),
            $"{level.SceneName} camera is disabled or not tagged MainCamera.");
        Require(camera.orthographic && camera.orthographicSize > 0f,
            $"{level.SceneName} camera must be a usable orthographic gameplay camera.");
        Require(camera.targetTexture == null && camera.targetDisplay == 0,
            $"{level.SceneName} camera must render to Display 1.");
    }

    private static void ValidateHeroPresentationAndTuning(
        LevelExpectation level, HeroMovement hero, PlayerHealth health, MinerOutfitVisual outfit)
    {
        float speed = SerializedFloat(hero, "speed");
        float jumpForce = SerializedFloat(hero, "jumpForce");
        float jumpAnticipation = SerializedFloat(hero, "jumpAnticipationSeconds");
        Require(Mathf.Abs(speed - 7.5f) <= .05f,
            $"{level.SceneName} hero speed must be the slowed 7.5 target (75% of the original speed)." );
        Require(jumpForce >= 11.5f && jumpForce <= 13f,
            $"{level.SceneName} jump should use the slightly higher, movement-matched 11.5-13 range.");
        Require(jumpAnticipation >= .06f && jumpAnticipation <= .10f,
            $"{level.SceneName} jump needs a visible 60-100ms grounded squat before takeoff.");
        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        Require(body != null && body.gravityScale >= 5f && body.gravityScale <= 5.8f,
            $"{level.SceneName} hero gravity does not match the slower movement tuning.");
        Require(hero.transform.lossyScale.x >= 1.75f && hero.transform.lossyScale.x <= 2.1f,
            $"{level.SceneName} miner should remain approximately 125% of the original hero size.");
        Require(SerializedInt(health, "maxHealth") == GameProgress.BaseHearts,
            $"{level.SceneName} serialized base health must be five hearts.");

        SpriteRenderer[] renderers = hero.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer miner = renderers.FirstOrDefault(renderer =>
            renderer.sprite != null && NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(MinerArt));
        SpriteRenderer pick = renderers.FirstOrDefault(renderer =>
            renderer.sprite != null && NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(PickArt));
        Require(miner != null && miner.enabled, $"{level.SceneName} must use the completely remade miner sprite.");
        Require(pick != null && pick.enabled, $"{level.SceneName} miner needs the separate hand-held pick sprite.");
        Require(outfit.VisualRenderer == miner, $"{level.SceneName} miner outfit is not driving the remade body renderer.");
        CharacterOutfitDefinition definition = outfit.Outfit;
        Require(definition != null && definition.CharacterIdentity == "Main Hero" && definition.OutfitId == "bronze_miner",
            $"{level.SceneName} must use the reusable Main Hero bronze-miner outfit profile.");
        Require(definition.AnimationSheet != null &&
                NormalizePath(AssetDatabase.GetAssetPath(definition.AnimationSheet)) == NormalizePath(MinerAnimationArt),
            $"{level.SceneName} outfit profile is missing its 30-frame animation sheet.");
        TextureImporter animationImporter = AssetImporter.GetAtPath(MinerAnimationArt) as TextureImporter;
        Require(animationImporter != null && animationImporter.isReadable,
            "Miner animation sheet must stay readable for runtime frame slicing and future outfit swaps.");
        Require(pick.transform.IsChildOf(hero.transform), $"{level.SceneName} pick must move as part of the miner hierarchy.");
        SerializedProperty handPickaxe = new SerializedObject(outfit).FindProperty("handPickaxe");
        Transform handRig = handPickaxe?.objectReferenceValue as Transform;
        Require(handRig != null && (pick.transform == handRig || pick.transform.IsChildOf(handRig)),
            $"{level.SceneName} smaller pick must be attached to the hand rig animated by MinerOutfitVisual.");
        Require(pick.bounds.size.magnitude < miner.bounds.size.magnitude * .75f,
            $"{level.SceneName} mining pick is not visibly smaller than the miner.");
        Require(!hero.GetComponentsInChildren<Transform>(true).Any(transform =>
                transform != hero.transform && (transform.name.IndexOf("hat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                transform.name.IndexOf("helmet", StringComparison.OrdinalIgnoreCase) >= 0)),
            $"{level.SceneName} helmet must be integrated into the new miner sprite, not layered on separately.");
    }

    private static void ValidateDoor(LevelExpectation level)
    {
        LevelExitDoor[] doors = SceneComponents<LevelExitDoor>();
        Require(doors.Length == 1, $"{level.SceneName} must contain exactly one exit door.");
        LevelExitDoor door = doors[0];
        Require(door.DestinationScene == "DungeonOverview" && door.LevelNumber == level.Number,
            $"{level.SceneName} exit destination or completed-level number is wrong.");
        Require(SerializedFloat(door, "entranceSeconds") >= .5f,
            $"{level.SceneName} door needs a visible walk-in cutscene.");
        Collider2D doorTrigger = door.GetComponent<Collider2D>();
        Require(doorTrigger != null && doorTrigger.isTrigger, $"{level.SceneName} exit door trigger is missing.");

        Transform[] foundations = SceneTransforms()
            .Where(transform => transform.name == "Exit Door Foundation (Required)").ToArray();
        Require(foundations.Length == 1, $"{level.SceneName} exit door must have one required foundation platform.");
        Collider2D foundation = foundations[0].GetComponent<Collider2D>();
        Require(foundation != null && !foundation.isTrigger, $"{level.SceneName} door foundation is not solid.");
        Require(foundation.bounds.max.y < door.transform.position.y &&
                door.transform.position.y - foundation.bounds.max.y < 4f &&
                foundation.bounds.min.x < door.transform.position.x && foundation.bounds.max.x > door.transform.position.x,
            $"{level.SceneName} door foundation is not positioned directly beneath the door.");
    }

    private static void ValidateKeysAndChest(LevelExpectation level)
    {
        BronzeKeyCollectible[] bronzeKeys = SceneComponents<BronzeKeyCollectible>();
        RewardChest[] chests = SceneComponents<RewardChest>();
        Require(bronzeKeys.Length == 1 && bronzeKeys[0].LevelNumber == level.Number,
            $"{level.SceneName} needs exactly one same-level bronze key.");
        Require(chests.Length == 1 && chests[0].LevelNumber == level.Number,
            $"{level.SceneName} needs exactly one same-level one-time reward chest.");

        SilverKeyCollectible[] silverKeys = SceneComponents<SilverKeyCollectible>();
        Require(silverKeys.Length == (level.Number == 10 ? 1 : 0),
            level.Number == 10
                ? "Level 10 must hide the silver key that unlocks Level 11."
                : $"{level.SceneName} must not duplicate the Level 10 silver key.");
        if (level.Number == 10)
        {
            DamageZone[] nearbySpikes = SceneComponents<DamageZone>().Where(zone =>
                zone.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0 &&
                Vector2.Distance(zone.transform.position, silverKeys[0].transform.position) <= 2.5f).ToArray();
            Require(nearbySpikes.Length > 0 &&
                    silverKeys[0].transform.parent != null &&
                    silverKeys[0].transform.parent.name.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0,
                "Level 10 silver key must be hidden on a dangerous optional route.");
        }
    }

    private static void ValidateSpikes(LevelExpectation level)
    {
        DamageZone[] spikes = SceneComponents<DamageZone>()
            .Where(zone => zone.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        if (level.Number >= 2)
        {
            Require(spikes.Length > 0, $"{level.SceneName} needs at least one spike hazard.");
        }

        foreach (DamageZone spike in spikes)
        {
            Require(SerializedInt(spike, "damage") == 1,
                $"{level.SceneName} spike '{spike.name}' must remove exactly one heart per hit.");
        }
    }

    private static void ValidatePlatformMaterialsAndThickness(LevelExpectation level)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        BoxCollider2D[] platforms = SceneComponents<BoxCollider2D>()
            .Where(collider => !collider.isTrigger && collider.gameObject.layer == groundLayer &&
                               collider.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0 &&
                               collider.name.IndexOf("boundary", StringComparison.OrdinalIgnoreCase) < 0).ToArray();
        Require(platforms.Length > 0, $"{level.SceneName} has no solid mine platforms.");

        foreach (BoxCollider2D platform in platforms)
        {
            SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite != null &&
                    NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(PlatformArt),
                $"{level.SceneName} platform '{platform.name}' is not using the bronze-veined rock artwork.");
            float localThickness = platform.size.y * Mathf.Abs(platform.transform.lossyScale.y);
            Require(localThickness <= .85f,
                $"{level.SceneName} platform '{platform.name}' is too thick vertically ({localThickness:0.00}).");
        }
    }

    private static void ValidateOrdinaryHeadroom(LevelExpectation level, HeroMovement hero)
    {
        Collider2D heroCollider = hero.GetComponentsInChildren<Collider2D>(true)
            .FirstOrDefault(collider => !collider.isTrigger);
        Require(heroCollider != null, $"{level.SceneName} hero needs a solid standing collider for headroom validation.");
        float requiredClearance = heroCollider.bounds.size.y + .75f;

        int groundLayer = LayerMask.NameToLayer("Ground");
        BoxCollider2D[] platforms = SceneComponents<BoxCollider2D>()
            .Where(collider => !collider.isTrigger && collider.gameObject.layer == groundLayer &&
                               collider.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0 &&
                               collider.name.IndexOf("boundary", StringComparison.OrdinalIgnoreCase) < 0)
            .ToArray();

        for (int firstIndex = 0; firstIndex < platforms.Length; firstIndex++)
        for (int secondIndex = firstIndex + 1; secondIndex < platforms.Length; secondIndex++)
        {
            BoxCollider2D first = platforms[firstIndex];
            BoxCollider2D second = platforms[secondIndex];
            if (HasIntentionalHeadBumpExemption(first.transform) ||
                HasIntentionalHeadBumpExemption(second.transform))
            {
                continue;
            }

            BoxCollider2D lower = first.bounds.center.y <= second.bounds.center.y ? first : second;
            BoxCollider2D upper = lower == first ? second : first;
            float horizontalOverlap = Mathf.Min(lower.bounds.max.x, upper.bounds.max.x) -
                                      Mathf.Max(lower.bounds.min.x, upper.bounds.min.x);
            if (horizontalOverlap <= .2f)
            {
                continue;
            }

            float centerDeltaX = Mathf.Abs(upper.bounds.center.x - lower.bounds.center.x);
            float centerDeltaY = Mathf.Abs(upper.bounds.center.y - lower.bounds.center.y);
            if (centerDeltaX >= centerDeltaY)
            {
                continue; // Laterally adjoining pieces are not an overhead stack.
            }

            float clearance = upper.bounds.min.y - lower.bounds.max.y;
            Require(clearance + .01f >= requiredClearance,
                $"{level.SceneName} ordinary platforms '{lower.name}' and '{upper.name}' provide only " +
                $"{clearance:0.00} headroom; {requiredClearance:0.00} is required. " +
                "A deliberate exception must be parented under an object named exactly 'Intentional Head-Bump Challenge'.");
        }
    }

    private static bool HasIntentionalHeadBumpExemption(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (string.Equals(current.name, "Intentional Head-Bump Challenge", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static float ValidateLevel2Route(HeroMovement hero)
    {
        BoxCollider2D[] upper = RoutePlatforms("upper").Where(platform =>
            Mathf.Abs(SignedZ(platform.transform.eulerAngles.z)) <= 1f &&
            platform.name.IndexOf("upper", StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(platform => platform.bounds.center.x)
            .ToArray();
        int groundLayer = LayerMask.NameToLayer("Ground");
        BoxCollider2D[] ramp = SceneComponents<BoxCollider2D>().Where(platform =>
            !platform.isTrigger && platform.gameObject.layer == groundLayer &&
            platform.name.IndexOf("ramp", StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(platform => platform.bounds.center.x)
            .ToArray();

        Require(upper.Length >= 6, "Level 2 needs at least six individually horizontal upper platforms.");
        Require(upper.Zip(upper.Skip(1), (left, right) => right.bounds.center.y > left.bounds.center.y + .2f).All(value => value),
            "Level 2 upper platform centers must consistently rise while moving right.");
        float upperTrend = Mathf.Atan2(
            upper[^1].bounds.center.y - upper[0].bounds.center.y,
            upper[^1].bounds.center.x - upper[0].bounds.center.x) * Mathf.Rad2Deg;
        Require(upperTrend >= 12f && upperTrend <= 28f,
            $"Level 2 upper route must form a readable diagonal ascent; measured trend is {upperTrend:0.0} degrees.");
        Require(ApproximateDistinctCount(upper.Select(platform => platform.bounds.size.x), .35f) >= 3,
            "Level 2 needs at least three materially distinct upper-platform widths.");

        float[] horizontalGaps = ConsecutiveHorizontalGaps(upper);
        float[] verticalSteps = upper.Zip(upper.Skip(1),
            (left, right) => right.bounds.center.y - left.bounds.center.y).ToArray();
        Require(horizontalGaps.Length == upper.Length - 1 && horizontalGaps.All(gap => gap >= .75f),
            "Every Level 2 upper jump needs a meaningful visible horizontal gap of at least 0.75 world units.");
        Require(ApproximateDistinctCount(horizontalGaps, .2f) >= 2 &&
                ApproximateDistinctCount(verticalSteps, .2f) >= 2,
            "Level 2 consecutive jumps must vary in both horizontal and vertical distance.");

        Require(ramp.Length >= 4, "Level 2 needs a continuous multi-piece lower sliding ramp.");
        Require(ramp.All(platform => Mathf.Abs(SignedZ(platform.transform.eulerAngles.z) - 18f) <= 1f),
            "Every Level 2 recovery-ramp segment must rise right at +18 degrees.");
        Require(ramp.Zip(ramp.Skip(1), (left, right) => right.bounds.center.y > left.bounds.center.y).All(value => value),
            "Level 2 recovery ramp must rise toward the right, leaving its downhill end at bottom-left.");
        Require(ramp.All(platform => platform.sharedMaterial != null && platform.sharedMaterial.friction <= .05f),
            "Level 2 lower ramp must use a low-friction material so falls reliably slide to the bottom.");
        Require(Mathf.Abs(upperTrend - 18f) <= 5f,
            "Level 2 upper ascent and +18-degree recovery ramp must read as approximately parallel.");
        Require(upper.All(platform =>
        {
            BoxCollider2D nearestRamp = ramp.OrderBy(segment =>
                Mathf.Abs(segment.bounds.center.x - platform.bounds.center.x)).First();
            return nearestRamp.bounds.max.y + .5f < platform.bounds.min.y;
        }), "Level 2 recovery ramp must remain clearly below the upper gap route.");

        Transform rampRoot = ramp[0].transform.parent;
        DamageZone[] rampSpikes = SceneComponents<DamageZone>().Where(zone =>
            zone.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0 &&
            rampRoot != null && zone.transform.IsChildOf(rampRoot)).ToArray();
        Require(rampSpikes.Length >= 3, "Level 2 lower ramp needs several one-heart spikes.");
        Require(rampSpikes.All(spike => Mathf.Abs(SignedZ(spike.transform.eulerAngles.z)) <= 5f),
            "Level 2 ramp spikes must point within five degrees of world up, not inherit ramp rotation.");
        Require(rampSpikes.All(spike => ramp.Any(segment =>
                Mathf.Sqrt(segment.bounds.SqrDistance(spike.transform.position)) <= .8f)),
            "Every Level 2 spike must be visibly seated on the recovery ramp.");

        Component[] retryZones = SceneComponents<Component>()
            .Where(component => component.GetType().Name == "LevelRetryZone").ToArray();
        Require(retryZones.Length == 1,
            "Level 2 needs exactly one LevelRetryZone at the ramp bottom.");
        Collider2D retryTrigger = retryZones[0].GetComponent<Collider2D>();
        Require(retryTrigger != null && retryTrigger.isTrigger && retryZones[0].GetComponent<DamageZone>() == null,
            "Level 2 retry must be a non-damaging trigger rather than a life-costing pit.");
        Require(Vector3.Distance(SerializedPosition(retryZones[0], "resetPosition"), hero.transform.position) <= .1f,
            "Level 2 retry zone must send the miner back to the route's starting position.");
        Require(retryTrigger.bounds.center.x < ramp.Min(segment => segment.bounds.center.x) &&
                retryTrigger.bounds.center.y < ramp.Min(segment => segment.bounds.center.y),
            "Level 2 retry trigger must sit at the ramp's downhill bottom-left end.");
        Require(!SceneTransforms().Any(transform => transform.name.IndexOf("Bottomless Pit", StringComparison.OrdinalIgnoreCase) >= 0),
            "Level 2 gaps must drop onto the retry ramp, not into bottomless pits.");

        SpriteRenderer backdrop = SceneComponents<SpriteRenderer>().FirstOrDefault(renderer =>
            renderer.name.IndexOf("backdrop", StringComparison.OrdinalIgnoreCase) >= 0);
        Require(backdrop != null && Mathf.Abs(SignedZ(backdrop.transform.eulerAngles.z)) >= 5f,
            "Level 2 background artwork must be angled to match the sliding mine shaft.");
        return Range(upper.Select(platform => platform.bounds.center.x));
    }

    private static float ValidateDirection(LevelExpectation level, HeroMovement hero)
    {
        switch (level.Direction)
        {
            case ShaftDirection.Vertical:
            {
                Require(SceneComponents<VerticalCameraFollow>().Length == 1,
                    $"{level.SceneName} must use the vertical shaft camera.");
                BoxCollider2D[] route = RoutePlatforms("vertical");
                Require(route.Length >= level.MinimumWaypoints,
                    $"{level.SceneName} vertical route is too short.");
                float vertical = Range(route.Select(platform => platform.bounds.center.y));
                float horizontal = Range(route.Select(platform => platform.bounds.center.x));
                Require(vertical > horizontal * 1.5f, $"{level.SceneName} does not read as a straight-up shaft.");
                return vertical;
            }
            case ShaftDirection.Angled:
            {
                Require(SceneComponents<BoundedCameraFollow>().Length == 1,
                    $"{level.SceneName} must use an angled bounded camera.");
                BoxCollider2D[] route = RoutePlatforms("angled");
                Require(route.Length >= level.MinimumWaypoints,
                    $"{level.SceneName} angled route is too short.");
                Require(route.Count(platform => Mathf.Abs(SignedZ(platform.transform.eulerAngles.z)) >= 8f) >= level.MinimumWaypoints,
                    $"{level.SceneName} needs angled shaft platforms rather than a flat route.");
                float horizontal = Range(route.Select(platform => platform.bounds.center.x));
                float vertical = Range(route.Select(platform => platform.bounds.center.y));
                Require(horizontal > 15f && vertical > 5f, $"{level.SceneName} does not travel up and across.");
                return horizontal;
            }
            case ShaftDirection.Horizontal:
            {
                Require(SceneComponents<BoundedCameraFollow>().Length == 1,
                    $"{level.SceneName} must use a bounded horizontal camera.");
                BoxCollider2D[] route = RoutePlatforms("horizontal");
                Require(route.Length >= level.MinimumWaypoints,
                    $"{level.SceneName} horizontal route is too short.");
                Require(route.All(platform => Mathf.Abs(SignedZ(platform.transform.eulerAngles.z)) <= 1f),
                    $"{level.SceneName} horizontal route contains tilted main platforms.");
                ValidateHorizontalPits(level, hero, route);
                return Range(route.Select(platform => platform.bounds.center.x));
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void ValidateHorizontalPits(
        LevelExpectation level, HeroMovement hero, IEnumerable<BoxCollider2D> routePlatforms)
    {
        BoxCollider2D[] route = routePlatforms.OrderBy(platform => platform.bounds.center.x).ToArray();
        Collider2D heroCollider = hero.GetComponentsInChildren<Collider2D>(true)
            .FirstOrDefault(collider => !collider.isTrigger);
        Require(heroCollider != null, $"{level.SceneName} hero needs a collider for pit-clearance validation.");

        DamageZone[] fatalZones = SceneComponents<DamageZone>().Where(zone =>
            zone.name.IndexOf("bottomless", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(fatalZones.Length > 0, $"{level.SceneName} needs lethal bottomless-pit coverage.");
        Require(fatalZones.All(zone => SerializedInt(zone, "damage") >= GameProgress.BaseHearts),
            $"{level.SceneName} bottomless zones must be fatal, not one-heart hazards.");
        Collider2D[] fatalTriggers = fatalZones.Select(zone => zone.GetComponent<Collider2D>()).ToArray();
        Require(fatalTriggers.All(trigger => trigger != null && trigger.isTrigger),
            $"{level.SceneName} every bottomless DamageZone needs a trigger collider.");

        float minimumGap = Mathf.Max(.75f, heroCollider.bounds.size.x * .9f);
        float[] gapWidths = ConsecutiveHorizontalGaps(route);
        Require(gapWidths.Length == route.Length - 1 && gapWidths.All(width => width >= minimumGap),
            $"{level.SceneName} needs meaningful visible gaps at least {minimumGap:0.00} units wide; " +
            $"smallest is {(gapWidths.Length == 0 ? 0f : gapWidths.Min()):0.00}.");

        float safeDeathCeiling = route.Min(platform => platform.bounds.min.y) - .5f;
        foreach (Collider2D fatal in fatalTriggers)
        {
            Require(fatal.bounds.max.y <= safeDeathCeiling + .01f,
                $"{level.SceneName} fatal trigger '{fatal.name}' reaches y={fatal.bounds.max.y:0.00}; " +
                $"it must remain at or below the safe ceiling y={safeDeathCeiling:0.00}.");
            Require(route.All(platform => !Intersects2D(fatal.bounds, platform.bounds)),
                $"{level.SceneName} fatal trigger '{fatal.name}' overlaps a visible route platform.");
            Require(!Intersects2D(fatal.bounds, heroCollider.bounds),
                $"{level.SceneName} fatal trigger '{fatal.name}' overlaps the hero spawn collider.");
        }

        float heroHalfWidth = heroCollider.bounds.extents.x;
        float heroHeight = heroCollider.bounds.size.y;
        foreach (BoxCollider2D platform in route)
        {
            Bounds standingEnvelope = BoundsFromEdges(
                platform.bounds.min.x - heroHalfWidth - .1f,
                platform.bounds.max.x + heroHalfWidth + .1f,
                platform.bounds.min.y - .25f,
                platform.bounds.max.y + heroHeight + .75f);
            Require(fatalTriggers.All(fatal => !Intersects2D(fatal.bounds, standingEnvelope)),
                $"{level.SceneName} has a fatal trigger inside the required landing/standing envelope of '{platform.name}'.");
        }

        AutomatedPlaytestWaypoint[] waypoints = SceneComponents<AutomatedPlaytestWaypoint>();
        foreach (AutomatedPlaytestWaypoint waypoint in waypoints)
        {
            Bounds waypointEnvelope = BoundsFromEdges(
                waypoint.transform.position.x - heroHalfWidth,
                waypoint.transform.position.x + heroHalfWidth,
                waypoint.transform.position.y - heroHeight * .5f,
                waypoint.transform.position.y + heroHeight * .5f);
            Require(fatalTriggers.All(fatal => !Intersects2D(fatal.bounds, waypointEnvelope)),
                $"{level.SceneName} fatal geometry overlaps required waypoint {waypoint.Order}.");
        }

        Collider2D[] localized = fatalTriggers.Where(trigger =>
            trigger.name.StartsWith("Bottomless Pit", StringComparison.OrdinalIgnoreCase)).ToArray();
        for (int index = 0; index < localized.Length; index++)
        {
            Collider2D pit = localized[index];
            int matchingGap = FindContainingGap(route, pit.bounds.center.x);
            Require(matchingGap >= 0,
                $"{level.SceneName} localized trigger '{pit.name}' is not centered beneath a visible gap.");
            float gapLeft = route[matchingGap].bounds.max.x;
            float gapRight = route[matchingGap + 1].bounds.min.x;
            Require(pit.bounds.min.x >= gapLeft - .01f && pit.bounds.max.x <= gapRight + .01f,
                $"{level.SceneName} localized trigger '{pit.name}' extends outside its visible gap " +
                $"[{gapLeft:0.00}, {gapRight:0.00}].");
        }

        Collider2D[] preferredCoverage = localized.Length > 0 ? localized : fatalTriggers;
        for (int index = 0; index < route.Length - 1; index++)
        {
            float gapLeft = route[index].bounds.max.x;
            float gapRight = route[index + 1].bounds.min.x;
            float midpoint = (gapLeft + gapRight) * .5f;
            Require(preferredCoverage.Any(trigger =>
                    trigger.bounds.min.x <= midpoint && trigger.bounds.max.x >= midpoint &&
                    trigger.bounds.max.y <= safeDeathCeiling + .01f),
                $"{level.SceneName} visible gap {index + 1} has no safely placed lethal coverage.");

            float corridorBottom = Mathf.Min(route[index].bounds.min.y, route[index + 1].bounds.min.y) - .25f;
            float corridorTop = Mathf.Max(route[index].bounds.max.y, route[index + 1].bounds.max.y) + heroHeight + .75f;
            Bounds jumpCorridor = BoundsFromEdges(
                gapLeft - heroHalfWidth, gapRight + heroHalfWidth, corridorBottom, corridorTop);
            Require(fatalTriggers.All(trigger => !Intersects2D(trigger.bounds, jumpCorridor)),
                $"{level.SceneName} fatal geometry intrudes into the normal jump corridor for gap {index + 1}.");
        }
    }

    private static void ValidateLevel11Treasure(LevelExpectation level)
    {
        if (level.Number != 11)
        {
            return;
        }

        GreenCrystalCollectible[] gems = SceneComponents<GreenCrystalCollectible>();
        Require(gems.Count(gem => gem.Value == 1) >= 20, "Level 11 needs plentiful green gems off the safe route.");
        Require(gems.Count(gem => gem.Value == 5) == 5, "Level 11 must contain exactly five difficult blue gems worth five each.");
        Require(gems.Count(gem => gem.Value == 20) == 1, "Level 11 must contain exactly one extremely difficult purple gem worth 20.");
        Require(gems.All(gem => gem.Value == 1 || gem.Value == 5 || gem.Value == 20),
            "Level 11 contains a gem with an unsupported value.");

        DamageZone[] spikes = SceneComponents<DamageZone>().Where(zone =>
            zone.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        GreenCrystalCollectible[] blue = gems.Where(gem => gem.Value == 5).ToArray();
        GreenCrystalCollectible purple = gems.Single(gem => gem.Value == 20);
        Require(blue.All(gem => spikes.Any(spike => Vector2.Distance(spike.transform.position, gem.transform.position) <= 2.5f)),
            "Every Level 11 blue gem must require a dangerous spike-side detour.");
        Require(spikes.Count(spike => Vector2.Distance(spike.transform.position, purple.transform.position) <= 2.5f) >= 2,
            "The Level 11 purple gem must be guarded by multiple nearby spikes.");
    }

    private static void ValidateIncreasingLengths(IReadOnlyDictionary<int, float> lengths)
    {
        Require(lengths[4] > lengths[1] + 5f && lengths[7] > lengths[4] + 5f && lengths[10] > lengths[7] + 5f,
            "Straight-up Levels 1, 4, 7, and 10 must become progressively longer.");
        Require(lengths[8] > lengths[5] + 10f && lengths[11] > lengths[8] + 10f,
            "Angled Levels 5, 8, and 11 must become progressively longer.");
        Require(lengths[6] > lengths[3] + 10f && lengths[9] > lengths[6] + 10f,
            "Horizontal Levels 3, 6, and 9 must become progressively longer.");
    }

    private static void ValidateBuildSettings()
    {
        string[] expected = new[] { Overview, GameOver }.Concat(Levels.Select(level => level.Path)).ToArray();
        string[] actual = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Require(actual.SequenceEqual(expected),
            "Build Settings must contain DungeonOverview, GameOver, then Bronze Mines Levels 1-11 in order.");
    }

    private static BoxCollider2D[] RoutePlatforms(string routeNameFragment)
    {
        Transform routeRoot = SceneTransforms().FirstOrDefault(transform =>
            transform.name.IndexOf(routeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0 &&
            transform.name.IndexOf("route", StringComparison.OrdinalIgnoreCase) >= 0);
        Require(routeRoot != null, $"Scene is missing its {routeNameFragment} route root.");
        int groundLayer = LayerMask.NameToLayer("Ground");
        return routeRoot.GetComponentsInChildren<BoxCollider2D>(true)
            .Where(collider => !collider.isTrigger && collider.gameObject.layer == groundLayer).ToArray();
    }

    private static float[] ConsecutiveHorizontalGaps(IEnumerable<BoxCollider2D> colliders)
    {
        BoxCollider2D[] ordered = colliders.OrderBy(collider => collider.bounds.min.x).ToArray();
        var gaps = new List<float>();
        for (int index = 1; index < ordered.Length; index++)
        {
            gaps.Add(ordered[index].bounds.min.x - ordered[index - 1].bounds.max.x);
        }

        return gaps.ToArray();
    }

    private static int ApproximateDistinctCount(IEnumerable<float> values, float tolerance)
    {
        var representatives = new List<float>();
        foreach (float value in values.OrderBy(value => value))
        {
            if (representatives.All(existing => Mathf.Abs(existing - value) >= tolerance))
            {
                representatives.Add(value);
            }
        }

        return representatives.Count;
    }

    private static int FindContainingGap(IReadOnlyList<BoxCollider2D> orderedPlatforms, float x)
    {
        for (int index = 0; index < orderedPlatforms.Count - 1; index++)
        {
            if (x >= orderedPlatforms[index].bounds.max.x &&
                x <= orderedPlatforms[index + 1].bounds.min.x)
            {
                return index;
            }
        }

        return -1;
    }

    private static Bounds BoundsFromEdges(float minX, float maxX, float minY, float maxY)
    {
        return new Bounds(
            new Vector3((minX + maxX) * .5f, (minY + maxY) * .5f, 0f),
            new Vector3(Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY), 1f));
    }

    private static bool Intersects2D(Bounds first, Bounds second)
    {
        const float epsilon = .001f;
        return first.min.x < second.max.x - epsilon && first.max.x > second.min.x + epsilon &&
               first.min.y < second.max.y - epsilon && first.max.y > second.min.y + epsilon;
    }

    private static T[] SceneComponents<T>() where T : Component
    {
        Scene active = SceneManager.GetActiveScene();
        return Resources.FindObjectsOfTypeAll<T>()
            .Where(component => component != null && !EditorUtility.IsPersistent(component) && component.gameObject.scene == active)
            .ToArray();
    }

    private static Transform[] SceneTransforms()
    {
        Scene active = SceneManager.GetActiveScene();
        return active.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
    }

    private static float SerializedFloat(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Require(property != null && property.propertyType == SerializedPropertyType.Float,
            $"{target.GetType().Name}.{propertyName} is missing from its serialized contract.");
        return property.floatValue;
    }

    private static int SerializedInt(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Require(property != null && property.propertyType == SerializedPropertyType.Integer,
            $"{target.GetType().Name}.{propertyName} is missing from its serialized contract.");
        return property.intValue;
    }

    private static Vector3 SerializedPosition(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Require(property != null &&
                (property.propertyType == SerializedPropertyType.Vector2 ||
                 property.propertyType == SerializedPropertyType.Vector3),
            $"{target.GetType().Name}.{propertyName} is missing from its serialized contract.");
        return property.propertyType == SerializedPropertyType.Vector2
            ? (Vector3)property.vector2Value
            : property.vector3Value;
    }

    private static float Range(IEnumerable<float> values)
    {
        float[] array = values.ToArray();
        Require(array.Length > 0, "Cannot measure an empty route.");
        return array.Max() - array.Min();
    }

    private static float SignedZ(float degrees) => degrees > 180f ? degrees - 360f : degrees;

    private static string NormalizePath(string path) => (path ?? string.Empty).Replace('\\', '/');

    private static void OpenScene(string path)
    {
        Require(File.Exists(ProjectFilePath(path)), $"Required scene is missing: {path}");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static string ProjectFilePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(projectRoot, NormalizePath(assetPath)));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"MINES VALIDATION FAILED: {message}");
        }
    }
}
