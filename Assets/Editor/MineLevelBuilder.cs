using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MineLevelBuilder
{
    private const string OldScenePath = "Assets/Scenes/Level1.unity";
    private const string LevelScenePath = "Assets/Scenes/Level1_TheMines.unity";
    private const string OverviewScenePath = "Assets/Scenes/DungeonOverview.unity";
    private const string HeroPrefabPath = "Assets/PreFabs/Hero.prefab";
    private const string GeneratedArtFolder = "Assets/Art/Generated";

    private const string PlatformArtPath = GeneratedArtFolder + "/MineMovingPlatform.png";
    private const string LevelBackdropPath = GeneratedArtFolder + "/MineLevel1BronzeBackdrop.png";
    private const string OverviewBackdropPath = GeneratedArtFolder + "/MineDungeonOverview.png";
    private const string ExitDoorPath = GeneratedArtFolder + "/MineExitDoor.png";

    private static readonly Color32 BronzeTint = new(196, 137, 78, 255);
    private static readonly Color32 Amber = new(244, 180, 82, 255);

    [MenuItem("Jump/Level Tools/Build Level 1 - The Mines")]
    public static void Build()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject heroPrefab = EnsureHeroPrefab();
        Sprite platformSprite = CreatePlatformSprite();
        Sprite levelBackdrop = ImportSprite(LevelBackdropPath, 32f, FilterMode.Point);
        Sprite overviewBackdrop = ImportSprite(OverviewBackdropPath, 100f, FilterMode.Point);
        Sprite exitDoorSprite = ImportSprite(ExitDoorPath, 256f, FilterMode.Point);

        BuildLevelScene(heroPrefab, platformSprite, levelBackdrop, exitDoorSprite);
        BuildDungeonOverviewScene(overviewBackdrop);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(OverviewScenePath, true),
            new EditorBuildSettingsScene(LevelScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        Debug.Log("Built a simple vertical Level 1, exit-door completion, and the Mines dungeon overview.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Generated");
        EnsureFolder("Assets", "PreFabs");
        EnsureFolder("Assets", "Scenes");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static GameObject EnsureHeroPrefab()
    {
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        if (existingPrefab != null)
        {
            SanitizeHeroPrefab();
            return AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        }

        string sourceScenePath = AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelScenePath) != null
            ? LevelScenePath
            : OldScenePath;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sourceScenePath) == null)
        {
            throw new FileNotFoundException("A scene containing the existing Hero is required to create the Hero prefab.");
        }

        EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
        HeroMovement sourceHero = Object.FindFirstObjectByType<HeroMovement>();
        if (sourceHero == null)
        {
            throw new MissingReferenceException("The existing level scene does not contain a HeroMovement component.");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceHero.gameObject, HeroPrefabPath);
        if (prefab == null)
        {
            throw new IOException("Unity could not create the reusable Hero prefab.");
        }

        SanitizeHeroPrefab();
        return AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
    }

    private static void SanitizeHeroPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HeroPrefabPath);
        try
        {
            for (int index = prefabRoot.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = prefabRoot.transform.GetChild(index);
                if (child.name != "FeetPosition")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            AudioListener listener = prefabRoot.GetComponent<AudioListener>();
            if (listener != null)
            {
                Object.DestroyImmediate(listener);
            }

            PlayerHealth health = prefabRoot.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ConfigureDisplay(null);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HeroPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Sprite CreatePlatformSprite()
    {
        if (!File.Exists(PlatformArtPath))
        {
            var texture = new Texture2D(48, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.SetPixels32(new Color32[48 * 16]);

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

            texture.Apply();
            File.WriteAllBytes(PlatformArtPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        return ImportSprite(PlatformArtPath, 16f, FilterMode.Point);
    }

    private static Sprite ImportSprite(string path, float pixelsPerUnit, FilterMode filterMode)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required generated art is missing: {path}");
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidDataException($"Unity could not import {path} as a texture.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filterMode;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void BuildLevelScene(GameObject heroPrefab, Sprite platformSprite, Sprite backdropSprite,
        Sprite exitDoorSprite)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject levelRoot = new("Level 1 - Bronze Shaft");
        CreateLevelBackdrop(levelRoot.transform, backdropSprite);
        CreateLighting(levelRoot.transform);

        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, scene);
        hero.name = "Hero";
        hero.transform.position = new Vector3(3f, -1.35f, 0f);
        PlayerHealth health = hero.GetComponent<PlayerHealth>() ?? hero.AddComponent<PlayerHealth>();
        if (hero.GetComponent<PlayerWeight>() == null)
        {
            hero.AddComponent<PlayerWeight>();
        }

        CreateCamera(hero.transform);
        CreateLevelGeometry(levelRoot.transform, platformSprite);
        CreateExitDoor(levelRoot.transform, exitDoorSprite);
        CreateLevelHud(health);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, LevelScenePath);
    }

    private static void CreateLevelBackdrop(Transform parent, Sprite sprite)
    {
        GameObject backdrop = new("Bronze Mine Shaft Backdrop");
        backdrop.transform.SetParent(parent);
        backdrop.transform.position = new Vector3(0f, 15.2f, 5f);
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;
    }

    private static void CreateLighting(Transform parent)
    {
        GameObject lightObject = new("Global Mine Light");
        lightObject.transform.SetParent(parent);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color32(190, 202, 225, 255);
        light.intensity = 0.8f;
    }

    private static void CreateCamera(Transform hero)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        camera.backgroundColor = new Color32(8, 11, 18, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.AddComponent<VerticalCameraFollow>().Configure(hero, 0f, 2f, 30.8f, 1.35f);
    }

    private static void CreateLevelGeometry(Transform parent, Sprite platformSprite)
    {
        Transform platforms = new GameObject("Beginner Vertical Route").transform;
        platforms.SetParent(parent);

        CreatePlatform(platforms, platformSprite, "Start Floor", new Vector2(0f, -2.7f), 11f);

        Vector2[] positions =
        {
            new(-2.8f, 0.5f),
            new(2.8f, 3.7f),
            new(-2.8f, 6.9f),
            new(2.8f, 10.1f),
            new(-2.8f, 13.3f),
            new(2.8f, 16.5f),
            new(-2.8f, 19.7f),
            new(2.8f, 22.9f),
            new(-2.8f, 26.1f),
            new(2.8f, 29.3f),
            new(-2.8f, 32.5f)
        };

        float[] widths = { 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 6.2f };
        for (int index = 0; index < positions.Length; index++)
        {
            CreatePlatform(platforms, platformSprite, $"Bronze Ledge {index + 1:00}", positions[index], widths[index]);
            CreateWaypoint(parent, positions[index] + Vector2.up * 1.05f, index + 1);
        }

        CreateBoundary(parent, "Left Shaft Wall", new Vector2(-9.2f, 15f), new Vector2(1f, 50f));
        CreateBoundary(parent, "Right Shaft Wall", new Vector2(9.2f, 15f), new Vector2(1f, 50f));

        GameObject pit = new("Respawn Pit");
        pit.transform.SetParent(parent);
        pit.transform.position = new Vector3(0f, -8.5f, 0f);
        BoxCollider2D pitCollider = pit.AddComponent<BoxCollider2D>();
        pitCollider.isTrigger = true;
        pitCollider.size = new Vector2(22f, 2f);
        pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreatePlatform(Transform parent, Sprite sprite, string name, Vector2 position, float width)
    {
        GameObject platform = new(name);
        platform.transform.SetParent(parent);
        platform.transform.position = position;
        platform.transform.localScale = new Vector3(width / 3f, 0.78f, 1f);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.tag = "Ground";

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = BronzeTint;
        renderer.sortingOrder = 2;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.92f, 0.68f);
    }

    private static void CreateBoundary(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject boundary = new(name);
        boundary.transform.SetParent(parent);
        boundary.transform.position = position;
        boundary.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void CreateWaypoint(Transform parent, Vector2 position, int order)
    {
        GameObject waypoint = new($"Playtest Waypoint {order:00}");
        waypoint.transform.SetParent(parent);
        waypoint.transform.position = position;
        waypoint.AddComponent<AutomatedPlaytestWaypoint>().Configure(order);
    }

    private static void CreateExitDoor(Transform parent, Sprite doorSprite)
    {
        GameObject door = new("Mine Exit Door");
        door.transform.SetParent(parent);
        door.transform.position = new Vector3(-4.4f, 35.05f, 0f);
        door.transform.localScale = Vector3.one * 0.9f;

        SpriteRenderer renderer = door.AddComponent<SpriteRenderer>();
        renderer.sprite = doorSprite;
        renderer.sortingOrder = 5;

        BoxCollider2D trigger = door.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.35f, 3.55f);
        trigger.offset = new Vector2(0f, -0.1f);
        door.AddComponent<LevelExitDoor>().Configure("DungeonOverview");

        GameObject glow = new("Exit Lamp Glow");
        glow.transform.SetParent(door.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        Light2D light = glow.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = Amber;
        light.intensity = 1.15f;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 4.5f;

        GameObject labelObject = new("Exit Label", typeof(TextMeshPro));
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = new Vector3(-4.4f, 37.75f, 0f);
        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = "MINE EXIT";
        label.fontSize = 3.2f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(255, 220, 145, 255);
        label.rectTransform.sizeDelta = new Vector2(5f, 1f);
        label.sortingOrder = 8;
    }

    private static void CreateLevelHud(PlayerHealth health)
    {
        Canvas canvas = CreateScreenCanvas("Level HUD");

        TextMeshProUGUI title = CreateUiText(canvas.transform, "Level Title", "LEVEL 1  |  BRONZE SHAFT", 28f,
            TextAlignmentOptions.Center, Amber);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(620f, 46f));

        TextMeshProUGUI healthText = CreateUiText(canvas.transform, "Health Display", "HEALTH  3 / 3", 22f,
            TextAlignmentOptions.Left, new Color32(235, 235, 225, 255));
        SetRect(healthText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(260f, 40f));
        health.ConfigureDisplay(healthText);

        TextMeshProUGUI instructions = CreateUiText(canvas.transform, "Instructions",
            "A / D OR ARROWS TO MOVE     SPACE TO JUMP     CLIMB TO THE EXIT", 20f,
            TextAlignmentOptions.Center, Color.white);
        instructions.textWrappingMode = TextWrappingModes.NoWrap;
        instructions.outlineWidth = 0.18f;
        instructions.outlineColor = new Color32(18, 14, 12, 235);
        SetRect(instructions.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(920f, 42f));
    }

    private static void BuildDungeonOverviewScene(Sprite backgroundSprite)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Canvas canvas = CreateScreenCanvas("Dungeon Overview Canvas");
        CreateFullScreenImage(canvas.transform, "Mine Overview Background", backgroundSprite, Color.white);
        CreateFullScreenImage(canvas.transform, "Readability Overlay", null, new Color(0.02f, 0.03f, 0.06f, 0.38f));

        TextMeshProUGUI heading = CreateUiText(canvas.transform, "Dungeon Heading", "DUNGEON 1", 32f,
            TextAlignmentOptions.Center, Amber);
        SetRect(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(500f, 44f));

        TextMeshProUGUI title = CreateUiText(canvas.transform, "Dungeon Name", "THE MINES", 58f,
            TextAlignmentOptions.Center, Color.white);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(720f, 82f));

        GameObject panel = CreatePanel(canvas.transform, "Level Selection Panel", new Color(0.035f, 0.045f, 0.07f, 0.86f));
        SetRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(610f, 390f));

        TextMeshProUGUI vein = CreateUiText(panel.transform, "Material Tier", "CURRENT VEIN: BRONZE", 23f,
            TextAlignmentOptions.Center, new Color32(224, 157, 91, 255));
        SetRect(vein.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(520f, 40f));

        TextMeshProUGUI description = CreateUiText(panel.transform, "Level Description",
            "LEVEL 1  -  BRONZE SHAFT\nLearn to move and jump as you climb to the mine exit.", 24f,
            TextAlignmentOptions.Center, Color.white);
        SetRect(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(520f, 100f));

        CreateLevelButton(panel.transform);

        TextMeshProUGUI locked = CreateUiText(panel.transform, "Future Levels", "LEVELS 2-5  |  COMING SOON", 19f,
            TextAlignmentOptions.Center, new Color32(150, 158, 174, 255));
        SetRect(locked.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(500f, 36f));

        TextMeshProUGUI progression = CreateUiText(canvas.transform, "Mine Material Progression",
            "BRONZE  >  SILVER  >  GOLD  >  RUBY / SAPPHIRE  >  DIAMOND", 20f,
            TextAlignmentOptions.Center, new Color32(225, 225, 225, 255));
        progression.outlineWidth = 0.15f;
        progression.outlineColor = Color.black;
        SetRect(progression.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(920f, 40f));

        GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, OverviewScenePath);
    }

    private static void CreateLevelButton(Transform parent)
    {
        GameObject buttonObject = new("Play Level 1", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(SceneLoadButton));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(174, 108, 54, 255);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color32(222, 151, 78, 255);
        colors.pressedColor = new Color32(126, 75, 39, 255);
        button.colors = colors;
        buttonObject.GetComponent<SceneLoadButton>().Configure("Level1_TheMines");
        SetRect((RectTransform)buttonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(410f, 78f));

        TextMeshProUGUI label = CreateUiText(buttonObject.transform, "Label", "PLAY LEVEL 1", 27f,
            TextAlignmentOptions.Center, Color.white);
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Canvas CreateScreenCanvas(string name)
    {
        GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateFullScreenImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color32(174, 108, 54, 210);
        outline.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static TextMeshProUGUI CreateUiText(Transform parent, string name, string text, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
