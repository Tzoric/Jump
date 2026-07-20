using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class MineLevelValidator
{
    private const string Overview = "Assets/Scenes/DungeonOverview.unity";
    private const string GameOver = "Assets/Scenes/GameOver.unity";
    private const string SilverOverview = "Assets/Scenes/SilverDungeonOverview.unity";
    private const string SilverLevel = "Assets/Scenes/SilverLevel1_SilverLode.unity";
    private const string PlatformArt = "Assets/Art/Generated/MineRockBronzePlatform.png";
    private const string MinerArt = "Assets/Art/Generated/MinerCharacterV2.png";
    private const string MinerAnimationArt = "Assets/Art/Generated/MinerAnimationSheet.png";
    private const string BackdropArt = "Assets/Art/Generated/MineLevel1BronzeBackdrop.png";
    private const string DiagonalBackdropArt = "Assets/Art/Generated/MineDiagonalBronzeBackdrop.png";
    private const string FarCaveBackdropArt = "Assets/Art/Generated/MineFarCaveTile.png";
    private const string MixedHorizontalBackdropArt = "Assets/Art/Generated/MineHorizontalTunnelTile.png";
    private const string MixedVerticalBackdropArt = "Assets/Art/Generated/MineVerticalShaftTile.png";
    private const string MixedDiagonalBackdropArt = "Assets/Art/Generated/MineDiagonalShaftTile.png";
    private const string MixedDescentBackdropArt = "Assets/Art/Generated/MineDescentShaftTile.png";
    private const string DoorArt = "Assets/Art/Generated/MineExitDoor.png";
    private const string ClosedChestArt = "Assets/Art/Generated/BronzeRewardChest.png";
    private const string OpenChestArt = "Assets/Art/Generated/BronzeRewardChestOpen.png";
    private const string SpikeArt = "Assets/Art/Generated/BronzeSpike.png";
    private const string SharedRockFillArt = "Assets/Art/Silver/SharedCaveRockTile.png";
    private const string DistantMineBackgroundArt = "Assets/Art/Silver/DistantMineBackground.png";
    private const string ForegroundRockPanelArt = "Assets/Art/Silver/ForegroundRockPanel9Slice.png";
    private const string PolishedSpikeArt = "Assets/Art/Silver/PolishedBronzeSpikes.png";
    private const string CutGemArt = "Assets/Art/Silver/TintableCutGem.png";
    private const string SilverChestClosedArt = "Assets/Art/Silver/SilverBoundChestClosed.png";
    private const string SilverChestOpenArt = "Assets/Art/Silver/SilverBoundChestOpen.png";
    private const string HangGliderArt = "Assets/Art/Silver/MinerHangGlider.png";
    private const string HangGliderFloatRightArt = "Assets/Art/Silver/HangGliderFloatRight.png";
    private const string HangGliderDiveRightArt = "Assets/Art/Silver/HangGliderDiveRight.png";
    private const string HangGliderBankRightArt = "Assets/Art/Silver/HangGliderBankRight.png";

    private enum ShaftDirection
    {
        Vertical,
        Angled,
        Horizontal,
        Descent,
        Mixed
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
        new(3, "Level3_ChasmRun", ShaftDirection.Descent, 7),
        new(4, "Level4_CopperColumn", ShaftDirection.Vertical, 16),
        new(5, "Level5_CrookedIncline", ShaftDirection.Angled, 10),
        new(6, "Level6_BrokenRail", ShaftDirection.Horizontal, 12),
        new(7, "Level7_FurnaceRise", ShaftDirection.Vertical, 21),
        new(8, "Level8_RazorAscent", ShaftDirection.Angled, 14),
        new(9, "Level9_AbyssRun", ShaftDirection.Horizontal, 16),
        new(10, "Level10_KeyVault", ShaftDirection.Vertical, 26),
        new(11, "Level11_TreasureVein", ShaftDirection.Angled, 18),
        new(12, "Level12_DeepworksGauntlet", ShaftDirection.Mixed, 60)
    };

    [MenuItem("Jump/Level Tools/Validate Bronze Mines Levels 1-12")]
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
        ValidateSilverDungeonWhenBuilt();
        ValidateBuildSettings();

        EditorSceneManager.OpenScene(Overview, OpenSceneMode.Single);
        Debug.Log("MINES VALIDATION PASSED: Bronze Mines Levels 1-12 and the available Silver dungeon content satisfy progression, seven-heart health, counted keys/chests, global hang-glider, shop, visual-theme, hazard, and route contracts.");
    }

    private static void ValidateEconomyRules()
    {
        Require(GameProgress.BaseHearts == 7, "Every level must begin with seven base hearts.");
        Require(GameProgress.StartingLives == 3, "A new game must begin with three lives.");
        Require(GameProgress.MaxMineLevel == 12, "The Bronze Mines progression contract must include Level 12.");
        Require(GameProgress.HealthPotionPrice == 3, "A health potion must cost three green gems.");
        Require(GameProgress.ExtraLifePrice == 25, "An extra life must cost 25 green gems.");
        Require(Mathf.Approximately(RewardChest.BlueGemChance, .50f), "The chest blue-gem chance must be 50%.");
        Require(Mathf.Approximately(RewardChest.PotionChance, .45f), "The chest health-potion chance must be 45%.");
        Require(Mathf.Approximately(RewardChest.ExtraLifeChance, .05f), "The chest extra-life chance must be 5%.");
        Require(Mathf.Approximately(
            RewardChest.BlueGemChance + RewardChest.PotionChance + RewardChest.ExtraLifeChance, 1f),
            "Chest reward chances must total 100%.");
        Require(RewardChest.OpenPrompt.Contains("UP") && RewardChest.OpenPrompt.Contains("W") &&
                RewardChest.OpenPrompt.Contains("OPEN CHEST"),
            "Chest instructions must show the current controller binding and fixed keyboard Up/W controls.");

        MineButtonAction[] actions = MineInput.GetBindableActions();
        Require(actions.Length == MineInput.BindableActionCount && actions.Distinct().Count() == 6,
            "The centralized input contract needs six distinct rebindable controller actions.");
        var expectedDefaults = new Dictionary<MineButtonAction, string>
        {
            { MineButtonAction.Run, "<Gamepad>/buttonSouth" },
            { MineButtonAction.Jump, "<Gamepad>/buttonEast" },
            { MineButtonAction.Interact, "<Gamepad>/buttonWest" },
            { MineButtonAction.Potion, "<Gamepad>/buttonNorth" },
            { MineButtonAction.Pause, "<Gamepad>/start" },
            { MineButtonAction.Home, "<Gamepad>/select" }
        };
        foreach (KeyValuePair<MineButtonAction, string> expected in expectedDefaults)
        {
            Require(string.Equals(MineInput.GetDefaultControllerBindingPath(expected.Key), expected.Value,
                    StringComparison.OrdinalIgnoreCase),
                $"Default {expected.Key} controller path must remain {expected.Value}.");
        }
        string interactActionName = MineInput.GetActionName(MineButtonAction.Interact);
        Require(MineInput.GetActionName(MineButtonAction.Jump) == "JUMP" &&
                interactActionName.StartsWith("INTERACT", StringComparison.Ordinal) &&
                (interactActionName.Contains("PARACHUTE") || interactActionName.Contains("GLIDER")),
            "The global hang glider must share controller X/Interact while Jump remains independent.");
        Require(ParachuteDescentControllerHasGlobalFlightContract(),
            "Hang-glider tuning must expose hover, neutral-glide, and faster-down modes independent of chute zones.");

        InputActionAsset mappingAsset = Resources.Load<InputActionAsset>("MineControllerActions");
        Require(mappingAsset != null, "The persistent controller mapping action asset is missing from Resources.");
        InputActionMap mappingMap = mappingAsset.FindActionMap("MineButtons", true);
        Require(mappingMap.actions.Count == MineInput.BindableActionCount &&
                mappingMap.bindings.All(binding => binding.id != Guid.Empty),
            "Controller mapping actions need six stable actions and persistent binding GUIDs.");
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
        MineShopController[] shops = SceneComponents<MineShopController>();
        Require(shops.Length == 1 && shops[0].LevelPanel != null && shops[0].ShopPanel != null &&
                shops[0].ControlsPanel != null,
            "Overview is missing its distinct Levels, Shop, or Controls page.");
        MineShopController shop = shops[0];
        Require(shop.LevelPanel != shop.ShopPanel && shop.LevelPanel != shop.ControlsPanel &&
                shop.ShopPanel != shop.ControlsPanel && shop.LevelPanel.activeSelf &&
                !shop.ShopPanel.activeSelf && !shop.ControlsPanel.activeSelf,
            "Overview must serialize with only the distinct Levels page visible.");
        Vector2Int[] expectedPlaytestSequence =
        {
            Vector2Int.up, Vector2Int.up, Vector2Int.down, Vector2Int.down,
            Vector2Int.left, Vector2Int.right, Vector2Int.left, Vector2Int.right,
            Vector2Int.down, Vector2Int.up
        };
        Require(MineShopController.PlaytestKeyboardCode == "MINER" &&
                MineShopController.PlaytestControllerSequenceLength == expectedPlaytestSequence.Length &&
                expectedPlaytestSequence.Select((step, index) =>
                    MineShopController.GetPlaytestControllerSequenceStep(index) == step).All(matches => matches) &&
                shop.BalanceDisplay != null,
            "Overview needs the hidden MINER / direction-sequence Foreman's Master Key and a visible active-status display.");

        MineControlsController[] mappings = SceneComponents<MineControlsController>();
        Require(mappings.Length == 1 && mappings[0].gameObject == shop.ControlsPanel,
            "Overview needs exactly one controller mapping controller on its Controls page.");
        MineControlsController mapping = mappings[0];
        UnityEngine.UI.Button[] mappingButtons =
        {
            mapping.RunButton, mapping.JumpButton, mapping.InteractButton,
            mapping.PotionButton, mapping.PauseButton, mapping.HomeButton
        };
        Require(mappingButtons.All(button => button != null) &&
                mappingButtons.Distinct().Count() == MineInput.BindableActionCount &&
                mapping.RestoreDefaultsButton != null && mapping.ControllerDisplay != null &&
                mapping.StatusDisplay != null && shop.ControlsDefaultSelection == mapping.RunButton.gameObject,
            "Controls page needs six distinct mapping rows, controller/status labels, Restore Defaults, and Run as its first selection.");
        Require(HasPersistentListener(mapping.RunButton, mapping, nameof(MineControlsController.RebindRun)) &&
                HasPersistentListener(mapping.JumpButton, mapping, nameof(MineControlsController.RebindJump)) &&
                HasPersistentListener(mapping.InteractButton, mapping, nameof(MineControlsController.RebindInteract)) &&
                HasPersistentListener(mapping.PotionButton, mapping, nameof(MineControlsController.RebindPotion)) &&
                HasPersistentListener(mapping.PauseButton, mapping, nameof(MineControlsController.RebindPause)) &&
                HasPersistentListener(mapping.HomeButton, mapping, nameof(MineControlsController.RebindHome)) &&
                HasPersistentListener(mapping.RestoreDefaultsButton, mapping, nameof(MineControlsController.RestoreDefaults)),
            "Every mapping row and Restore Defaults button must have its persistent listener.");

        string controlsCopy = string.Join(" ", shop.ControlsPanel
            .GetComponentsInChildren<TextMeshProUGUI>(true).Select(label => label.text)).ToUpperInvariant();
        Require(controlsCopy.Contains("BUTTON MAPPING") && controlsCopy.Contains("STICK / D-PAD") &&
                controlsCopy.Contains("KEYBOARD") && controlsCopy.Contains("UI NAVIGATION") &&
                controlsCopy.Contains("X MODE") && controlsCopy.Contains("EACH CONTROLLER MODEL"),
            "Controls page must explain mapping, fixed movement/keyboard/UI controls, F310 X mode, and per-model saves.");

        UnityEngine.UI.Button[] overviewButtons = SceneComponents<UnityEngine.UI.Button>();
        Require(overviewButtons.Any(button => button.name == "Levels Tab" &&
                    HasPersistentListener(button, shop, nameof(MineShopController.ShowLevels))) &&
                overviewButtons.Any(button => button.name == "Shop Tab" &&
                    HasPersistentListener(button, shop, nameof(MineShopController.ShowShop))) &&
                overviewButtons.Any(button => button.name == "Controls Tab" &&
                    HasPersistentListener(button, shop, nameof(MineShopController.ShowControls))),
            "Overview needs wired Levels, Shop, and Controls tabs.");
        ValidateInputSystemEventSystem("Overview", true);

        MineLevelSelectButton[] nodes = SceneComponents<MineLevelSelectButton>();
        Require(nodes.Length == Levels.Length, "Overview must contain exactly 12 playable mineshaft nodes.");
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
        ValidateInputSystemEventSystem("Game Over", true);
    }

    private static float ValidateLevel(LevelExpectation level)
    {
        OpenScene(level.Path);
        ValidateSceneCamera(level);
        Require(SceneComponents<MineShopController>().Length == 0,
            $"{level.SceneName} must not listen for the overview-only playtest easter egg.");

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
        ValidateLevelMenu(level);

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

        ValidateEntranceDoor(level, hero);
        ValidateDoor(level);
        ValidateKeysAndChest(level);
        ValidateSpikes(level, hero, waypoints);
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
        float runSpeed = SerializedFloat(hero, "runSpeed");
        float jumpForce = SerializedFloat(hero, "jumpForce");
        float powerJumpForce = SerializedFloat(hero, "powerJumpForce");
        float jumpTime = SerializedFloat(hero, "jumpTime");
        float powerJumpTime = SerializedFloat(hero, "powerJumpTime");
        float jumpAnticipation = SerializedFloat(hero, "jumpAnticipationSeconds");
        Require(Mathf.Abs(speed - 7.5f) <= .05f,
            $"{level.SceneName} hero speed must be the slowed 7.5 target (75% of the original speed)." );
        Require(jumpForce >= 11.5f && jumpForce <= 13f,
            $"{level.SceneName} jump should use the slightly higher, movement-matched 11.5-13 range.");
        Require(Mathf.Abs(runSpeed - 9f) <= .05f && runSpeed > speed,
            $"{level.SceneName} A/Shift run speed must be the 9.0 power-run target.");
        Require(Mathf.Abs(powerJumpForce - 14.75f) <= .05f && powerJumpForce > jumpForce &&
                Mathf.Abs(jumpTime - .24f) <= .01f && Mathf.Abs(powerJumpTime - .26f) <= .01f,
            $"{level.SceneName} power jump must use force 14.75 and a .26-second hold window " +
            "while preserving the ordinary 12/.24 jump.");
        Require(jumpAnticipation >= .06f && jumpAnticipation <= .10f,
            $"{level.SceneName} jump needs a visible 60-100ms grounded squat before takeoff.");
        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        Require(body != null && body.gravityScale >= 5f && body.gravityScale <= 5.8f,
            $"{level.SceneName} hero gravity does not match the slower movement tuning.");
        Collider2D bodyCollider = hero.GetComponentsInChildren<Collider2D>(true)
            .FirstOrDefault(collider => !collider.isTrigger);
        Require(bodyCollider != null && bodyCollider.sharedMaterial != null &&
                bodyCollider.sharedMaterial.friction <= .01f && bodyCollider.sharedMaterial.bounciness <= .01f,
            $"{level.SceneName} hero needs a zero-friction, zero-bounce collider material so wall contact cannot suspend the miner.");
        Require(hero.transform.lossyScale.x >= 1.75f && hero.transform.lossyScale.x <= 2.1f,
            $"{level.SceneName} miner should remain approximately 125% of the original hero size.");
        Require(SerializedInt(health, "maxHealth") == GameProgress.BaseHearts,
            $"{level.SceneName} serialized base health must be seven hearts.");

        SpriteRenderer[] renderers = hero.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer miner = renderers.FirstOrDefault(renderer =>
            renderer.sprite != null && NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(MinerArt));
        Require(miner != null && miner.enabled, $"{level.SceneName} must use the completely remade miner sprite.");
        Require(miner != null && Mathf.Approximately(miner.color.a, 1f),
            $"{level.SceneName} miner body must start fully opaque; damage flashing restores this authored alpha.");
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
        SerializedProperty handPickaxe = new SerializedObject(outfit).FindProperty("handPickaxe");
        Transform handRig = handPickaxe?.objectReferenceValue as Transform;
        Require(definition.HandTool == null && handRig == null && outfit.HandPickaxe == null,
            $"{level.SceneName} Bronze Miner must not carry the removed pickaxe.");
        Require(!hero.GetComponentsInChildren<Transform>(true).Any(transform =>
                transform.name.IndexOf("pick", StringComparison.OrdinalIgnoreCase) >= 0),
            $"{level.SceneName} still contains a pick or pick-hand rig after pickaxe removal.");
        ParachuteDescentController parachute = hero.GetComponent<ParachuteDescentController>();
        HangGliderVisualController gliderVisual = hero.GetComponent<HangGliderVisualController>();
        SpriteRenderer parachuteRenderer = renderers.FirstOrDefault(renderer =>
            renderer.name.IndexOf("parachute", StringComparison.OrdinalIgnoreCase) >= 0 ||
            renderer.name.IndexOf("glider", StringComparison.OrdinalIgnoreCase) >= 0);
        Require(parachute != null && gliderVisual != null && gliderVisual.HasCompleteDirectionalArt &&
                gliderVisual.CurrentState == HangGliderVisualState.Stowed &&
                gliderVisual.GliderRenderer == parachuteRenderer &&
                parachuteRenderer != null && !parachuteRenderer.enabled &&
                parachuteRenderer.sprite != null && parachuteRenderer.transform.localScale.y > 0f &&
                parachuteRenderer.transform.localPosition.y > 0f &&
                parachuteRenderer.sprite.bounds.size.x > parachuteRenderer.sprite.bounds.size.y * 1.5f,
            $"{level.SceneName} hero needs hidden hover/float/dive/left/right hang-glider presentation.");
        Require(NormalizePath(AssetDatabase.GetAssetPath(gliderVisual.HoverFrontSprite)) ==
                    NormalizePath(HangGliderArt) &&
                NormalizePath(AssetDatabase.GetAssetPath(gliderVisual.FloatRightSprite)) ==
                    NormalizePath(HangGliderFloatRightArt) &&
                NormalizePath(AssetDatabase.GetAssetPath(gliderVisual.DiveRightSprite)) ==
                    NormalizePath(HangGliderDiveRightArt) &&
                NormalizePath(AssetDatabase.GetAssetPath(gliderVisual.BankRightSprite)) ==
                    NormalizePath(HangGliderBankRightArt),
            $"{level.SceneName} glider states must use the authored front hover and side-profile directional art.");
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
        Require(doorTrigger != null && doorTrigger.enabled && doorTrigger.isTrigger,
            $"{level.SceneName} exit door needs an enabled proximity trigger for Up/W interaction.");
        Require(LevelExitDoor.ExitPrompt.Contains("UP") && LevelExitDoor.ExitPrompt.Contains("W") &&
                LevelExitDoor.ExitPrompt.Contains("EXIT"),
            $"{level.SceneName} exit prompt must identify the current controller binding and keyboard Up/W.");
        Require(door.DoorAnimator != null || door.GetComponent<MineDoorAnimator>() != null,
            $"{level.SceneName} exit controller must wait for its door animator before traversal.");
        ValidateDoorAnimation(door.gameObject, $"{level.SceneName} exit");
        MineDoorAnimator doorAnimator = door.DoorAnimator ?? door.GetComponent<MineDoorAnimator>();
        Require(doorAnimator != null && doorAnimator.PassageBlocker != null &&
                doorAnimator.PassageBlocker.enabled && !doorAnimator.PassageBlocker.isTrigger,
            $"{level.SceneName} exit door must physically block the passage until it opens.");

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

    private static void ValidateEntranceDoor(LevelExpectation level, HeroMovement hero)
    {
        LevelEntranceDoor[] entrances = SceneComponents<LevelEntranceDoor>();
        Require(entrances.Length == 1,
            $"{level.SceneName} must contain exactly one supported entrance door.");
        LevelEntranceDoor entrance = entrances[0];
        Require(entrance.Hero == hero && Vector2.Distance(entrance.GameplayPosition,
                hero.transform.position) <= .05f && entrance.EntranceSeconds >= .5f,
            $"{level.SceneName} entrance door must walk the scene hero to the authored start position.");
        Require(entrance.DoorAnimator != null || entrance.GetComponent<MineDoorAnimator>() != null,
            $"{level.SceneName} entrance controller must wait for its door animator before traversal.");
        MineDoorAnimator entranceAnimator = entrance.GetComponentInChildren<MineDoorAnimator>(true);
        SpriteRenderer renderer = entranceAnimator == null
            ? entrance.GetComponent<SpriteRenderer>()
            : entranceAnimator.ClosedRenderer;
        Require(renderer != null && renderer.sprite != null && renderer.sortingOrder == 5 &&
                (NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(DoorArt) ||
                 renderer.GetComponentInParent<ThemedMetalFlakes>() != null ||
                 entranceAnimator != null),
            $"{level.SceneName} entrance must use the themed mine-door art behind the emerging miner.");
        Require(Mathf.Abs(entrance.transform.position.x - entrance.GameplayPosition.x) <= .1f &&
                entrance.transform.position.y > entrance.GameplayPosition.y,
            $"{level.SceneName} entrance door is not aligned behind the hero's start position.");

        Require(entrance.GetComponentsInChildren<Collider2D>(true).Length == 0 &&
                entrance.GetComponent<LevelExitDoor>() == null,
            $"{level.SceneName} entrance door must be noninteractive and have no collision trigger.");
        ValidateDoorAnimation(entrance.gameObject, $"{level.SceneName} entrance");

        Transform[] foundations = SceneTransforms().Where(transform =>
            transform.name == "Entrance Door Foundation (Required)").ToArray();
        Require(foundations.Length == 1,
            $"{level.SceneName} must reuse exactly one start platform as its entrance foundation.");
        Collider2D foundation = foundations[0].GetComponent<Collider2D>();
        SpriteRenderer foundationRenderer = foundations[0].GetComponent<SpriteRenderer>();
        if (foundationRenderer == null)
            foundationRenderer = foundations[0].GetComponentInChildren<SpriteRenderer>(true);
        float arrivalClearance = foundation == null
            ? float.MaxValue
            : entrance.GameplayPosition.y - foundation.bounds.max.y;
        bool hasThemedFoundation = foundations[0].GetComponentInChildren<ThemedMetalFlakes>(true) != null;
        string foundationSpritePath = foundationRenderer == null || foundationRenderer.sprite == null
            ? "<none>"
            : NormalizePath(AssetDatabase.GetAssetPath(foundationRenderer.sprite));
        Require(foundation != null && foundation.enabled && !foundation.isTrigger &&
                foundation.gameObject.layer == LayerMask.NameToLayer("Ground") &&
                foundation.CompareTag("Ground") && foundationRenderer != null && foundationRenderer.sprite != null &&
                (foundationSpritePath == NormalizePath(PlatformArt) || hasThemedFoundation) &&
                foundation.bounds.min.x < entrance.transform.position.x &&
                foundation.bounds.max.x > entrance.transform.position.x &&
                arrivalClearance >= .7f && arrivalClearance <= 1.7f,
            $"{level.SceneName} entrance foundation must be the solid themed start platform directly beneath arrival. " +
            $"collider={foundation != null}, enabled={foundation != null && foundation.enabled}, " +
            $"trigger={foundation != null && foundation.isTrigger}, layer={foundation?.gameObject.layer}, " +
            $"tag={(foundation == null ? "<none>" : foundation.tag)}, renderer={foundationRenderer != null}, " +
            $"sprite={foundationSpritePath}, flakes={hasThemedFoundation}, clearance={arrivalClearance:0.00}.");
    }

    private static void ValidateLevelMenu(LevelExpectation level)
    {
        MineLevelMenuController[] menus = SceneComponents<MineLevelMenuController>();
        Require(menus.Length == 1, $"{level.SceneName} needs exactly one Start-pause/Back-home controller.");
        MineLevelMenuController menu = menus[0];
        Require(menu.HomeScene == MineLevelMenuController.DefaultHomeScene && menu.PausePanel != null &&
                !menu.PausePanel.activeSelf,
            $"{level.SceneName} pause controller must target DungeonOverview and start with its panel hidden.");

        TextMeshProUGUI[] labels = menu.PausePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        string combinedLabels = string.Join(" ", labels.Select(label => label.text));
        Require(combinedLabels.Contains("PAUSED") && combinedLabels.Contains("SHOP"),
            $"{level.SceneName} pause panel must explain resume and return-to-shop behavior.");

        MineControlHintDisplay[] controlHints = SceneComponents<MineControlHintDisplay>();
        Require(controlHints.Length == 1 && controlHints[0].LevelInstructions != null &&
                controlHints[0].PauseInstructions != null &&
                controlHints[0].PauseInstructions.transform.IsChildOf(menu.PausePanel.transform),
            $"{level.SceneName} needs one dynamic HUD/pause binding display so remapped buttons stay accurate.");

        UnityEngine.UI.Button[] buttons = menu.PausePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        Require(buttons.Any(button => HasPersistentListener(button, menu, nameof(MineLevelMenuController.ResumeGame))) &&
                buttons.Any(button => HasPersistentListener(button, menu, nameof(MineLevelMenuController.ReturnToOverview))),
            $"{level.SceneName} pause panel needs wired Resume and Return to Overview/Shop buttons.");
        ValidateMidLevelShop(level.SceneName, menu);
        ValidateInputSystemEventSystem(level.SceneName, false);
    }

    private static void ValidateKeysAndChest(LevelExpectation level)
    {
        BronzeKeyCollectible[] bronzeKeys = SceneComponents<BronzeKeyCollectible>();
        RewardChest[] chests = SceneComponents<RewardChest>();
        Require(bronzeKeys.Length == 1 && bronzeKeys[0].LevelNumber == level.Number,
            $"{level.SceneName} needs exactly one same-level bronze key.");
        Require(chests.Length == 1 && chests[0].LevelNumber == level.Number,
            $"{level.SceneName} needs exactly one same-level one-time reward chest.");

        RewardChest chest = chests[0];
        Collider2D chestTrigger = chest.GetComponent<Collider2D>();
        SpriteRenderer chestRenderer = chest.GetComponent<SpriteRenderer>();
        Require(chestTrigger != null && chestTrigger.enabled && chestTrigger.isTrigger,
            $"{level.SceneName} chest needs an enabled proximity trigger for Up/W interaction and replay feedback.");
        string closedChestPath = chestRenderer == null || chestRenderer.sprite == null
            ? string.Empty
            : NormalizePath(AssetDatabase.GetAssetPath(chestRenderer.sprite));
        Require(chestRenderer != null && chestRenderer.sprite != null &&
                (closedChestPath == NormalizePath(ClosedChestArt) ||
                 closedChestPath == NormalizePath(SilverChestClosedArt)),
            $"{level.SceneName} chest needs the authored metal-bound closed-chest sprite.");
        Require(chest.OpenedSprite != null && chest.OpenedSprite != chestRenderer.sprite &&
                (NormalizePath(AssetDatabase.GetAssetPath(chest.OpenedSprite)) == NormalizePath(OpenChestArt) ||
                 NormalizePath(AssetDatabase.GetAssetPath(chest.OpenedSprite)) == NormalizePath(SilverChestOpenArt)),
            $"{level.SceneName} chest needs a distinct configured opened metal-bound sprite for replayed levels.");

        Collider2D chestPerch = chest.transform.parent == null ? null :
            chest.transform.parent.GetComponentsInChildren<Collider2D>(true).FirstOrDefault(collider =>
                !collider.isTrigger && collider.name == "Reward Chest Perch");
        Require(chestPerch != null && chestPerch.bounds.max.y < chest.transform.position.y &&
                chestPerch.bounds.min.x < chest.transform.position.x &&
                chestPerch.bounds.max.x > chest.transform.position.x,
            $"{level.SceneName} reward chest must remain visibly supported by its dedicated perch.");

        MineRunInventory[] inventories = SceneComponents<MineRunInventory>();
        Require(inventories.Length == 1 && inventories[0].LevelNumber == level.Number &&
                inventories[0].HasStatusDisplay,
            $"{level.SceneName} hero inventory needs a same-level HUD display for chest prompts.");

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

    private static void ValidateSpikes(LevelExpectation level, HeroMovement hero,
        AutomatedPlaytestWaypoint[] waypoints)
    {
        Physics2D.SyncTransforms();
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

            PolygonCollider2D polygon = spike.GetComponent<PolygonCollider2D>();
            Require(polygon != null && polygon.enabled && polygon.isTrigger &&
                    spike.GetComponents<Collider2D>().Length == 1 && spike.GetComponent<BoxCollider2D>() == null,
                $"{level.SceneName} spike '{spike.name}' must use one enabled polygon trigger, not an air-filling box.");
            Require(polygon.pathCount == SpikeHitboxGeometry.PathCount,
                $"{level.SceneName} spike '{spike.name}' needs one separate path for each of its three visible teeth.");
            for (int pathIndex = 0; pathIndex < SpikeHitboxGeometry.PathCount; pathIndex++)
            {
                Vector2[] points = polygon.GetPath(pathIndex);
                Require(points.Length == SpikeHitboxGeometry.PointsPerPath,
                    $"{level.SceneName} spike '{spike.name}' path {pathIndex} must be triangular.");
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    Require(Vector2.Distance(points[pointIndex],
                                SpikeHitboxGeometry.ExpectedPoint(pathIndex, pointIndex)) <= .002f,
                        $"{level.SceneName} spike '{spike.name}' collider exceeds the inset visible-tooth geometry.");
                }
            }

            SpriteRenderer renderer = spike.GetComponent<SpriteRenderer>();
            string spikePath = renderer == null || renderer.sprite == null
                ? string.Empty
                : NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite));
            bool usesLegacySpike = spikePath == NormalizePath(SpikeArt) &&
                Mathf.Abs(renderer.sprite.pixelsPerUnit - 24f) <= .01f &&
                renderer.sprite.rect.size == new Vector2(40f,24f) &&
                Vector2.Distance(renderer.sprite.pivot,new Vector2(20f,12f)) <= .01f;
            bool usesPolishedSpike = spikePath == NormalizePath(PolishedSpikeArt) &&
                spike.GetComponent<SpriteShineAnimator>() != null;
            Require(renderer != null && renderer.sprite != null && (usesLegacySpike || usesPolishedSpike),
                $"{level.SceneName} spike '{spike.name}' must use either the legacy inset art or the polished half-scale shining bronze asset.");

            float[] toothCenters = { -.5208333f, .0208333f, .5625f };
            foreach (float center in toothCenters)
            {
                Require(polygon.OverlapPoint(spike.transform.TransformPoint(new Vector2(center,0f))),
                    $"{level.SceneName} spike '{spike.name}' collider misses a visible tooth center.");
            }
            float[] transparentValleys = { -.25f, .2916667f };
            foreach (float valley in transparentValleys)
            {
                Require(!polygon.OverlapPoint(spike.transform.TransformPoint(new Vector2(valley,0f))),
                    $"{level.SceneName} spike '{spike.name}' still damages transparent air between teeth.");
            }
        }

        if (level.Number == 10)
        {
            AutomatedPlaytestWaypoint[] landings = waypoints
                .Where(waypoint => waypoint.Mode == AutomatedWaypointMode.GroundedLanding).ToArray();
            Require(landings.Length == level.MinimumWaypoints && landings.All(waypoint => waypoint.UsePowerJump),
                "Level 10 must mark every required shaft jump for the A+B/Shift+Space power-run mechanic.");

            Collider2D heroCollider = hero.GetComponentsInChildren<Collider2D>(true)
                .FirstOrDefault(collider => !collider.isTrigger);
            Require(heroCollider != null, "Level 10 hero needs a solid collider for spike-landing clearance checks.");
            foreach (DamageZone spike in spikes)
            {
                Collider2D spikeCollider = spike.GetComponent<Collider2D>();
                if (spikeCollider == null || landings.Length == 0) continue;
                AutomatedPlaytestWaypoint nearest = landings.OrderBy(waypoint =>
                    Mathf.Abs(waypoint.transform.position.y - spike.transform.position.y) +
                    Mathf.Abs(waypoint.transform.position.x - spike.transform.position.x) * .1f).First();
                if (Mathf.Abs(nearest.transform.position.y - spike.transform.position.y) > .9f) continue;

                float horizontalClearance = Mathf.Abs(nearest.transform.position.x - spikeCollider.bounds.center.x) -
                    heroCollider.bounds.extents.x - spikeCollider.bounds.extents.x;
                Require(horizontalClearance >= .25f,
                    $"Level 10 landing waypoint {nearest.Order} leaves only {horizontalClearance:0.00} " +
                    $"clearance from spike '{spike.name}'; at least 0.25 is required.");
            }
        }
        else
        {
            Require(waypoints.All(waypoint => !waypoint.UsePowerJump),
                $"{level.SceneName} must not require the Level 10 power-jump route flag.");
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
            if (renderer == null)
                renderer = platform.GetComponentInChildren<SpriteRenderer>(true);
            bool legacyBronze = renderer != null && renderer.sprite != null &&
                NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(PlatformArt);
            ThemedMetalFlakes themedFlakes = platform.GetComponentInChildren<ThemedMetalFlakes>(true);
            bool themedBronze = renderer != null && renderer.sprite != null && themedFlakes != null &&
                themedFlakes.Theme != null &&
                themedFlakes.Theme.DungeonId == GameProgress.BronzeDungeonId;
            Require(legacyBronze || themedBronze,
                $"{level.SceneName} platform '{platform.name}' is not using legacy or centrally themed bronze-veined rock artwork.");
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
            (platform.name.IndexOf("upper", StringComparison.OrdinalIgnoreCase) >= 0 ||
             platform.name == "Entrance Door Foundation (Required)"))
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
        Require(backdrop != null && backdrop.sprite != null &&
                NormalizePath(AssetDatabase.GetAssetPath(backdrop.sprite)) == NormalizePath(DiagonalBackdropArt) &&
                Mathf.Abs(SignedZ(backdrop.transform.eulerAngles.z)) <= 1f,
            "Level 2 must use the dedicated, natively diagonal mine backdrop without rotating vertical artwork.");
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
                SpriteRenderer backdrop = SceneComponents<SpriteRenderer>().FirstOrDefault(renderer =>
                    renderer.name.IndexOf("backdrop", StringComparison.OrdinalIgnoreCase) >= 0);
                Require(backdrop != null && backdrop.sprite != null &&
                        NormalizePath(AssetDatabase.GetAssetPath(backdrop.sprite)) == NormalizePath(DiagonalBackdropArt) &&
                        Mathf.Abs(SignedZ(backdrop.transform.eulerAngles.z)) <= 1f,
                    $"{level.SceneName} must use unrotated artwork composed specifically for a diagonal mine.");
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
            case ShaftDirection.Descent:
            {
                Require(level.Number == 3 && SceneComponents<BoundedCameraFollow>().Length == 1,
                    "Level 3 parachute descent needs one bounded follow camera.");
                ParachuteDescentZone[] zones = SceneComponents<ParachuteDescentZone>();
                ParachuteLaunchZone[] launches = SceneComponents<ParachuteLaunchZone>();
                Require(zones.Length == 1 && zones[0].MinimumDepth >= 24f &&
                        launches.Length == 1 && launches[0].Sign == null &&
                        launches[0].GetComponent<BoxCollider2D>() is { isTrigger: true },
                    "Level 3 needs one deep parachute shaft with a silent launch area.");
                Require(SceneComponents<ParachuteInstructionDisplay>().Length == 0,
                    "Level 3 must not display parachute instruction text.");
                Collider2D descent = zones[0].GetComponent<Collider2D>();
                int descentGems = SceneComponents<GreenCrystalCollectible>().Count(gem =>
                    descent != null && descent.bounds.Contains(gem.transform.position));
                Require(descentGems >= 4,
                    "Level 3 parachute shaft needs a visible gem trail through its safe descent lanes.");
                Require(SceneTransforms().Any(transform => transform.name == "Parachute Landing Shelf 01") &&
                        SceneTransforms().Count(transform =>
                            transform.name.StartsWith("Parachute Landing Exit Step", StringComparison.OrdinalIgnoreCase)) == 2,
                    "Level 3 needs a safe landing and two-step exit tunnel after the descent.");
                return zones[0].MinimumDepth;
            }
            case ShaftDirection.Mixed:
                return ValidateLevel12MixedRoute(level);
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

        FatalFallZone[] fatalZones = SceneComponents<FatalFallZone>().Where(zone =>
            zone.name.IndexOf("bottomless", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(fatalZones.Length > 0, $"{level.SceneName} needs lethal bottomless-pit coverage.");
        Collider2D[] fatalTriggers = fatalZones.Select(zone => zone.GetComponent<Collider2D>()).ToArray();
        Require(fatalTriggers.All(trigger => trigger != null && trigger.isTrigger),
            $"{level.SceneName} every FatalFallZone needs a trigger collider.");

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

    private static float ValidateLevel12MixedRoute(LevelExpectation level)
    {
        Require(level.Number == 12, "Mixed-route validation is reserved for Level 12.");
        MixedRouteCameraFollow[] cameraFollowers = SceneComponents<MixedRouteCameraFollow>();
        Require(cameraFollowers.Length == 1,
            "Level 12 needs one descent-aware mixed-route camera with stable parachute framing.");
        MixedRouteCameraFollow cameraFollow = cameraFollowers[0];
        HeroMovement levelHero = SceneComponents<HeroMovement>().Single();
        Camera gameplayCamera = cameraFollow.GetComponent<Camera>();
        Vector2 expectedCameraStart = (Vector2)levelHero.transform.position + new Vector2(
            0f, SerializedFloat(cameraFollow, "upwardLookAhead"));
        Require(gameplayCamera != null &&
                Vector2.Distance(gameplayCamera.transform.position, expectedCameraStart) <= .15f,
            "Level 12 camera must begin at the miner instead of the final route endpoint.");
        Require(gameplayCamera.orthographicSize >= 6.2f && cameraFollow.VerticalDeadZone >= 1f &&
                cameraFollow.DownwardLookAhead <= 2.25f &&
                SerializedFloat(cameraFollow, "smoothTime") >= .18f &&
                SerializedFloat(cameraFollow, "lookAheadSmoothTime") >= .25f &&
                SerializedFloat(cameraFollow, "descentBlendTime") >= .24f,
            "Level 12 camera needs zoomed-out framing plus smoothed route, facing, and descent transitions.");

        ParachuteInstructionDisplay[] chutePrompts = SceneComponents<ParachuteInstructionDisplay>();
        Require(chutePrompts.Length == 0,
            "Level 12 must not display parachute instruction text.");

        MineRouteSection[] sections = SceneComponents<MineRouteSection>()
            .OrderBy(section => section.Order).ToArray();
        Require(sections.Length == 12, "Level 12 must contain exactly twelve ordered mixed-route sections.");
        for (int index = 0; index < sections.Length; index++)
        {
            Require(sections[index].Order == index + 1,
                $"Level 12 section order is broken at section {index + 1}.");
            if (index > 0)
                Require(Vector2.Distance(sections[index - 1].Exit, sections[index].Entry) <= .1f,
                    $"Level 12 sections {index} and {index + 1} do not connect continuously.");
        }

        foreach (MineRouteSectionType type in Enum.GetValues(typeof(MineRouteSectionType)))
            Require(sections.Count(section => section.SectionType == type) == 3,
                $"Level 12 needs exactly three {type} sections in its seeded mixed order.");

        float pathLength = sections.Sum(section => section.PathLength);
        Require(pathLength >= 300f,
            $"Level 12 must remain a very long mixed gauntlet; authored path is only {pathLength:0.0} units.");

        MineRouteSection[] descents = sections.Where(section =>
            section.SectionType == MineRouteSectionType.VerticalDown).ToArray();
        Require(descents.All(section => section.Entry.y - section.Exit.y >= 24f),
            "Every Level 12 downward section must descend at least 24 world units.");

        ParachuteDescentZone[] zones = SceneComponents<ParachuteDescentZone>();
        Require(zones.Length == 3 && zones.All(zone => zone.MinimumDepth >= 24f),
            "Level 12 must have one deep parachute zone for each downward section.");
        ParachuteLaunchZone[] launchZones = SceneComponents<ParachuteLaunchZone>();
        Require(launchZones.Length == 3 && launchZones.All(launch =>
                launch.GetComponent<BoxCollider2D>() is { isTrigger: true } && launch.Sign == null),
            "Level 12 must have one silent, non-solid parachute launch area before each shaft.");

        Physics2D.SyncTransforms();
        foreach (MineRouteSection descent in descents)
        {
            ParachuteDescentZone zone = descent.GetComponentInChildren<ParachuteDescentZone>(true);
            BoxCollider2D trigger = zone == null ? null : zone.GetComponent<BoxCollider2D>();
            ParachuteLaunchZone launch = descent.GetComponentInChildren<ParachuteLaunchZone>(true);
            BoxCollider2D launchTrigger = launch == null ? null : launch.GetComponent<BoxCollider2D>();
            BoxCollider2D launchShelf = descent.GetComponentsInChildren<BoxCollider2D>(true).Single(collider =>
                collider.name.IndexOf("Parachute Launch Shelf", StringComparison.OrdinalIgnoreCase) >= 0);
            BoxCollider2D landing = descent.GetComponentsInChildren<BoxCollider2D>(true).Single(collider =>
                collider.name.IndexOf("Parachute Landing Shelf", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(launchTrigger != null && launchTrigger.bounds.Intersects(launchShelf.bounds) &&
                    launchTrigger.bounds.max.x > descent.Entry.x + 1f,
                $"Level 12 descent {descent.Order} launch prompt must cover its shelf and marked right lip.");
            Require(trigger != null && trigger.bounds.max.y <= launchShelf.bounds.min.y - .25f,
                $"Level 12 descent {descent.Order} chute trigger starts while the miner is still standing on the shelf.");
            Require(trigger != null && trigger.bounds.min.y >= landing.bounds.max.y + .75f,
                $"Level 12 descent {descent.Order} chute trigger reaches through its landing platform.");

            int routeIndex = Array.IndexOf(sections, descent);
            if (routeIndex >= sections.Length - 1) continue;
            MineRouteSection following = sections[routeIndex + 1];
            BoxCollider2D[] outgoing = following.GetComponentsInChildren<BoxCollider2D>(true)
                .Where(collider => !collider.isTrigger &&
                    collider.name.IndexOf("Mixed", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(collider => Vector2.Distance(collider.bounds.center, following.Entry))
                .ToArray();
            Require(outgoing.Length >= 2,
                $"Level 12 descent {descent.Order} needs two readable outgoing ledges.");
            bool flatTransitionShelf = Mathf.Abs(SignedZ(outgoing[0].transform.eulerAngles.z)) <= 1f;
            float allowedLandingOverlap = flatTransitionShelf ? .6f : .15f;
            float allowedBreakoutGap = flatTransitionShelf ? 2f : 1f;
            Require(outgoing[0].bounds.min.x >= landing.bounds.max.x - allowedLandingOverlap &&
                    outgoing[0].bounds.min.x <= landing.bounds.max.x + allowedBreakoutGap,
                $"Level 12 descent {descent.Order} first outgoing ledge must clear the miner's head " +
                "without creating an excessive breakout gap.");

            BoxCollider2D rightWall = descent.GetComponentsInChildren<BoxCollider2D>(true).Single(collider =>
                collider.name.IndexOf("Descent Right Rock Wall", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(rightWall.bounds.min.y >=
                    Mathf.Max(outgoing[0].bounds.max.y, outgoing[1].bounds.max.y) + 3.5f,
                $"Level 12 descent {descent.Order} right wall blocks the first two outgoing ledges.");
            DamageZone finalFixedHazard = descent.GetComponentsInChildren<DamageZone>(true).Single(zoneDamage =>
                zoneDamage.name.EndsWith("-04", StringComparison.OrdinalIgnoreCase));
            Require(finalFixedHazard.transform.position.x < descent.Entry.x,
                $"Level 12 descent {descent.Order} final fixed spike blocks its lower-right exit.");
        }

        ProximityHiddenHazard[] hidden = SceneComponents<ProximityHiddenHazard>();
        Require(hidden.Length >= 12 && hidden.All(hazard =>
                hazard.RevealDistance >= 9f && hazard.WarningSeconds >= .5f &&
                SerializedInt(hazard.GetComponent<DamageZone>(), "damage") == 1),
            "Level 12 hidden descent spikes need long reveal distance, a readable warning, and one-heart damage.");

        OscillatingHazard[] moving = SceneComponents<OscillatingHazard>();
        Require(moving.Length >= 6 && moving.All(hazard =>
                hazard.TravelDistance >= 3.5f &&
                SerializedInt(hazard.GetComponent<DamageZone>(), "damage") == 1),
            "Level 12 needs visible moving one-heart hazards throughout all descents.");

        AutomatedPlaytestWaypoint[] airborne = SceneComponents<AutomatedPlaytestWaypoint>().Where(waypoint =>
            waypoint.Mode == AutomatedWaypointMode.AirbornePass).ToArray();
        Require(airborne.Length >= 12 && airborne.All(waypoint => waypoint.DeployParachute),
            "Level 12 descents need parachute-enabled airborne safe-lane waypoints around every major hazard.");
        GreenCrystalCollectible[] routeGems = SceneComponents<GreenCrystalCollectible>();
        Require(zones.All(zone =>
                routeGems.Count(gem => zone.GetComponent<Collider2D>().bounds.Contains(gem.transform.position)) >= 4),
            "Every Level 12 parachute shaft needs a four-gem trail through its safe descent lanes.");

        FatalFallZone[] localPits = SceneComponents<FatalFallZone>().Where(zone =>
            zone.gameObject.activeInHierarchy &&
            zone.name.IndexOf("Level 12 Local Bottomless Pit", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        int horizontalToDescentTransitions = 0;
        int verticalToHorizontalTransitions = 0;
        for (int index = 0; index + 1 < sections.Length; index++)
        {
            if (sections[index].SectionType == MineRouteSectionType.Horizontal &&
                sections[index + 1].SectionType == MineRouteSectionType.VerticalDown)
                horizontalToDescentTransitions++;
            if (sections[index].SectionType == MineRouteSectionType.VerticalUp &&
                sections[index + 1].SectionType == MineRouteSectionType.Horizontal)
            {
                verticalToHorizontalTransitions++;
                BoxCollider2D transitionHub = sections[index + 1]
                    .GetComponentsInChildren<BoxCollider2D>(true)
                    .Single(collider => collider.name.IndexOf("Mixed Horizontal Hub",
                        StringComparison.OrdinalIgnoreCase) >= 0);
                BoxCollider2D firstLanding = sections[index + 1]
                    .GetComponentsInChildren<BoxCollider2D>(true)
                    .Where(collider => collider.name.IndexOf("Mixed Pit Ledge",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(collider => collider.bounds.center.x)
                    .First();
                float transitionGap = firstLanding.bounds.min.x - transitionHub.bounds.max.x;
                Require(transitionGap >= 1.25f && transitionGap <= 3f,
                    $"Level 12 climb {sections[index].Order} needs a full body-width, reachable opening " +
                    $"to its horizontal route; gap is {transitionGap:0.00} units.");
            }
        }
        int expectedLocalPits = sections.Count(section =>
            section.SectionType == MineRouteSectionType.Horizontal) * 7 -
            horizontalToDescentTransitions - verticalToHorizontalTransitions;
        Require(localPits.Length == expectedLocalPits &&
                localPits.All(zone => zone.GetComponent<Collider2D>() is { isTrigger: true }),
            "Each Level 12 horizontal gap needs fatal coverage except its parachute approach and " +
            "the first jump after a vertical climb.");

        Collider2D[] descentCorridors = SceneComponents<ParachuteDescentZone>()
            .Select(zone => zone.GetComponent<Collider2D>())
            .Where(collider => collider != null)
            .ToArray();
        string[] pitDescentOverlaps = localPits
            .SelectMany(pit => descentCorridors
                .Where(corridor => Intersects2D(pit.GetComponent<Collider2D>().bounds, corridor.bounds))
                .Select(corridor => $"{pit.name} -> {corridor.name}"))
            .ToArray();
        Require(pitDescentOverlaps.Length == 0,
            "Level 12 bottomless-pit triggers must not intrude invisibly into a later parachute shaft: " +
            string.Join(", ", pitDescentOverlaps));

        BoxCollider2D[] shaftWalls = SceneComponents<BoxCollider2D>().Where(collider =>
            collider.name.IndexOf("Descent", StringComparison.OrdinalIgnoreCase) >= 0 &&
            collider.name.IndexOf("Rock Wall", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(shaftWalls.All(wall => localPits.All(pit =>
                !Intersects2D(wall.bounds, pit.GetComponent<Collider2D>().bounds))),
            "Level 12 shaft walls must not create a survivable ledge through a bottomless-pit trigger.");

        FatalFallZone[] globalAbyss = SceneComponents<FatalFallZone>().Where(zone =>
            zone.name == "Level 12 Global Off-Route Abyss").ToArray();
        BoxCollider2D abyssTrigger = globalAbyss.Length == 1
            ? globalAbyss[0].GetComponent<BoxCollider2D>()
            : null;
        Require(abyssTrigger != null && abyssTrigger.isTrigger &&
                abyssTrigger.bounds.max.y <
                sections.Min(section => Mathf.Min(section.Entry.y, section.Exit.y)) - 4f,
            "Level 12 needs one safely low global abyss so an off-map fall cannot become a walkable void.");

        foreach (MineRouteSection section in sections)
        {
            Transform background = section.transform.Find(
                $"Section {section.Order:00} {section.SectionType} Modular Backdrop");
            Require(background != null,
                $"Level 12 section {section.Order} is missing its modular overscan background.");
            SpriteRenderer tile = background.GetComponent<SpriteRenderer>();
            string expectedArt = section.SectionType switch
            {
                MineRouteSectionType.Horizontal => MixedHorizontalBackdropArt,
                MineRouteSectionType.VerticalUp => MixedVerticalBackdropArt,
                MineRouteSectionType.AngledUp => MixedDiagonalBackdropArt,
                MineRouteSectionType.VerticalDown => MixedDescentBackdropArt,
                _ => FarCaveBackdropArt
            };
            Require(tile != null && tile.sprite != null && tile.drawMode == SpriteDrawMode.Tiled &&
                    NormalizePath(AssetDatabase.GetAssetPath(tile.sprite)) == NormalizePath(expectedArt) &&
                    Mathf.Abs(tile.transform.lossyScale.x - 1f) <= .01f &&
                    Mathf.Abs(tile.transform.lossyScale.y - 1f) <= .01f &&
                    Mathf.Abs(SignedZ(tile.transform.eulerAngles.z)) <= 1f &&
                    tile.sortingOrder <= -110 && tile.color.r < .5f && tile.color.g < .5f && tile.color.b < .5f &&
                    background.GetComponent<Collider2D>() == null,
                $"Level 12 section {section.Order} must use its uniformly tiled, dim, non-collidable direction art.");

            Bounds coverage = tile.bounds;
            float requiredBelow = section.SectionType == MineRouteSectionType.Horizontal ? 24f : 10f;
            Require(coverage.min.x <= Mathf.Min(section.Entry.x, section.Exit.x) - 10f &&
                    coverage.max.x >= Mathf.Max(section.Entry.x, section.Exit.x) + 10f &&
                    coverage.min.y <= Mathf.Min(section.Entry.y, section.Exit.y) - requiredBelow &&
                    coverage.max.y >= Mathf.Max(section.Entry.y, section.Exit.y) + 10f,
                $"Level 12 section {section.Order} background does not cover the camera overscan/fall view.");
        }

        SpriteRenderer farFill = SceneComponents<SpriteRenderer>().SingleOrDefault(renderer =>
            renderer.name == "Level 12 Continuous Far Cave Fill");
        float routeMinX = sections.Min(section => Mathf.Min(section.Entry.x, section.Exit.x));
        float routeMaxX = sections.Max(section => Mathf.Max(section.Entry.x, section.Exit.x));
        float routeMinY = sections.Min(section => Mathf.Min(section.Entry.y, section.Exit.y));
        float routeMaxY = sections.Max(section => Mathf.Max(section.Entry.y, section.Exit.y));
        Require(farFill != null && farFill.drawMode == SpriteDrawMode.Tiled &&
                NormalizePath(AssetDatabase.GetAssetPath(farFill.sprite)) == NormalizePath(FarCaveBackdropArt) &&
                farFill.bounds.min.x <= routeMinX - 20f && farFill.bounds.max.x >= routeMaxX + 20f &&
                farFill.bounds.min.y <= routeMinY - 16f && farFill.bounds.max.y >= routeMaxY + 16f,
            "Level 12 needs a continuous far-cave fill beyond the complete camera envelope.");

        BoxCollider2D[] visibleWalls = SceneComponents<BoxCollider2D>().Where(collider =>
            collider.name.IndexOf("Descent", StringComparison.OrdinalIgnoreCase) >= 0 &&
            collider.name.IndexOf("Rock Wall", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(visibleWalls.Length == 6 && visibleWalls.All(wall =>
                wall.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer =>
                    renderer.sprite != null && renderer.sortingOrder == 1 &&
                    (NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(PlatformArt) ||
                     NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(SharedRockFillArt))) &&
                wall.GetComponentInChildren<ThemedMetalFlakes>(true) != null),
            "Every Level 12 shaft collider needs a matching centrally themed bronze-flecked rock wall visual.");

        Require(SceneComponents<GreenCrystalCollectible>().Count(gem => gem.Value == 1) >= 20,
            "The long Level 12 route needs regular green-crystal rewards.");
        return pathLength;
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
        Require(lengths[3] >= 24f,
            "Level 3 must remain a deep introductory parachute descent.");
        Require(lengths[9] > lengths[6] + 10f,
            "Horizontal Levels 6 and 9 must become progressively longer.");
    }

    private static void ValidateSilverDungeonWhenBuilt()
    {
        bool overviewExists = File.Exists(ProjectFilePath(SilverOverview));
        bool levelExists = File.Exists(ProjectFilePath(SilverLevel));
        if (!overviewExists && !levelExists)
        {
            Debug.LogWarning("SILVER VALIDATION DEFERRED: SilverDungeonOverview and " +
                "SilverLevel1_SilverLode have not been generated yet.");
            return;
        }

        Require(overviewExists && levelExists,
            "SilverDungeonOverview and SilverLevel1_SilverLode must be generated together.");
        ValidateSilverOverview();
        ValidateSilverLevel();
    }

    private static void ValidateSilverOverview()
    {
        OpenScene(SilverOverview);
        Camera[] cameras = SceneComponents<Camera>();
        Require(cameras.Length == 1 && cameras[0].enabled && cameras[0].CompareTag("MainCamera"),
            "Silver overview needs one enabled MainCamera.");
        SceneLoadButton[] sceneButtons = SceneComponents<SceneLoadButton>();
        Require(sceneButtons.Any(button => button.TargetScene == "SilverLevel1_SilverLode" &&
                                           button.BeginsPlaytestRun),
            "Silver overview needs a playtest-aware button that starts Silver Dungeon Level 1.");
        Require(sceneButtons.Any(button => button.TargetScene == "DungeonOverview"),
            "Silver overview needs a route back to the Bronze dungeon overview.");
        Require(SceneComponents<DungeonOverviewBoundary>().Length == 1,
            "Silver overview must end the temporary MINER sandbox when it is entered.");
        ValidateInputSystemEventSystem("Silver overview", true);
    }

    private static void ValidateSilverLevel()
    {
        OpenScene(SilverLevel);
        Physics2D.SyncTransforms();

        Camera[] cameras = SceneComponents<Camera>();
        Require(cameras.Length == 1 && cameras[0].enabled && cameras[0].orthographic &&
                cameras[0].CompareTag("MainCamera"),
            "Silver Level 1 needs one enabled orthographic MainCamera.");

        HeroMovement[] heroes = SceneComponents<HeroMovement>();
        Require(heroes.Length == 1, "Silver Level 1 must contain exactly one miner.");
        HeroMovement hero = heroes[0];
        PlayerHealth health = hero.GetComponent<PlayerHealth>();
        MineRunInventory inventory = hero.GetComponent<MineRunInventory>();
        ParachuteDescentController glider = hero.GetComponent<ParachuteDescentController>();
        HangGliderVisualController gliderVisual = hero.GetComponent<HangGliderVisualController>();
        Require(health != null && SerializedInt(health, "maxHealth") == 7,
            "Silver Level 1 must serialize the game-wide seven-heart starting health.");
        Require(inventory != null && inventory.DungeonId == GameProgress.SilverDungeonId &&
                inventory.LevelNumber == 1 && inventory.HasStatusDisplay,
            "Silver Level 1 inventory must be scoped to silver/1 and display its counted key total.");
        SpriteRenderer gliderRenderer = hero.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(renderer => renderer.name.IndexOf("glider", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        renderer.name.IndexOf("parachute", StringComparison.OrdinalIgnoreCase) >= 0);
        Require(glider != null && gliderVisual != null && gliderVisual.HasCompleteDirectionalArt &&
                gliderVisual.CurrentState == HangGliderVisualState.Stowed &&
                gliderVisual.GliderRenderer == gliderRenderer &&
                gliderRenderer != null && !gliderRenderer.enabled &&
                gliderRenderer.sprite != null &&
                NormalizePath(AssetDatabase.GetAssetPath(gliderRenderer.sprite)) == NormalizePath(HangGliderArt) &&
                gliderRenderer.transform.localPosition.y > 0f && gliderRenderer.transform.localScale.y > 0f &&
                gliderRenderer.sprite.bounds.size.x > gliderRenderer.sprite.bounds.size.y * 1.5f,
            "Silver miner needs a smaller animated hover/float/dive/directional hang glider hidden until X deploys it.");

        AutomatedPlaytestWaypoint[] waypoints = SceneComponents<AutomatedPlaytestWaypoint>()
            .OrderBy(waypoint => waypoint.Order).ToArray();
        int firstWaypointOrder = waypoints.Length == 0 ? -1 : waypoints[0].Order;
        Require(waypoints.Length >= 20 && (firstWaypointOrder == 0 || firstWaypointOrder == 1) &&
                waypoints.Select(waypoint => waypoint.Order)
                    .SequenceEqual(Enumerable.Range(firstWaypointOrder, waypoints.Length)),
            "Silver Level 1 needs at least 20 contiguous route waypoints for its long hand-drawn path.");
        Require(waypoints.Count(waypoint => waypoint.Mode == AutomatedWaypointMode.AirbornePass &&
                                           waypoint.DeployParachute) >= 6,
            "Silver Level 1 needs glider-enabled airborne waypoints through both major chute sections.");

        ParachuteDescentZone[] chuteZones = SceneComponents<ParachuteDescentZone>();
        Require(chuteZones.Length >= 2 && chuteZones.All(zone =>
                zone.GetComponent<Collider2D>() is { enabled: true, isTrigger: true }),
            "Silver Level 1 needs both major camera-framing chute zones as non-solid triggers.");
        Require(chuteZones.All(zone => zone.MinimumDepth >= 12f),
            "Each Silver chute camera zone must cover a substantial descent.");

        BronzeKeyCollectible[] keys = SceneComponents<BronzeKeyCollectible>();
        RewardChest[] chests = SceneComponents<RewardChest>();
        Require(keys.Length >= 5 && chests.Length >= 5 && keys.Length == chests.Length,
            "Silver Level 1 needs multiple keys and an equal number of key-consuming chests.");
        Require(keys.All(key => key.DungeonId == GameProgress.SilverDungeonId && key.LevelNumber == 1 &&
                                key.GetComponent<Collider2D>() is { enabled: true, isTrigger: true }) &&
                keys.Select(key => key.PickupId).Distinct(StringComparer.Ordinal).Count() == keys.Length,
            "Every Silver key needs a unique persisted pickup ID in the silver/1 inventory scope.");
        Require(chests.All(chest => chest.DungeonId == GameProgress.SilverDungeonId && chest.LevelNumber == 1 &&
                                     chest.GetComponent<Collider2D>() is { enabled: true, isTrigger: true }) &&
                chests.Select(chest => chest.ChestId).Distinct(StringComparer.Ordinal).Count() == chests.Length,
            "Every Silver chest needs a unique persisted chest ID in the silver/1 inventory scope.");
        foreach (RewardChest chest in chests)
        {
            SpriteRenderer renderer = chest.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite != null &&
                    NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(SilverChestClosedArt) &&
                    chest.OpenedSprite != null &&
                    NormalizePath(AssetDatabase.GetAssetPath(chest.OpenedSprite)) == NormalizePath(SilverChestOpenArt) &&
                    chest.OpenedSprite != renderer.sprite && chest.TotalOpeningDuration >= .35f,
                $"Silver chest '{chest.name}' needs metal-bound closed/open keyhole art and a visible opening animation.");
        }

        ThemedMetalFlakes[] flakes = SceneComponents<ThemedMetalFlakes>();
        DungeonVisualTheme[] themes = flakes.Select(flake => flake.Theme).Where(theme => theme != null)
            .Distinct().ToArray();
        Require(flakes.Length >= 6 && themes.Length == 1 &&
                themes[0].DungeonId == GameProgress.SilverDungeonId &&
                themes[0].MetalFlakeDensity > 0f &&
                flakes.All(flake => flake.EffectiveFlakeSprite != null),
            "Silver rock surfaces must share one Silver theme asset so metal color, density, seed, and flake sprite are changed centrally.");
        Color silver = themes[0].MetalBase;
        Require(Mathf.Max(silver.r, Mathf.Max(silver.g, silver.b)) -
                Mathf.Min(silver.r, Mathf.Min(silver.g, silver.b)) <= .22f,
            "Silver Level 1 theme metal must remain a neutral silver rather than a bronze tint.");

        SpriteRenderer[] sceneRenderers = SceneComponents<SpriteRenderer>();
        HashSet<string> rendererPaths = sceneRenderers.Where(renderer => renderer.sprite != null)
            .Select(renderer => NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string distantBackgroundPath=NormalizePath(DistantMineBackgroundArt);
        string foregroundPanelPath=NormalizePath(ForegroundRockPanelArt);
        Require(rendererPaths.Contains(distantBackgroundPath) &&
                rendererPaths.Contains(foregroundPanelPath),
            "Silver cave must use the quiet distant-rock background and unified foreground nine-slice panel.");
        Require(themes[0].DistantBackgroundSprite!=null && themes[0].ForegroundRockPanelSprite!=null &&
                NormalizePath(AssetDatabase.GetAssetPath(themes[0].DistantBackgroundSprite))==distantBackgroundPath &&
                NormalizePath(AssetDatabase.GetAssetPath(themes[0].ForegroundRockPanelSprite))==foregroundPanelPath,
            "Silver background and foreground rock panel must be assigned centrally on the dungeon theme.");

        SpriteRenderer[] distantBackgrounds=sceneRenderers.Where(renderer=>renderer.sprite!=null &&
            NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite))==distantBackgroundPath).ToArray();
        Require(distantBackgrounds.Length==1 &&
                distantBackgrounds[0].name.IndexOf("Background",StringComparison.OrdinalIgnoreCase)>=0 &&
                distantBackgrounds[0].GetComponentsInParent<Collider2D>(true).Length==0 &&
                distantBackgrounds[0].GetComponentsInChildren<Collider2D>(true).Length==0,
            "Silver needs exactly one clearly named, fully non-collidable distant-rock background.");

        SpriteRenderer[] foregroundPanels=sceneRenderers.Where(renderer=>renderer.sprite!=null &&
            NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite))==foregroundPanelPath).ToArray();
        Require(foregroundPanels.Length>=30 && foregroundPanels.All(renderer=>
                renderer.drawMode==SpriteDrawMode.Tiled && renderer.transform.localRotation==Quaternion.identity &&
                renderer.sprite.border.x>0f && renderer.sprite.border.y>0f &&
                renderer.sprite.border.z>0f && renderer.sprite.border.w>0f),
            "Every Silver foreground platform/wall must use the unrotated bordered panel; edge direction comes from the authored tile, not rotated corner pieces.");
        Require(foregroundPanels.All(renderer=>
                renderer.name.IndexOf("Corner",StringComparison.OrdinalIgnoreCase)<0 &&
                renderer.name.IndexOf("Edge",StringComparison.OrdinalIgnoreCase)<0),
            "Silver foreground rendering must not contain free-floating edge or corner decorations.");

        BoxCollider2D[] silverGround=SceneComponents<BoxCollider2D>().Where(collider=>
            !collider.isTrigger && collider.gameObject.layer==LayerMask.NameToLayer("Ground")).ToArray();
        Require(silverGround.Length>=25 && silverGround.All(collider=>
        {
            SpriteRenderer panel=collider.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(renderer=>
                renderer.sprite!=null &&
                NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite))==foregroundPanelPath);
            if(panel==null) return false;
            Bounds visual=panel.bounds;
            Bounds solid=collider.bounds;
            return visual.min.x<=solid.min.x+.05f && visual.max.x>=solid.max.x-.05f &&
                   visual.min.y<=solid.min.y+.05f && visual.max.y>=solid.max.y-.05f;
        }), "Every solid Silver rock surface must be visually covered by the same foreground panel so collision is readable.");
        Require(foregroundPanels.Count(renderer=>renderer.name.IndexOf("Single-Thickness",StringComparison.OrdinalIgnoreCase)>=0 &&
                renderer.size.y<=1.25f)>=20,
            "Silver rock tiles must support the route's single-thickness platforms without three-by-two corner assemblies or seams.");

        FakeWallReveal[] fakeWalls = SceneComponents<FakeWallReveal>();
        Require(fakeWalls.Length >= 1 && fakeWalls.All(wall =>
                wall.GetComponentsInChildren<Collider2D>(true).All(collider => !collider.enabled || collider.isTrigger)),
            "Silver Level 1 needs a visually solid but collision-free fake rock wall for the secret room.");
        GreenCrystalCollectible[] gems = SceneComponents<GreenCrystalCollectible>();
        Require(gems.Any(gem => gem.Value == 5) && gems.Any(gem => gem.Value == 20),
            "The Silver secret route needs both blue and purple gem rewards.");
        Require(fakeWalls.Any(wall => gems.Count(gem => gem.Value >= 5 &&
                Vector2.Distance(gem.transform.position, wall.transform.position) <= 15f) >= 3),
            "Blue/purple rewards must be clustered behind or beside the fake rock wall, not on the main path.");

        GreenCrystalCollectible[] cutGems = gems.Where(gem => gem.GetComponent<SpriteRenderer>() != null).ToArray();
        Require(cutGems.Length >= 12 && cutGems.All(gem =>
        {
            SpriteRenderer renderer = gem.GetComponent<SpriteRenderer>();
            float scale = Mathf.Max(Mathf.Abs(gem.transform.localScale.x), Mathf.Abs(gem.transform.localScale.y));
            return NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(CutGemArt) &&
                   scale >= .45f && scale <= .75f &&
                   gem.GetComponent<SpriteShineAnimator>() != null;
        }), "Silver gems must use the cut-gem art at about 60% scale with an animated shine.");

        DamageZone[] spikes = SceneComponents<DamageZone>().Where(zone =>
            zone.name.IndexOf("spike", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        Require(spikes.Length >= 6 && spikes.All(spike =>
        {
            SpriteRenderer renderer = spike.GetComponent<SpriteRenderer>();
            float scale = Mathf.Max(Mathf.Abs(spike.transform.localScale.x), Mathf.Abs(spike.transform.localScale.y));
            return renderer != null && renderer.sprite != null &&
                   NormalizePath(AssetDatabase.GetAssetPath(renderer.sprite)) == NormalizePath(PolishedSpikeArt) &&
                   scale >= .35f && scale <= .65f &&
                   spike.GetComponent<SpriteShineAnimator>() != null &&
                   SerializedInt(spike, "damage") == 1;
        }), "Silver spikes must use polished bronze art at about 50% scale, shine, and deal one heart.");

        OscillatingHazard[] movingHazards = SceneComponents<OscillatingHazard>();
        Require(movingHazards.Length >= 4 &&
                movingHazards.Any(hazard => Mathf.Abs(SerializedVector2(hazard, "axis").x) > .8f) &&
                movingHazards.Any(hazard => Mathf.Abs(SerializedVector2(hazard, "axis").y) > .8f),
            "Silver Level 1 needs multiple moving hazards with both horizontal and vertical double-arrow axes.");
        Require(SceneComponents<MovingPlatform>().Length >= 2,
            "Silver Level 1 needs multiple moving platforms matching the map's axis arrows.");

        MineDoorAnimator[] doors = SceneComponents<MineDoorAnimator>();
        Require(doors.Length == 2 && doors.All(door => DoorAnimatorIsConfigured(door)),
            "Silver entrance and exit both need closed/open sprites and must open before traversal.");
        LevelExitDoor[] exits = SceneComponents<LevelExitDoor>();
        Require(exits.Length == 1 && exits[0].DestinationScene == "SilverDungeonOverview",
            "Silver Level 1 exit must return to SilverDungeonOverview.");
        MineDoorAnimator silverExitAnimator = exits.Length == 1
            ? exits[0].GetComponent<MineDoorAnimator>()
            : null;
        Require(silverExitAnimator != null && silverExitAnimator.PassageBlocker != null &&
                silverExitAnimator.PassageBlocker.enabled &&
                !silverExitAnimator.PassageBlocker.isTrigger,
            "Silver Level 1 exit must be solid until its opening animation completes.");

        MineLevelMenuController[] menus = SceneComponents<MineLevelMenuController>();
        Require(menus.Length == 1, "Silver Level 1 needs one pause/shop menu coordinator.");
        ValidateMidLevelShop("Silver Level 1", menus[0]);
        ValidateInputSystemEventSystem("Silver Level 1", false);
    }

    private static void ValidateBuildSettings()
    {
        string[] bronze = new[] { Overview, GameOver }.Concat(Levels.Select(level => level.Path)).ToArray();
        bool silverExists = File.Exists(ProjectFilePath(SilverOverview)) || File.Exists(ProjectFilePath(SilverLevel));
        string[] expected = silverExists
            ? bronze.Concat(new[] { SilverOverview, SilverLevel }).ToArray()
            : bronze;
        string[] actual = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Require(actual.SequenceEqual(expected),
            silverExists
                ? "Build Settings must contain the Bronze scenes followed by SilverDungeonOverview and SilverLevel1_SilverLode."
                : "Build Settings must contain DungeonOverview, GameOver, then Bronze Mines Levels 1-12 in order.");
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

    private static void ValidateInputSystemEventSystem(string sceneLabel, bool requireInitialSelection)
    {
        EventSystem[] eventSystems = SceneComponents<EventSystem>();
        Require(eventSystems.Length == 1, $"{sceneLabel} must contain exactly one EventSystem.");
        EventSystem eventSystem = eventSystems[0];
        InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
        Require(module != null && module.actionsAsset != null,
            $"{sceneLabel} EventSystem needs InputSystemUIInputModule actions for stick and D-pad navigation.");
        Require(eventSystem.GetComponent<StandaloneInputModule>() == null,
            $"{sceneLabel} must not use the legacy UI module that omits reliable gamepad D-pad navigation.");
        if (requireInitialSelection)
        {
            Require(eventSystem.firstSelectedGameObject != null,
                $"{sceneLabel} controller navigation needs an initial selected button.");
        }
    }

    private static bool HasPersistentListener(UnityEngine.UI.Button button, UnityEngine.Object target,
        string methodName)
    {
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == target &&
                button.onClick.GetPersistentMethodName(index) == methodName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ParachuteDescentControllerHasGlobalFlightContract()
    {
        Type type = typeof(ParachuteDescentController);
        return type.GetProperty(nameof(ParachuteDescentController.HoverVerticalSpeed)) != null &&
               type.GetProperty(nameof(ParachuteDescentController.HoverResponse)) != null &&
               type.GetProperty(nameof(ParachuteDescentController.FastDescentGravity)) != null &&
               type.GetProperty(nameof(ParachuteDescentController.MaximumHorizontalSpeed)) != null &&
               type.GetProperty(nameof(ParachuteDescentController.GliderVerticalInput)) != null &&
               type.GetProperty(nameof(ParachuteDescentController.IsCameraTrackingDescent)) != null;
    }

    private static void ValidateDoorAnimation(GameObject doorObject, string label)
    {
        MineDoorAnimator[] animators = doorObject.GetComponentsInChildren<MineDoorAnimator>(true);
        Require(animators.Length == 1 && DoorAnimatorIsConfigured(animators[0]),
            $"{label} door needs one configured closed-to-open animation before the miner traverses it.");
    }

    private static bool DoorAnimatorIsConfigured(MineDoorAnimator animator)
    {
        if (animator == null || animator.ClosedRenderer == null || animator.OpenRenderer == null ||
            animator.ClosedRenderer == animator.OpenRenderer ||
            animator.ClosedRenderer.sprite == null || animator.OpenRenderer.sprite == null ||
            animator.OpeningSeconds < .2f || animator.ClosingSeconds < .15f)
        {
            return false;
        }

        Vector2 closedSize = animator.ClosedRenderer.bounds.size;
        Vector2 openSize = animator.OpenRenderer.bounds.size;
        return Mathf.Abs(closedSize.x - openSize.x) <= Mathf.Max(.08f, closedSize.x * .08f) &&
               Mathf.Abs(closedSize.y - openSize.y) <= Mathf.Max(.08f, closedSize.y * .08f) &&
               animator.ClosedRenderer != animator.OpenRenderer &&
               animator.OpeningSeconds >= .2f && animator.ClosingSeconds >= .15f;
    }

    private static void ValidateMidLevelShop(string label, MineLevelMenuController menu)
    {
        MidLevelShopController[] shops = SceneComponents<MidLevelShopController>();
        Require(shops.Length == 1, $"{label} needs exactly one in-level shop controller.");
        MidLevelShopController shop = shops[0];
        Require(menu != null && menu.MidLevelShop == shop && shop.LevelMenu == menu &&
                shop.ShopPanel != null && !shop.ShopPanel.activeSelf &&
                shop.BalanceDisplay != null && shop.StatusDisplay != null &&
                shop.PlayerHealth != null && shop.DefaultSelection != null,
            $"{label} in-level shop must be registered, fully wired, and serialized closed.");
        UnityEngine.UI.Button[] buttons = shop.ShopPanel
            .GetComponentsInChildren<UnityEngine.UI.Button>(true);
        Require(buttons.Any(button => HasPersistentListener(button, shop,
                    nameof(MidLevelShopController.BuyExtraLife))) &&
                buttons.Any(button => HasPersistentListener(button, shop,
                    nameof(MidLevelShopController.BuyHealthPotion))) &&
                buttons.Any(button => HasPersistentListener(button, shop,
                    nameof(MidLevelShopController.BuyHeartUpgrade))) &&
                buttons.Any(button => HasPersistentListener(button, shop,
                    nameof(MidLevelShopController.HideShop))) &&
                buttons.Any(button => HasPersistentListener(button, shop,
                    nameof(MidLevelShopController.ReturnToOverview))),
            $"{label} in-level shop needs wired life, potion, heart, close, and explicit overview buttons.");
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

    private static Vector2 SerializedVector2(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Require(property != null && property.propertyType == SerializedPropertyType.Vector2,
            $"{target.GetType().Name}.{propertyName} is missing from its serialized contract.");
        return property.vector2Value;
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
