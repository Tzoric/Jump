using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MineLevelBuilder
{
    private const string Level1Path = "Assets/Scenes/Level1_TheMines.unity";
    private const string Level2Path = "Assets/Scenes/Level2_SlidingAscent.unity";
    private const string OverviewPath = "Assets/Scenes/DungeonOverview.unity";
    private const string HeroPrefabPath = "Assets/PreFabs/Hero.prefab";
    private const string Art = "Assets/Art/Generated";
    private const string BackdropPath = Art + "/MineLevel1BronzeBackdrop.png";
    private const string OverviewBackdropPath = Art + "/MineDungeonOverview.png";
    private const string DoorPath = Art + "/MineExitDoor.png";
    private const string PlatformPath = Art + "/MineRockBronzePlatform.png";
    private const string CrystalPath = Art + "/GreenCrystal.png";
    private const string SpikePath = Art + "/BronzeSpike.png";
    private const string HatPath = Art + "/MinerHat.png";
    private const string PickPath = Art + "/MinerPickaxe.png";

    private static readonly Color32 Amber = new(244, 180, 82, 255);
    private static readonly Color32 Bronze = new(184, 113, 58, 255);

    [MenuItem("Jump/Level Tools/Build Mines Levels")]
    public static void Build()
    {
        EnsureFolders();
        CreatePixelAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite platform = ImportSprite(PlatformPath, 16f);
        Sprite crystal = ImportSprite(CrystalPath, 24f);
        Sprite spike = ImportSprite(SpikePath, 24f);
        Sprite hat = ImportSprite(HatPath, 32f);
        Sprite pick = ImportSprite(PickPath, 32f);
        Sprite backdrop = ImportSprite(BackdropPath, 32f);
        Sprite overview = ImportSprite(OverviewBackdropPath, 100f);
        Sprite door = ImportSprite(DoorPath, 256f);

        GameObject heroPrefab = EnsureHeroPrefab(hat, pick);
        BuildLevel1(heroPrefab, platform, backdrop, door);
        BuildLevel2(heroPrefab, platform, backdrop, door, crystal, spike);
        BuildOverview(overview);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(OverviewPath, true),
            new EditorBuildSettingsScene(Level1Path, true),
            new EditorBuildSettingsScene(Level2Path, true)
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(OverviewPath, OpenSceneMode.Single);
        Debug.Log("Built Mines overview, shop, Level 1 Bronze Shaft, and Level 2 Sliding Ascent.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art"); EnsureFolder("Assets/Art", "Generated");
        EnsureFolder("Assets", "PreFabs"); EnsureFolder("Assets", "Scenes");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child);
    }

    private static GameObject EnsureHeroPrefab(Sprite hatSprite, Sprite pickSprite)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        if (prefab == null) throw new FileNotFoundException($"Missing reusable hero prefab at {HeroPrefabPath}");
        GameObject root = PrefabUtility.LoadPrefabContents(HeroPrefabPath);
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                if (root.transform.GetChild(i).name != "FeetPosition") Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            root.transform.localScale = new Vector3(1.875f, 1.875f, 1.875f);
            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            body.gravityScale = 5.4f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            HeroMovement movement = root.GetComponent<HeroMovement>();
            movement.ConfigureMovement(7.5f, 9f, 0.24f);
            PlayerHealth health = root.GetComponent<PlayerHealth>() ?? root.AddComponent<PlayerHealth>();
            health.ConfigureDisplay(null);
            if (root.GetComponent<PlayerWeight>() == null) root.AddComponent<PlayerWeight>();

            Transform hat = CreateAccessory(root.transform, "Miner Hat", hatSprite, new Vector3(0.02f, .31f, -.01f), 12);
            Transform pick = CreateAccessory(root.transform, "Miner Pickaxe", pickSprite, new Vector3(.27f, -.02f, -.02f), 11);
            MinerOutfitVisual outfit = root.GetComponent<MinerOutfitVisual>() ?? root.AddComponent<MinerOutfitVisual>();
            outfit.Configure(root.GetComponent<SpriteRenderer>(), hat, pick);
            PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
    }

    private static Transform CreateAccessory(Transform parent, string name, Sprite sprite, Vector3 position, int order)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.sortingOrder = order;
        return go.transform;
    }

    private static void BuildLevel1(GameObject prefab, Sprite platform, Sprite backdrop, Sprite door)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 1 - Bronze Shaft").transform;
        CreateBackdropAndLight(root, backdrop, new Vector3(0f, 12.5f, 5f));
        GameObject hero = SpawnHero(prefab, scene, new Vector2(3f, -1.2f));
        CreateVerticalCamera(hero.transform);
        Transform route = new GameObject("Beginner Vertical Route").transform; route.SetParent(root);
        CreatePlatform(route, platform, "Start Floor", new Vector2(0f, -2.7f), 12f, 0f);
        Vector2[] positions = { new(-2.4f,-.1f), new(2.4f,2.6f), new(-2.4f,5.3f), new(2.4f,8f), new(-2.4f,10.7f), new(2.4f,13.4f), new(-2.4f,16.1f), new(2.4f,18.8f), new(-2.4f,21.5f), new(2.4f,24.2f), new(-2.4f,26.9f) };
        for (int i = 0; i < positions.Length; i++)
        {
            CreatePlatform(route, platform, $"Bronze Rock Ledge {i + 1:00}", positions[i], i == 10 ? 7f : 5.5f, 0f);
            CreateWaypoint(root, positions[i] + Vector2.up * 1.1f, i + 1);
        }
        CreateDoorWithFoundation(root, platform, door, new Vector2(-3.8f, 29.45f));
        CreateWallsAndPit(root, new Vector2(0f, 12.5f), 22f, 43f);
        CreateHud(hero.GetComponent<PlayerHealth>(), "LEVEL 1  |  BRONZE SHAFT", "A / D OR ARROWS TO MOVE     SPACE TO JUMP     WALK INTO THE EXIT");
        EditorSceneManager.SaveScene(scene, Level1Path);
    }

    private static void BuildLevel2(GameObject prefab, Sprite platform, Sprite backdrop, Sprite door, Sprite crystal, Sprite spike)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 2 - Sliding Ascent").transform;
        CreateBackdropAndLight(root, backdrop, new Vector3(13f, 7f, 5f));
        GameObject hero = SpawnHero(prefab, scene, new Vector2(-7.5f, -2f));
        CreateAngledCamera(hero.transform);
        Transform route = new GameObject("Angled Ascent Route").transform; route.SetParent(root);
        CreatePlatform(route, platform, "Start Shelf", new Vector2(-7f,-3.2f), 7f, 0f);
        const float angle = 22f;
        Vector2[] slopes = { new(-3.5f,-1.5f), new(2f,.75f), new(7.5f,3f), new(13f,5.25f), new(18.5f,7.5f), new(24f,9.75f) };
        for (int i = 0; i < slopes.Length; i++)
        {
            CreatePlatform(route, platform, $"Sliding Bronze Ramp {i + 1:00}", slopes[i], 7.2f, angle);
            CreateWaypoint(root, slopes[i] + new Vector2(0f, 1.6f), i + 1);
        }
        Vector2[] spikePositions = { new(-1.7f,-.65f), new(4f,1.65f), new(9.4f,3.85f), new(20.3f,8.3f) };
        foreach (Vector2 position in spikePositions) CreateRampSpike(root, spike, position, angle);
        Vector2[] gems = { new(-4.2f,.15f), new(1.2f,2.45f), new(6.8f,4.75f), new(12.5f,7f), new(18f,9.25f), new(23.5f,11.5f) };
        foreach (Vector2 position in gems) CreateCrystal(root, crystal, position);
        CreateDoorWithFoundation(root, platform, door, new Vector2(27.3f, 12.1f));
        CreateWallsAndPit(root, new Vector2(10f, 4f), 45f, 30f);
        CreateHud(hero.GetComponent<PlayerHealth>(), "LEVEL 2  |  SLIDING ASCENT", "CLIMB UP AND RIGHT     JUMP THE SPIKES     FALLING SLIDES YOU BACK DOWN     H USES A POTION");
        EditorSceneManager.SaveScene(scene, Level2Path);
    }

    private static GameObject SpawnHero(GameObject prefab, Scene scene, Vector2 position)
    {
        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        hero.name = "Miner Hero"; hero.transform.position = position;
        return hero;
    }

    private static void CreateBackdropAndLight(Transform parent, Sprite sprite, Vector3 position)
    {
        GameObject backdrop = new("Bronze Mine Shaft Backdrop"); backdrop.transform.SetParent(parent); backdrop.transform.position = position;
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = -100;
        GameObject lightGo = new("Global Mine Light"); lightGo.transform.SetParent(parent);
        Light2D light = lightGo.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Global; light.color = new Color32(190,202,225,255); light.intensity = .82f;
    }

    private static Camera CreateCameraBase(Vector3 position)
    {
        GameObject go = new("Main Camera"); go.tag = "MainCamera"; go.transform.position = position;
        Camera camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.6f; camera.backgroundColor = new Color32(8,11,18,255); camera.clearFlags = CameraClearFlags.SolidColor;
        go.AddComponent<AudioListener>(); go.AddComponent<UniversalAdditionalCameraData>(); return camera;
    }

    private static void CreateVerticalCamera(Transform hero) => CreateCameraBase(new Vector3(0f,2f,-10f)).gameObject.AddComponent<VerticalCameraFollow>().Configure(hero, 0f, 2f, 24.2f, 1.35f);
    private static void CreateAngledCamera(Transform hero) => CreateCameraBase(new Vector3(-5f,0f,-10f)).gameObject.AddComponent<BoundedCameraFollow>().Configure(hero, new Vector2(-5f,0f), new Vector2(25f,10f), new Vector2(1.8f,1f));

    private static void CreatePlatform(Transform parent, Sprite sprite, string name, Vector2 position, float width, float angle)
    {
        GameObject go = new(name); go.transform.SetParent(parent); go.transform.position = position; go.transform.rotation = Quaternion.Euler(0f,0f,angle); go.transform.localScale = new Vector3(width / 6f, .9f, 1f); go.layer = LayerMask.NameToLayer("Ground"); go.tag = "Ground";
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 2;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(5.85f, 1.25f);
    }

    private static void CreateDoorWithFoundation(Transform parent, Sprite platform, Sprite doorSprite, Vector2 position)
    {
        CreatePlatform(parent, platform, "Exit Door Foundation (Required)", position + Vector2.down * 2.05f, 6.5f, 0f);
        GameObject door = new("Mine Exit Door"); door.transform.SetParent(parent); door.transform.position = position; door.transform.localScale = Vector3.one * .9f;
        SpriteRenderer renderer = door.AddComponent<SpriteRenderer>(); renderer.sprite = doorSprite; renderer.sortingOrder = 5;
        BoxCollider2D trigger = door.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(2.2f,3.4f); trigger.offset = new Vector2(0f,-.1f);
        door.AddComponent<LevelExitDoor>().Configure("DungeonOverview");
        GameObject glow = new("Exit Lamp Glow"); glow.transform.SetParent(door.transform,false); glow.transform.localPosition = new Vector3(0f,1.7f,0f);
        Light2D light = glow.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Point; light.color = Amber; light.intensity = 1.2f; light.pointLightOuterRadius = 4f;
    }

    private static void CreateRampSpike(Transform parent, Sprite sprite, Vector2 position, float angle)
    {
        GameObject go = new("Ramp Spike - 1 Heart"); go.transform.SetParent(parent); go.transform.position = position; go.transform.rotation = Quaternion.Euler(0f,0f,angle);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 5;
        BoxCollider2D trigger = go.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.2f,.65f); trigger.offset = new Vector2(0f,.12f);
        go.AddComponent<DamageZone>().Configure(1);
    }

    private static void CreateCrystal(Transform parent, Sprite sprite, Vector2 position)
    {
        GameObject go = new("Green Crystal"); go.transform.SetParent(parent); go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 6;
        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = .55f;
        go.AddComponent<GreenCrystalCollectible>();
    }

    private static void CreateWaypoint(Transform parent, Vector2 position, int order)
    {
        GameObject go = new($"Playtest Waypoint {order:00}"); go.transform.SetParent(parent); go.transform.position = position; go.AddComponent<AutomatedPlaytestWaypoint>().Configure(order);
    }

    private static void CreateWallsAndPit(Transform parent, Vector2 center, float width, float height)
    {
        CreateBoundary(parent, "Left Wall", center + Vector2.left * width * .5f, new Vector2(1f,height));
        CreateBoundary(parent, "Right Wall", center + Vector2.right * width * .5f, new Vector2(1f,height));
        GameObject pit = new("Respawn Pit"); pit.transform.SetParent(parent); pit.transform.position = new Vector3(center.x, -8f, 0f);
        BoxCollider2D trigger = pit.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(width,2f); pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateBoundary(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new(name); go.transform.SetParent(parent); go.transform.position = position; go.layer = LayerMask.NameToLayer("Ground"); go.AddComponent<BoxCollider2D>().size = size;
    }

    private static void CreateHud(PlayerHealth health, string titleText, string instructionText)
    {
        Canvas canvas = CreateCanvas("Level HUD");
        TextMeshProUGUI title = Text(canvas.transform,"Level Title",titleText,28,TextAlignmentOptions.Center,Amber); Rect(title.rectTransform,new(.5f,1),new(.5f,1),new(0,-22),new(700,46));
        TextMeshProUGUI hearts = Text(canvas.transform,"Heart Display","HEARTS",24,TextAlignmentOptions.Left,Color.white); Rect(hearts.rectTransform,new(0,1),new(0,1),new(22,-22),new(420,42));
        TextMeshProUGUI lives = Text(canvas.transform,"Lives Display","LIVES",22,TextAlignmentOptions.Right,Color.white); Rect(lives.rectTransform,new(1,1),new(1,1),new(-22,-22),new(250,42));
        health.ConfigureDisplays(hearts,lives);
        TextMeshProUGUI instructions = Text(canvas.transform,"Instructions",instructionText,18,TextAlignmentOptions.Center,Color.white); instructions.textWrappingMode = TextWrappingModes.NoWrap; instructions.outlineWidth = .18f; instructions.outlineColor = Color.black; Rect(instructions.rectTransform,new(.5f,0),new(.5f,0),new(0,22),new(1250,42));
    }

    private static void BuildOverview(Sprite background)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCameraBase(new Vector3(0,0,-10)); camera.orthographicSize = 5f;
        Canvas canvas = CreateCanvas("Dungeon Overview Canvas");
        FullImage(canvas.transform,"Mine Overview Background",background,Color.white);
        FullImage(canvas.transform,"Readability Overlay",null,new Color(.02f,.03f,.06f,.25f));
        TextMeshProUGUI heading = Text(canvas.transform,"Dungeon Heading","DUNGEON 1  —  THE MINES",42,TextAlignmentOptions.Center,Color.white); Rect(heading.rectTransform,new(.5f,1),new(.5f,1),new(0,-35),new(900,65));
        TextMeshProUGUI balance = Text(canvas.transform,"Persistent Balance","",22,TextAlignmentOptions.Center,new Color32(130,255,165,255)); Rect(balance.rectTransform,new(.5f,1),new(.5f,1),new(0,-100),new(1000,42));

        GameObject levels = Panel(canvas.transform,"Mine Level Map",new Color(.025f,.035f,.06f,.74f)); Rect((RectTransform)levels.transform,new(.5f,.5f),new(.5f,.5f),new(0,-20),new(1040,500));
        TextMeshProUGUI mapTitle = Text(levels.transform,"Map Rule","EACH MINESHAFT IS A LEVEL",24,TextAlignmentOptions.Center,Amber); Rect(mapTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-28),new(700,40));
        CreateLevelNode(levels.transform,"Mineshaft 1", "1\nBRONZE SHAFT", "Level1_TheMines", new Vector2(-360,95), true);
        CreateLevelNode(levels.transform,"Mineshaft 2", "2\nSLIDING ASCENT", "Level2_SlidingAscent", new Vector2(-175,-70), true);
        CreateLevelNode(levels.transform,"Mineshaft 3", "3\nLOCKED", "", new Vector2(20,100), false);
        CreateLevelNode(levels.transform,"Mineshaft 4", "4\nLOCKED", "", new Vector2(220,-70), false);
        CreateLevelNode(levels.transform,"Mineshaft 5", "5\nLOCKED", "", new Vector2(400,100), false);

        GameObject shop = Panel(canvas.transform,"Shop Page",new Color(.025f,.035f,.06f,.93f)); Rect((RectTransform)shop.transform,new(.5f,.5f),new(.5f,.5f),new(0,-20),new(860,520));
        TextMeshProUGUI shopTitle = Text(shop.transform,"Shop Title","MINER'S SUPPLY SHOP",34,TextAlignmentOptions.Center,Amber); Rect(shopTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-30),new(700,55));
        TextMeshProUGUI status = Text(shop.transform,"Shop Status","Spend crystals collected in the shafts.",20,TextAlignmentOptions.Center,Color.white); Rect(status.rectTransform,new(.5f,0),new(.5f,0),new(0,28),new(760,45));
        MineShopController controller = canvas.gameObject.AddComponent<MineShopController>(); controller.Configure(levels,shop,balance,status);
        CreateActionButton(shop.transform,"Buy Life",$"EXTRA LIFE  —  {GameProgress.ExtraLifePrice} CRYSTALS",new Vector2(0,110),controller.BuyExtraLife);
        CreateActionButton(shop.transform,"Buy Potion",$"HEALTH POTION  —  {GameProgress.HealthPotionPrice} CRYSTALS",new Vector2(0,10),controller.BuyHealthPotion);
        CreateActionButton(shop.transform,"Buy Heart",$"+1 HEART UPGRADE  —  {GameProgress.HeartUpgradePrice} CRYSTALS",new Vector2(0,-90),controller.BuyHeartUpgrade);
        CreateActionButton(canvas.transform,"Levels Tab","LEVELS",new Vector2(-130,-485),controller.ShowLevels);
        CreateActionButton(canvas.transform,"Shop Tab","SHOP",new Vector2(130,-485),controller.ShowShop);
        controller.ShowLevels();
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene,OverviewPath);
    }

    private static void CreateLevelNode(Transform parent, string name, string label, string scene, Vector2 position, bool enabled)
    {
        GameObject go = new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color = enabled ? new Color32(166,99,48,245) : new Color32(55,61,72,230);
        Button button = go.GetComponent<Button>(); button.interactable = enabled; if (enabled) { SceneLoadButton loader = go.AddComponent<SceneLoadButton>(); loader.Configure(scene); }
        Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(165,125)); TextMeshProUGUI text = Text(go.transform,"Label",label,20,TextAlignmentOptions.Center,Color.white); Stretch(text.rectTransform);
    }

    private static void CreateActionButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color = Bronze; UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, action);
        Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(460,72)); TextMeshProUGUI text = Text(go.transform,"Label",label,21,TextAlignmentOptions.Center,Color.white); Stretch(text.rectTransform);
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f; return canvas;
    }
    private static void FullImage(Transform parent,string name,Sprite sprite,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false); Image image=go.GetComponent<Image>(); image.sprite=sprite; image.color=color; image.raycastTarget=false; Stretch((RectTransform)go.transform); }
    private static GameObject Panel(Transform parent,string name,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Outline)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; Outline o=go.GetComponent<Outline>(); o.effectColor=new Color32(174,108,54,210); o.effectDistance=new Vector2(2,-2); return go; }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float size,TextAlignmentOptions align,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); TextMeshProUGUI t=go.GetComponent<TextMeshProUGUI>(); t.text=value; t.fontSize=size; t.fontStyle=FontStyles.Bold; t.alignment=align; t.color=color; t.raycastTarget=false; return t; }
    private static void Rect(RectTransform rect,Vector2 anchorMin,Vector2 anchorMax,Vector2 position,Vector2 size) { rect.anchorMin=anchorMin; rect.anchorMax=anchorMax; rect.pivot=(anchorMin+anchorMax)*.5f; rect.anchoredPosition=position; rect.sizeDelta=size; }
    private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }

    private static Sprite ImportSprite(string path,float ppu)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter; if(importer==null) throw new InvalidDataException(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.maxTextureSize=2048; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreatePixelAssets()
    {
        WritePlatform(); WriteCrystal(); WriteSpike(); WriteHat(); WritePickaxe();
    }
    private static Texture2D Texture(int w,int h) { Texture2D t=new(w,h,TextureFormat.RGBA32,false); t.filterMode=FilterMode.Point; t.SetPixels32(new Color32[w*h]); return t; }
    private static void Save(Texture2D texture,string path) { texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture); }
    private static void Fill(Texture2D t,int x0,int y0,int x1,int y1,Color32 c) { for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++) t.SetPixel(x,y,c); }
    private static void WritePlatform() { Texture2D t=Texture(96,32); Color32 dark=new(40,34,38,255),rock=new(72,72,82,255),light=new(108,103,105,255),bronze=new(181,105,52,255),shine=new(231,157,77,255); Fill(t,1,3,95,27,dark); int[] seams={0,17,35,54,74,96}; for(int i=0;i<seams.Length-1;i++){int a=seams[i]+2,b=seams[i+1]-1;Fill(t,a,8,b,26,(i%2==0)?rock:light);Fill(t,a,23,b,27,bronze);} Fill(t,0,5,96,9,bronze); Fill(t,3,7,93,9,shine); for(int x=8;x<92;x+=16){Fill(t,x,5,x+3,8,dark);t.SetPixel(x+1,6,shine);} Save(t,PlatformPath); }
    private static void WriteCrystal() { Texture2D t=Texture(32,40); Color32 d=new(11,91,56,255),g=new(32,210,106,255),l=new(145,255,184,255); for(int y=2;y<36;y++){int half=(y<20?y/3:(38-y)/3); for(int x=16-half;x<=16+half;x++) t.SetPixel(x,y,x<16?d:g);} Fill(t,16,10,19,29,l); Save(t,CrystalPath); }
    private static void WriteSpike() { Texture2D t=Texture(40,24); Color32 d=new(63,43,38,255),b=new(179,104,54,255),l=new(239,174,88,255); for(int n=0;n<3;n++){int center=7+n*13; for(int y=2;y<21;y++){int half=y/4; for(int x=center-half;x<=center+half;x++) t.SetPixel(x,y,x<center?d:b);} t.SetPixel(center,3,l);} Fill(t,1,20,39,23,d); Save(t,SpikePath); }
    private static void WriteHat() { Texture2D t=Texture(28,18); Color32 d=new(96,58,25,255),y=new(225,154,52,255),l=new(255,218,105,255); Fill(t,5,4,23,13,y); Fill(t,2,12,26,16,d); Fill(t,6,5,22,8,l); Fill(t,18,8,22,12,new Color32(238,231,174,255)); Save(t,HatPath); }
    private static void WritePickaxe() { Texture2D t=Texture(28,34); Color32 wood=new(130,76,34,255),metal=new(176,184,184,255),shine=new(238,230,194,255); for(int i=0;i<25;i++){t.SetPixel(12+i/5,3+i,wood);t.SetPixel(13+i/5,3+i,wood);} for(int x=3;x<25;x++){int y=27-Mathf.Abs(14-x)/5;t.SetPixel(x,y,metal);t.SetPixel(x,y+1,shine);} Save(t,PickPath); }
}
