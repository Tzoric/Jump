using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MineLevelBuilder
{
    private const string OldScenePath = "Assets/Scenes/Level1.unity";
    private const string ScenePath = "Assets/Scenes/Level1_TheMines.unity";
    private const string GeneratedArtFolder = "Assets/Art/Generated";
    private const string GameplayPrefabFolder = "Assets/PreFabs/Gameplay";

    [MenuItem("Jump/Level Tools/Build Level 1 - The Mines")]
    public static void Build()
    {
        EnsureFolders();
        Sprite platformSprite = CreatePlatformSprite();
        Sprite spikeSprite = CreateSpikeSprite();

        string sceneToOpen = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
            ? ScenePath
            : OldScenePath;
        Scene scene = EditorSceneManager.OpenScene(sceneToOpen, OpenSceneMode.Single);

        GameObject previousRoot = GameObject.Find("Level 1 - The Mines Gameplay");
        if (previousRoot != null)
        {
            Object.DestroyImmediate(previousRoot);
        }

        Transform gameplayRoot = new GameObject("Level 1 - The Mines Gameplay").transform;
        CreateMineAtmosphere(gameplayRoot);
        CreateGameplayPrefabs(platformSprite, spikeSprite);
        PlaceMovingPlatforms(gameplayRoot);
        PlaceHazards(gameplayRoot);
        ConfigurePlayerAndHud();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (scene.path == OldScenePath)
        {
            string error = AssetDatabase.MoveAsset(OldScenePath, ScenePath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException($"Could not rename Level 1 scene: {error}");
            }
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Level 1 - The Mines with health, moving platforms, and spike hazards.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Generated");
        EnsureFolder("Assets/PreFabs", "Gameplay");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Sprite CreatePlatformSprite()
    {
        string path = $"{GeneratedArtFolder}/MineMovingPlatform.png";
        if (!File.Exists(path))
        {
            var texture = NewTransparentTexture(48, 16);
            Color32 gold = new(194, 143, 47, 255);
            Color32 darkGold = new(94, 63, 27, 255);
            Color32 steel = new(87, 93, 103, 255);
            Color32 highlight = new(160, 166, 170, 255);

            for (int y = 2; y < 14; y++)
            {
                for (int x = 1; x < 47; x++)
                {
                    bool border = x <= 2 || x >= 45 || y <= 3 || y >= 12;
                    bool seam = x == 16 || x == 32;
                    texture.SetPixel(x, y, border ? darkGold : (seam ? gold : steel));
                }
            }

            for (int x = 4; x < 44; x += 8)
            {
                texture.SetPixel(x, 11, highlight);
                texture.SetPixel(x + 1, 11, highlight);
            }

            SaveSpriteTexture(texture, path, 16f);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite CreateSpikeSprite()
    {
        string path = $"{GeneratedArtFolder}/MineCeilingSpike.png";
        if (!File.Exists(path))
        {
            var texture = NewTransparentTexture(32, 32);
            Color32 outline = new(45, 38, 40, 255);
            Color32 stone = new(116, 108, 105, 255);
            Color32 highlight = new(181, 165, 145, 255);

            for (int y = 2; y < 30; y++)
            {
                float t = y / 29f;
                int halfWidth = Mathf.RoundToInt(t * 13f);
                for (int x = 16 - halfWidth; x <= 16 + halfWidth; x++)
                {
                    bool edge = x == 16 - halfWidth || x == 16 + halfWidth || y >= 28;
                    texture.SetPixel(x, y, edge ? outline : (x < 15 ? highlight : stone));
                }
            }

            SaveSpriteTexture(texture, path, 16f);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D NewTransparentTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        var clear = new Color32[width * height];
        texture.SetPixels32(clear);
        return texture;
    }

    private static void SaveSpriteTexture(Texture2D texture, string path, float pixelsPerUnit)
    {
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void CreateGameplayPrefabs(Sprite platformSprite, Sprite spikeSprite)
    {
        GameObject platform = new("Moving Platform");
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.tag = "Ground";
        platform.AddComponent<SpriteRenderer>().sprite = platformSprite;
        var platformCollider = platform.AddComponent<BoxCollider2D>();
        platformCollider.size = new Vector2(2.9f, 0.72f);
        var platformBody = platform.AddComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;
        platformBody.freezeRotation = true;
        platform.AddComponent<MovingPlatform>();
        PrefabUtility.SaveAsPrefabAsset(platform, $"{GameplayPrefabFolder}/MovingPlatform.prefab");
        Object.DestroyImmediate(platform);

        GameObject breakable = new("Weight-Sensitive Platform");
        breakable.layer = LayerMask.NameToLayer("Ground");
        breakable.tag = "Ground";
        var breakableRenderer = breakable.AddComponent<SpriteRenderer>();
        breakableRenderer.sprite = platformSprite;
        breakableRenderer.color = new Color32(178, 119, 71, 255);
        var breakableCollider = breakable.AddComponent<BoxCollider2D>();
        breakableCollider.size = new Vector2(2.9f, 0.72f);
        var breakableBody = breakable.AddComponent<Rigidbody2D>();
        breakableBody.bodyType = RigidbodyType2D.Kinematic;
        breakableBody.freezeRotation = true;
        breakable.AddComponent<WeightedBreakablePlatform>();
        PrefabUtility.SaveAsPrefabAsset(breakable, $"{GameplayPrefabFolder}/WeightedBreakablePlatform.prefab");
        Object.DestroyImmediate(breakable);

        GameObject fallingSpike = new("Falling Ceiling Spike");
        fallingSpike.transform.localScale = Vector3.one * 0.7f;
        var fallingRenderer = fallingSpike.AddComponent<SpriteRenderer>();
        fallingRenderer.sprite = spikeSprite;
        fallingRenderer.sortingOrder = 2;
        var fallingCollider = fallingSpike.AddComponent<BoxCollider2D>();
        fallingCollider.size = new Vector2(1.25f, 1.7f);
        var fallingBody = fallingSpike.AddComponent<Rigidbody2D>();
        fallingBody.bodyType = RigidbodyType2D.Kinematic;
        fallingBody.freezeRotation = true;
        fallingBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        fallingSpike.AddComponent<FallingSpike>();
        PrefabUtility.SaveAsPrefabAsset(fallingSpike, $"{GameplayPrefabFolder}/FallingCeilingSpike.prefab");
        Object.DestroyImmediate(fallingSpike);

        GameObject floorSpikes = new("Floor Spikes");
        floorSpikes.transform.localScale = new Vector3(1.4f, 0.65f, 1f);
        floorSpikes.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        var floorRenderer = floorSpikes.AddComponent<SpriteRenderer>();
        floorRenderer.sprite = spikeSprite;
        floorRenderer.sortingOrder = 2;
        var damageCollider = floorSpikes.AddComponent<BoxCollider2D>();
        damageCollider.isTrigger = true;
        damageCollider.size = new Vector2(1.2f, 1.3f);
        floorSpikes.AddComponent<DamageZone>();
        PrefabUtility.SaveAsPrefabAsset(floorSpikes, $"{GameplayPrefabFolder}/FloorSpikes.prefab");
        Object.DestroyImmediate(floorSpikes);
    }

    private static void PlaceMovingPlatforms(Transform root)
    {
        Transform group = new GameObject("Moving Platforms").transform;
        group.SetParent(root);
        CreateMovingPlatform(group, "Lower Crossing", new Vector3(-29f, -4.7f, 0f), new Vector2(8f, 0f), 1.8f);
        CreateMovingPlatform(group, "Shaft Lift", new Vector3(-42f, 10f, 0f), new Vector2(0f, 5f), 1.8f);
        CreateMovingPlatform(group, "Upper Ore Lift", new Vector3(-23f, 29f, 0f), new Vector2(6f, 0f), 2f);

        Transform breakableGroup = new GameObject("Weight-Sensitive Platforms").transform;
        breakableGroup.SetParent(root);
        CreateBreakablePlatform(breakableGroup, "Cracked Timber Bridge A", new Vector3(-31f, 1.5f, 0f), 3.2f);
        CreateBreakablePlatform(breakableGroup, "Cracked Timber Bridge B", new Vector3(-24f, 16f, 0f), 2.8f);
        CreateBreakablePlatform(breakableGroup, "Cracked Timber Bridge C", new Vector3(-18f, 31f, 0f), 2.5f);
    }

    private static void CreateMovingPlatform(Transform parent, string name, Vector3 position, Vector2 offset, float speed)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/MovingPlatform.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.GetComponent<MovingPlatform>().Configure(offset, speed, 0.55f);
    }

    private static void CreateBreakablePlatform(Transform parent, string name, Vector3 position, float loadSeconds)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/WeightedBreakablePlatform.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.GetComponent<WeightedBreakablePlatform>().Configure(loadSeconds, 0.3f, 3.5f);
    }

    private static void PlaceHazards(Transform root)
    {
        Transform fallingGroup = new GameObject("Falling Ceiling Spikes").transform;
        fallingGroup.SetParent(root);
        CreateFallingSpike(fallingGroup, new Vector3(-34f, 1.2f, 0f), 7f);
        CreateFallingSpike(fallingGroup, new Vector3(-21f, 9.5f, 0f), 8f);
        CreateFallingSpike(fallingGroup, new Vector3(-29f, 21f, 0f), 8f);
        CreateFallingSpike(fallingGroup, new Vector3(-15f, 35f, 0f), 9f);

        Transform floorGroup = new GameObject("Floor Spikes").transform;
        floorGroup.SetParent(root);
        CreateFloorSpikes(floorGroup, new Vector3(-27f, -5.7f, 0f));
        CreateFloorSpikes(floorGroup, new Vector3(-19f, 4.1f, 0f));
        CreateFloorSpikes(floorGroup, new Vector3(-31f, 12.1f, 0f));

        GameObject killZone = new("Mine Pit Kill Zone");
        killZone.transform.SetParent(root);
        killZone.transform.position = new Vector3(-35f, -14f, 0f);
        var collider = killZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(120f, 3f);
        killZone.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateFallingSpike(Transform parent, Vector3 position, float detectionRange)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/FallingCeilingSpike.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.GetComponent<FallingSpike>().Configure(detectionRange, 1.15f, 0.4f, 2.8f, 1);
    }

    private static void CreateFloorSpikes(Transform parent, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayPrefabFolder}/FloorSpikes.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = position;
    }

    private static void ConfigurePlayerAndHud()
    {
        HeroMovement hero = Object.FindFirstObjectByType<HeroMovement>();
        if (hero == null)
        {
            throw new MissingReferenceException("Level 1 does not contain a HeroMovement component.");
        }

        PlayerHealth health = hero.GetComponent<PlayerHealth>();
        if (health == null)
        {
            health = hero.gameObject.AddComponent<PlayerHealth>();
        }

        if (hero.GetComponent<PlayerWeight>() == null)
        {
            hero.gameObject.AddComponent<PlayerWeight>();
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            throw new MissingReferenceException("Level 1 does not contain a Canvas.");
        }

        Transform oldHud = canvas.transform.Find("Mine HUD");
        if (oldHud != null)
        {
            Object.DestroyImmediate(oldHud.gameObject);
        }

        GameObject hud = new("Mine HUD", typeof(RectTransform));
        hud.transform.SetParent(canvas.transform, false);
        RectTransform hudRect = (RectTransform)hud.transform;
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        TextMeshProUGUI title = CreateHudText(hud.transform, "Level Title", "LEVEL 1  •  THE MINES", 30f,
            TextAlignmentOptions.Center, new Color32(238, 190, 82, 255));
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(500f, 48f);

        TextMeshProUGUI healthText = CreateHudText(hud.transform, "Health Display", "HEALTH  3 / 3", 24f,
            TextAlignmentOptions.Left, new Color32(235, 235, 225, 255));
        RectTransform healthRect = healthText.rectTransform;
        healthRect.anchorMin = new Vector2(0f, 1f);
        healthRect.anchorMax = new Vector2(0f, 1f);
        healthRect.pivot = new Vector2(0f, 1f);
        healthRect.anchoredPosition = new Vector2(24f, -24f);
        healthRect.sizeDelta = new Vector2(260f, 42f);

        health.ConfigureDisplay(healthText);
    }

    private static TextMeshProUGUI CreateHudText(Transform parent, string name, string text, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void CreateMineAtmosphere(Transform root)
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color32(12, 13, 20, 255);
        }

        Light2D global = Object.FindFirstObjectByType<Light2D>();
        if (global != null && global.lightType == Light2D.LightType.Global)
        {
            global.intensity = 0.55f;
            global.color = new Color32(160, 172, 205, 255);
        }

        Transform lights = new GameObject("Mine Lamps").transform;
        lights.SetParent(root);
        CreateMineLamp(lights, new Vector3(-53f, -3f, 0f));
        CreateMineLamp(lights, new Vector3(-34f, 7f, 0f));
        CreateMineLamp(lights, new Vector3(-22f, 19f, 0f));
        CreateMineLamp(lights, new Vector3(-15f, 38f, 0f));
    }

    private static void CreateMineLamp(Transform parent, Vector3 position)
    {
        GameObject lamp = new("Amber Mine Lamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = position;
        Light2D light = lamp.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color32(255, 179, 83, 255);
        light.intensity = 1.1f;
        light.pointLightInnerRadius = 1.5f;
        light.pointLightOuterRadius = 7f;
        light.falloffIntensity = 0.75f;
    }
}
