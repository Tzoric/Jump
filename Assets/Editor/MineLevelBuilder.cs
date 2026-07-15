using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MineLevelBuilder
{
    private enum ShaftDirection { Vertical, Angled, Horizontal }

    private readonly struct LevelSpec
    {
        public readonly int Number;
        public readonly string SceneName;
        public readonly string DisplayName;
        public readonly ShaftDirection Direction;
        public readonly int Segments;

        public string Path => $"Assets/Scenes/{SceneName}.unity";

        public LevelSpec(int number, string sceneName, string displayName, ShaftDirection direction, int segments)
        {
            Number = number;
            SceneName = sceneName;
            DisplayName = displayName;
            Direction = direction;
            Segments = segments;
        }
    }

    private readonly struct ArtSet
    {
        public readonly Sprite Platform;
        public readonly Sprite GreenGem;
        public readonly Sprite BlueGem;
        public readonly Sprite PurpleGem;
        public readonly Sprite Spike;
        public readonly Sprite Door;
        public readonly Sprite Backdrop;
        public readonly Sprite BronzeKey;
        public readonly Sprite SilverKey;
        public readonly Sprite Chest;

        public ArtSet(Sprite platform, Sprite greenGem, Sprite blueGem, Sprite purpleGem, Sprite spike,
            Sprite door, Sprite backdrop, Sprite bronzeKey, Sprite silverKey, Sprite chest)
        {
            Platform = platform; GreenGem = greenGem; BlueGem = blueGem; PurpleGem = purpleGem;
            Spike = spike; Door = door; Backdrop = backdrop; BronzeKey = bronzeKey;
            SilverKey = silverKey; Chest = chest;
        }
    }

    private static readonly LevelSpec[] Levels =
    {
        new(1, "Level1_TheMines", "BRONZE SHAFT", ShaftDirection.Vertical, 11),
        new(2, "Level2_SlidingAscent", "SLIDING ASCENT", ShaftDirection.Angled, 6),
        new(3, "Level3_ChasmRun", "CHASM RUN", ShaftDirection.Horizontal, 9),
        new(4, "Level4_CopperColumn", "COPPER COLUMN", ShaftDirection.Vertical, 16),
        new(5, "Level5_CrookedIncline", "CROOKED INCLINE", ShaftDirection.Angled, 10),
        new(6, "Level6_BrokenRail", "BROKEN RAIL", ShaftDirection.Horizontal, 12),
        new(7, "Level7_FurnaceRise", "FURNACE RISE", ShaftDirection.Vertical, 21),
        new(8, "Level8_RazorAscent", "RAZOR ASCENT", ShaftDirection.Angled, 14),
        new(9, "Level9_AbyssRun", "ABYSS RUN", ShaftDirection.Horizontal, 16),
        new(10, "Level10_KeyVault", "THE KEY VAULT", ShaftDirection.Vertical, 26),
        new(11, "Level11_TreasureVein", "TREASURE VEIN", ShaftDirection.Angled, 18)
    };

    private const string OverviewPath = "Assets/Scenes/DungeonOverview.unity";
    private const string HeroPrefabPath = "Assets/PreFabs/Hero.prefab";
    private const string Art = "Assets/Art/Generated";
    private const string BackdropPath = Art + "/MineLevel1BronzeBackdrop.png";
    private const string OverviewBackdropPath = Art + "/MineDungeonOverview.png";
    private const string DoorPath = Art + "/MineExitDoor.png";
    private const string PlatformPath = Art + "/MineRockBronzePlatform.png";
    private const string GreenGemPath = Art + "/GreenCrystal.png";
    private const string BlueGemPath = Art + "/BlueCrystalValuable.png";
    private const string PurpleGemPath = Art + "/PurpleCrystalValuable.png";
    private const string SpikePath = Art + "/BronzeSpike.png";
    private const string MinerCharacterPath = Art + "/MinerCharacterV2.png";
    private const string MinerAnimationSheetPath = Art + "/MinerAnimationSheet.png";
    private const string MinerOutfitPath = Art + "/BronzeMinerOutfit.asset";
    private const string PickPath = Art + "/MinerPickaxe.png";
    private const string BronzeKeyPath = Art + "/BronzeKey.png";
    private const string SilverKeyPath = Art + "/SilverKey.png";
    private const string ChestPath = Art + "/BronzeRewardChest.png";

    private static readonly Color32 Amber = new(244, 180, 82, 255);
    private static readonly Color32 Bronze = new(184, 113, 58, 255);

    [MenuItem("Jump/Level Tools/Build Bronze Mines Levels 1-11")]
    public static void Build()
    {
        EnsureFolders();
        CreatePixelAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite miner = ImportSprite(MinerCharacterPath, 1024f);
        Sprite animationSheet = ImportSprite(MinerAnimationSheetPath, 220f, true);
        Sprite pick = ImportSprite(PickPath, 32f);
        CharacterOutfitDefinition minerOutfit = EnsureOutfitDefinition(animationSheet, pick);
        GameObject heroPrefab = EnsureHeroPrefab(miner, minerOutfit);
        ArtSet art = new(
            ImportSprite(PlatformPath, 16f), ImportSprite(GreenGemPath, 24f),
            ImportSprite(BlueGemPath, 24f), ImportSprite(PurpleGemPath, 24f),
            ImportSprite(SpikePath, 24f), ImportSprite(DoorPath, 256f),
            ImportSprite(BackdropPath, 32f), ImportSprite(BronzeKeyPath, 24f),
            ImportSprite(SilverKeyPath, 24f), ImportSprite(ChestPath, 24f));
        Sprite overview = ImportSprite(OverviewBackdropPath, 100f);

        foreach (LevelSpec level in Levels)
        {
            if (level.Number == 1) BuildLevel1(level, heroPrefab, art);
            else if (level.Number == 2) BuildLevel2(level, heroPrefab, art);
            else BuildGeneratedLevel(level, heroPrefab, art);
        }
        BuildOverview(overview);

        var buildScenes = new List<EditorBuildSettingsScene> { new(OverviewPath, true) };
        foreach (LevelSpec level in Levels) buildScenes.Add(new EditorBuildSettingsScene(level.Path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(OverviewPath, OpenSceneMode.Single);
        Debug.Log("Built Dungeon 1 — Bronze Mines: overview, shop, and Levels 1-11 with alternating shaft directions.");
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

    private static CharacterOutfitDefinition EnsureOutfitDefinition(Sprite animationSheet, Sprite pickSprite)
    {
        CharacterOutfitDefinition outfit = AssetDatabase.LoadAssetAtPath<CharacterOutfitDefinition>(MinerOutfitPath);
        if (outfit == null)
        {
            outfit = ScriptableObject.CreateInstance<CharacterOutfitDefinition>();
            AssetDatabase.CreateAsset(outfit, MinerOutfitPath);
        }
        outfit.Configure("Main Hero", "bronze_miner", animationSheet, pickSprite);
        EditorUtility.SetDirty(outfit);
        return outfit;
    }

    private static GameObject EnsureHeroPrefab(Sprite minerSprite, CharacterOutfitDefinition minerOutfit)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        if (prefab == null) throw new FileNotFoundException($"Missing reusable hero prefab at {HeroPrefabPath}");
        GameObject root = PrefabUtility.LoadPrefabContents(HeroPrefabPath);
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                if (root.transform.GetChild(i).name != "FeetPosition") Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            root.transform.localScale = Vector3.one * 1.875f;
            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            body.gravityScale = 5.4f; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            root.GetComponent<HeroMovement>().ConfigureMovement(7.5f, 12f, .24f);
            PlayerHealth health = root.GetComponent<PlayerHealth>() ?? root.AddComponent<PlayerHealth>();
            health.ConfigureBaseHealth(GameProgress.BaseHearts);
            health.ConfigureDisplay(null);
            if (root.GetComponent<PlayerWeight>() == null) root.AddComponent<PlayerWeight>();
            if (root.GetComponent<MineRunInventory>() == null) root.AddComponent<MineRunInventory>();

            SpriteRenderer directionSource = root.GetComponent<SpriteRenderer>();
            directionSource.enabled = false;
            Animator animator = root.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            Transform miner = CreateAccessory(root.transform, "Integrated Miner Character", minerSprite,
                new Vector3(0f, .02f, -.01f), 10);
            miner.localScale = Vector3.one * .95f;
            Transform pick = CreatePickaxeRig(root.transform, minerOutfit.HandTool);
            (root.GetComponent<MinerOutfitVisual>() ?? root.AddComponent<MinerOutfitVisual>())
                .Configure(directionSource, miner.GetComponent<SpriteRenderer>(), pick, minerOutfit);
            PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
    }

    private static Transform CreateAccessory(Transform parent, string name, Sprite sprite, Vector3 position, int order)
    {
        GameObject go = new(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
        return go.transform;
    }

    private static Transform CreatePickaxeRig(Transform parent, Sprite sprite)
    {
        GameObject pivot = new("Pick Hand Pivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(.31f, -.02f, -.02f);
        Transform pick = CreateAccessory(pivot.transform, "Small Hand Pickaxe", sprite,
            new Vector3(.04f, .01f, 0f), 11);
        pick.localScale = Vector3.one * .48f;
        return pivot.transform;
    }

    private static void BuildLevel1(LevelSpec level, GameObject prefab, ArtSet art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 1 - Bronze Shaft").transform;
        CreateBackdropAndLight(root, art.Backdrop, new Vector3(0,12.5f,5), new Vector2(1,1));
        GameObject hero = SpawnHero(prefab, scene, new Vector2(3,-1.2f));
        CreateVerticalCamera(hero.transform, 24.2f);
        Transform route = new GameObject("Vertical Bronze Route").transform; route.SetParent(root);
        CreatePlatform(route, art.Platform, "Start Floor", new Vector2(0,-2.7f), 12, 0);
        Vector2[] positions = { new(-2.4f,-.1f),new(2.4f,2.6f),new(-2.4f,5.3f),new(2.4f,8),new(-2.4f,10.7f),new(2.4f,13.4f),new(-2.4f,16.1f),new(2.4f,18.8f),new(-2.4f,21.5f),new(2.4f,24.2f),new(-2.4f,26.9f) };
        for (int i=0;i<positions.Length;i++)
        {
            CreatePlatform(route, art.Platform, $"Bronze Ledge {i+1:00}", positions[i], 4.8f, 0);
            CreateWaypoint(root, new Vector2(positions[i].x>0?.25f:-.25f, positions[i].y+1.5f), i+1);
        }
        CreateBronzeChallenge(root, art, level.Number, new Vector2(6.5f,14.6f), new Vector2(-6.3f,24.8f));
        CreateDoor(root, art, level.Number, new Vector2(-3.8f,29.45f));
        CreateWallsAndAbyss(root, new Vector2(0,12.5f),22,43,false);
        CreateHud(hero, level, "CLIMB TO THE EXIT     FIND THE BRONZE KEY AND CHEST");
        EditorSceneManager.SaveScene(scene, level.Path);
    }

    private static void BuildLevel2(LevelSpec level, GameObject prefab, ArtSet art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 2 - Sliding Ascent").transform;
        CreateBackdropAndLight(root, art.Backdrop, new Vector3(11,0,5), new Vector2(1.9f,1.3f), -14f);

        Vector2 resetPosition = new(-10f, 7.05f);
        GameObject hero = SpawnHero(prefab, scene, resetPosition);
        CreateAngledCamera(hero.transform, -7f, 31f, -4f, 5.5f);

        Transform upperRoute = new GameObject("Upper Horizontal Gap Route").transform;
        upperRoute.SetParent(root);
        Vector2[] upperPlatforms =
        {
            new(-9f,5.65f), new(-2.6f,5.85f), new(4f,5.55f), new(10.7f,5.95f),
            new(17.5f,5.6f), new(24.5f,5.9f), new(31.5f,6.05f)
        };
        float[] upperWidths = { 5.5f, 4.2f, 4.1f, 3.9f, 3.8f, 3.7f, 5.7f };
        for (int i = 0; i < upperPlatforms.Length; i++)
        {
            CreatePlatform(upperRoute, art.Platform, $"Upper Gap Platform {i + 1:00}",
                upperPlatforms[i], upperWidths[i], 0f);
            CreateWaypoint(root, upperPlatforms[i] + Vector2.up * 1.45f, i + 1);
            if (i > 0 && i < upperPlatforms.Length - 1)
                CreateGem(root, art.GreenGem, upperPlatforms[i] + Vector2.up * 1.8f, 1);
        }

        Transform lowerRamp = new GameObject("Lower Steep Spike Reset Ramp").transform;
        lowerRamp.SetParent(root);
        const float rampAngle = -16f;
        float rampSlope = Mathf.Tan(rampAngle * Mathf.Deg2Rad);
        for (int i = 0; i < 7; i++)
        {
            float x = -9f + i * 7.2f;
            float y = 2.25f + (x + 9f) * rampSlope;
            Vector2 rampPosition = new(x, y);
            CreatePlatform(lowerRamp, art.Platform, $"Lower Reset Ramp {i + 1:00}",
                rampPosition, 7.7f, rampAngle);
            if (i > 0 && i < 6)
                CreateSpike(lowerRamp, art.Spike, rampPosition + Vector2.up * .72f, rampAngle);
        }

        float retryX = 35f;
        float retryY = 2.25f + (retryX + 9f) * rampSlope;
        GameObject retry = new("Ramp Bottom Retry — No Life Lost");
        retry.transform.SetParent(root);
        retry.transform.position = new Vector3(retryX, retryY - .8f, 0f);
        BoxCollider2D retryTrigger = retry.AddComponent<BoxCollider2D>();
        retryTrigger.isTrigger = true;
        retryTrigger.size = new Vector2(5.5f, 5f);
        retry.AddComponent<LevelRetryZone>().Configure(resetPosition,
            "The ramp returned you to the start. Take the upper platforms again.");

        CreateBronzeChallenge(root, art, level.Number, new Vector2(15.3f,8.45f), new Vector2(26.7f,8.8f));
        CreateDoor(root, art, level.Number, new Vector2(33.4f,8.55f));
        CreateBoundary(root, "Level 2 Left Mine Wall", new Vector2(-15f,-1f), new Vector2(1f,34f));
        CreateBoundary(root, "Level 2 Right Mine Wall", new Vector2(39f,-1f), new Vector2(1f,34f));
        CreateHud(hero, level,
            "CROSS THE UPPER GAPS     FALLS LEAD TO THE SPIKE RAMP     SLIDE TO THE BOTTOM TO RETRY");
        EditorSceneManager.SaveScene(scene,level.Path);
    }

    private static void BuildGeneratedLevel(LevelSpec level, GameObject prefab, ArtSet art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject($"Level {level.Number} - {level.DisplayName}").transform;
        GameObject hero;
        Vector2 keyPosition;
        Vector2 chestPosition;

        if (level.Direction == ShaftDirection.Vertical)
        {
            float topY = -.1f + (level.Segments-1)*2.65f;
            CreateBackdropAndLight(root,art.Backdrop,new Vector3(0,topY*.5f,5),new Vector2(1,Mathf.Max(1,(topY+12)/58f)));
            hero=SpawnHero(prefab,scene,new Vector2(3,-1.2f));
            CreateVerticalCamera(hero.transform,Mathf.Max(2,topY-2));
            BuildVerticalRoute(root,level,art,out keyPosition,out chestPosition);
            CreateWallsAndAbyss(root,new Vector2(0,topY*.5f),24,topY+25,false);
        }
        else if (level.Direction == ShaftDirection.Angled)
        {
            float maxX=-3.5f+(level.Segments-1)*5f;
            float maxY=-1.5f+(level.Segments-1)*2.1f;
            CreateBackdropAndLight(root,art.Backdrop,new Vector3(maxX*.5f,maxY*.5f,5),new Vector2(Mathf.Max(1,(maxX+20)/36f),Mathf.Max(1,(maxY+15)/58f)),12f);
            hero=SpawnHero(prefab,scene,new Vector2(-7.5f,-2));
            CreateAngledCamera(hero.transform,-5,maxX,0,Mathf.Max(0,maxY-1));
            BuildAngledRoute(root,level,art,out keyPosition,out chestPosition);
            CreateWallsAndAbyss(root,new Vector2(maxX*.5f,maxY*.5f),maxX+28,maxY+30,false);
        }
        else
        {
            float maxX=-1.5f+(level.Segments-1)*6.5f;
            CreateBackdropAndLight(root,art.Backdrop,new Vector3(maxX*.5f,0,5),new Vector2(Mathf.Max(1,(maxX+24)/36f),1));
            hero=SpawnHero(prefab,scene,new Vector2(-8,-1.1f));
            CreateHorizontalCamera(hero.transform,-6,Mathf.Max(-6,maxX-1));
            BuildHorizontalRoute(root,level,art,out keyPosition,out chestPosition);
            CreateWallsAndAbyss(root,new Vector2(maxX*.5f,0),maxX+30,30,true);
        }

        CreateBronzeChallenge(root,art,level.Number,keyPosition,chestPosition);
        if(level.Number==10) CreateSilverKeyChallenge(root,art,keyPosition+new Vector2(1.8f,7.2f));
        if(level.Number==11) CreateLevel11Treasure(root,level,art);
        CreateHud(hero,level,level.Number==11?"SILVER KEY VAULT     THE RAREST GEMS ARE OFF THE SAFE ROUTE":"DANGER INCREASES     FIND THE BRONZE KEY AND CHEST");
        EditorSceneManager.SaveScene(scene,level.Path);
    }

    private static void BuildVerticalRoute(Transform root,LevelSpec level,ArtSet art,out Vector2 keyPosition,out Vector2 chestPosition)
    {
        Transform route=new GameObject("Vertical Shaft Route").transform; route.SetParent(root);
        CreatePlatform(route,art.Platform,"Start Floor",new Vector2(0,-2.7f),12,0);
        float width=Mathf.Max(3.7f,5.2f-level.Number*.12f);
        float topY=0;
        for(int i=0;i<level.Segments;i++)
        {
            float y=-.1f+i*2.65f; topY=y;
            float x=i%2==0?-2.6f:2.6f;
            CreatePlatform(route,art.Platform,$"Vertical Ledge {i+1:00}",new Vector2(x,y),width,0);
            CreateWaypoint(root,new Vector2(x>0?.4f:-.4f,y+1.5f),i+1);
            if(i>1 && i%(level.Number>=8?2:3)==0) CreateSpike(root,art.Spike,new Vector2(x+(i%4==0?1.1f:-1.1f),y+.8f),0);
            if(level.Number<11 && i>2 && i%4==1) CreateGem(root,art.GreenGem,new Vector2(x,y+2.1f),1);
        }
        CreateDoor(root,art,level.Number,new Vector2(level.Segments%2==0?3.5f:-3.5f,topY+2.55f));
        keyPosition=new Vector2(7,topY*.58f);
        chestPosition=new Vector2(-7,topY*.82f);
    }

    private static void BuildAngledRoute(Transform root,LevelSpec level,ArtSet art,out Vector2 keyPosition,out Vector2 chestPosition)
    {
        Transform route=new GameObject("Angled Shaft Route").transform; route.SetParent(root);
        CreatePlatform(route,art.Platform,"Start Shelf",new Vector2(-8,-3.2f),8,0);
        Vector2 last=Vector2.zero;
        for(int i=0;i<level.Segments;i++)
        {
            last=new Vector2(-3.5f+i*5f,-1.5f+i*2.1f);
            float width=Mathf.Max(5.5f,7.2f-level.Number*.1f);
            CreatePlatform(route,art.Platform,$"Angled Ramp {i+1:00}",last,width,22);
            CreateWaypoint(root,last+new Vector2(0,1.6f),i+1);
            if(i>1 && i%(level.Number>=8?2:3)==0) CreateSpike(root,art.Spike,last+new Vector2(1.1f,.9f),22);
            if(level.Number<11 && i>1 && i%3==1) CreateGem(root,art.GreenGem,last+new Vector2(0,2.3f),1);
        }
        CreateDoor(root,art,level.Number,last+new Vector2(3.2f,2.7f));
        keyPosition=new Vector2(last.x*.48f,last.y*.48f+4.3f);
        chestPosition=last+new Vector2(-1.5f,4.2f);
    }

    private static void BuildHorizontalRoute(Transform root,LevelSpec level,ArtSet art,out Vector2 keyPosition,out Vector2 chestPosition)
    {
        Transform route=new GameObject("Horizontal Bottomless Route").transform; route.SetParent(root);
        Vector2 previous=new(-8,-2.7f);
        CreatePlatform(route,art.Platform,"Start Shelf",previous,8,0);
        for(int i=0;i<level.Segments;i++)
        {
            float y=i%3==0?-1.2f:i%3==1?.35f:-.45f;
            Vector2 position=new(-1.5f+i*6.5f,y);
            float width=Mathf.Max(3.4f,4.9f-level.Number*.1f);
            CreatePlatform(route,art.Platform,$"Pit Ledge {i+1:00}",position,width,0);
            CreateWaypoint(root,position+Vector2.up*1.5f,i+1);
            CreateBottomlessPit(root,$"Bottomless Pit {i+1:00}",(previous.x+position.x)*.5f,Mathf.Abs(position.x-previous.x)-3.2f);
            if(i>0 && i%(level.Number>=8?2:3)==0) CreateSpike(root,art.Spike,position+new Vector2(.7f,.8f),0);
            if(i>1 && i%3==1) CreateGem(root,art.GreenGem,position+new Vector2(0,2.1f),1);
            previous=position;
        }
        CreateDoor(root,art,level.Number,previous+new Vector2(3.6f,2.6f));
        keyPosition=previous*.42f+new Vector2(0,5.2f);
        chestPosition=previous+new Vector2(-2,4.6f);
    }

    private static GameObject SpawnHero(GameObject prefab,Scene scene,Vector2 position)
    {
        GameObject hero=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); hero.name="Miner Hero"; hero.transform.position=position; return hero;
    }

    private static void CreateBackdropAndLight(Transform parent,Sprite sprite,Vector3 position,Vector2 scale,float angle=0f)
    {
        GameObject backdrop=new("Bronze Mine Backdrop"); backdrop.transform.SetParent(parent); backdrop.transform.position=position; backdrop.transform.rotation=Quaternion.Euler(0,0,angle); backdrop.transform.localScale=new Vector3(scale.x,scale.y,1);
        SpriteRenderer renderer=backdrop.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=-100;
        GameObject lightGo=new("Global Bronze Mine Light"); lightGo.transform.SetParent(parent); Light2D light=lightGo.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Global; light.color=new Color32(190,202,225,255); light.intensity=.82f;
    }

    private static Camera CreateCameraBase(Vector3 position)
    {
        GameObject go=new("Main Camera"); go.tag="MainCamera"; go.transform.position=position; Camera camera=go.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=5.6f; camera.backgroundColor=new Color32(8,11,18,255); camera.clearFlags=CameraClearFlags.SolidColor; go.AddComponent<AudioListener>(); go.AddComponent<UniversalAdditionalCameraData>(); return camera;
    }
    private static void CreateVerticalCamera(Transform hero,float maxY)=>CreateCameraBase(new Vector3(0,2,-10)).gameObject.AddComponent<VerticalCameraFollow>().Configure(hero,0,2,maxY,1.35f);
    private static void CreateAngledCamera(Transform hero,float minX,float maxX,float minY,float maxY)=>CreateCameraBase(new Vector3(minX,minY,-10)).gameObject.AddComponent<BoundedCameraFollow>().Configure(hero,new Vector2(minX,minY),new Vector2(maxX,maxY),new Vector2(1.8f,1));
    private static void CreateHorizontalCamera(Transform hero,float minX,float maxX)=>CreateCameraBase(new Vector3(minX,0,-10)).gameObject.AddComponent<BoundedCameraFollow>().Configure(hero,new Vector2(minX,0),new Vector2(maxX,0),new Vector2(2.2f,0));

    private static GameObject CreatePlatform(Transform parent,Sprite sprite,string name,Vector2 position,float width,float angle)
    {
        GameObject go=new(name); go.transform.SetParent(parent); go.transform.position=position; go.transform.rotation=Quaternion.Euler(0,0,angle); go.transform.localScale=new Vector3(width/6f,.58f,1); go.layer=LayerMask.NameToLayer("Ground"); go.tag="Ground";
        SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=2; BoxCollider2D collider=go.AddComponent<BoxCollider2D>(); collider.size=new Vector2(5.85f,.82f); return go;
    }

    private static void CreateDoor(Transform parent,ArtSet art,int levelNumber,Vector2 position)
    {
        CreatePlatform(parent,art.Platform,"Exit Door Foundation (Required)",position+Vector2.down*2.05f,6.5f,0);
        GameObject door=new("Mine Exit Door"); door.transform.SetParent(parent); door.transform.position=position; door.transform.localScale=Vector3.one*.9f; SpriteRenderer renderer=door.AddComponent<SpriteRenderer>(); renderer.sprite=art.Door; renderer.sortingOrder=5; BoxCollider2D trigger=door.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(2.2f,3.4f); trigger.offset=new Vector2(0,-.1f); door.AddComponent<LevelExitDoor>().Configure("DungeonOverview",levelNumber);
        GameObject glow=new("Exit Lamp Glow"); glow.transform.SetParent(door.transform,false); glow.transform.localPosition=new Vector3(0,1.7f,0); Light2D light=glow.AddComponent<Light2D>(); light.lightType=Light2D.LightType.Point; light.color=Amber; light.intensity=1.2f; light.pointLightOuterRadius=4;
    }

    private static void CreateSpike(Transform parent,Sprite sprite,Vector2 position,float angle)
    {
        GameObject go=new("Bronze Spike - 1 Heart"); go.transform.SetParent(parent); go.transform.position=position; go.transform.rotation=Quaternion.Euler(0,0,angle); SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=5; BoxCollider2D trigger=go.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(1.2f,.65f); trigger.offset=new Vector2(0,.12f); go.AddComponent<DamageZone>().Configure(1);
    }

    private static void CreateGem(Transform parent,Sprite sprite,Vector2 position,int value)
    {
        GameObject go=new(value==1?"Green Gem (1)":value==5?"Blue Gem (5)":"Purple Gem (20)"); go.transform.SetParent(parent); go.transform.position=position; SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=6; CircleCollider2D trigger=go.AddComponent<CircleCollider2D>(); trigger.isTrigger=true; trigger.radius=.55f; go.AddComponent<GreenCrystalCollectible>().Configure(value);
    }

    private static void CreateBronzeChallenge(Transform parent,ArtSet art,int levelNumber,Vector2 keyPosition,Vector2 chestPosition)
    {
        Transform challenge=new GameObject("Optional Bronze Key Chest Challenge").transform; challenge.SetParent(parent);
        CreatePlatform(challenge,art.Platform,"Bronze Key Perch",keyPosition+Vector2.down*1.25f,3.2f,0);
        GameObject key=new("Hidden Bronze Key"); key.transform.SetParent(challenge); key.transform.position=keyPosition; SpriteRenderer keyRenderer=key.AddComponent<SpriteRenderer>(); keyRenderer.sprite=art.BronzeKey; keyRenderer.sortingOrder=8; key.AddComponent<CircleCollider2D>().isTrigger=true; key.AddComponent<BronzeKeyCollectible>().Configure(levelNumber);
        CreatePlatform(challenge,art.Platform,"Reward Chest Perch",chestPosition+Vector2.down*1.25f,4.2f,0);
        GameObject chest=new("Bronze Key Reward Chest"); chest.transform.SetParent(challenge); chest.transform.position=chestPosition; SpriteRenderer chestRenderer=chest.AddComponent<SpriteRenderer>(); chestRenderer.sprite=art.Chest; chestRenderer.sortingOrder=8; chest.AddComponent<BoxCollider2D>().isTrigger=true; chest.AddComponent<RewardChest>().Configure(levelNumber);
    }

    private static void CreateSilverKeyChallenge(Transform parent,ArtSet art,Vector2 position)
    {
        Transform secret=new GameObject("Hard Hidden Silver Key Route").transform; secret.SetParent(parent);
        CreatePlatform(secret,art.Platform,"Secret Step A",position+new Vector2(-2.8f,-3.6f),2.6f,0);
        CreatePlatform(secret,art.Platform,"Secret Step B",position+new Vector2(-1.1f,-1.8f),2.3f,0);
        CreatePlatform(secret,art.Platform,"Silver Key Perch",position+Vector2.down*1.1f,2.4f,0);
        CreateSpike(secret,art.Spike,position+new Vector2(-1.1f,-1),0);
        GameObject key=new("Hidden Silver Key — Unlocks Level 11"); key.transform.SetParent(secret); key.transform.position=position; SpriteRenderer renderer=key.AddComponent<SpriteRenderer>(); renderer.sprite=art.SilverKey; renderer.sortingOrder=9; key.AddComponent<CircleCollider2D>().isTrigger=true; key.AddComponent<SilverKeyCollectible>();
    }

    private static void CreateLevel11Treasure(Transform parent,LevelSpec level,ArtSet art)
    {
        Transform treasure=new GameObject("Extreme Level 11 Gem Challenges").transform; treasure.SetParent(parent);
        for(int i=0;i<24;i++)
        {
            Vector2 p=new(-1+i*3.45f,-.2f+i*1.45f+(i%2==0?2.6f:0));
            CreateGem(treasure,art.GreenGem,p,1);
            if(i%4==2) CreateSpike(treasure,art.Spike,p+Vector2.down*.9f,22);
        }
        int[] blueIndices={3,6,9,12,15};
        foreach(int i in blueIndices)
        {
            Vector2 shelf=new(-3.5f+i*5f,-1.5f+i*2.1f+4.3f);
            CreatePlatform(treasure,art.Platform,$"Difficult Blue Gem Perch {i}",shelf+Vector2.down*1.2f,2.5f,0);
            CreateGem(treasure,art.BlueGem,shelf,5);
            CreateSpike(treasure,art.Spike,shelf+new Vector2(-1,-.75f),0);
        }
        Vector2 purple=new(-3.5f+15*5f,-1.5f+15*2.1f+8.2f);
        CreatePlatform(treasure,art.Platform,"Extreme Purple Gem Perch",purple+Vector2.down*1.2f,2.2f,0);
        CreatePlatform(treasure,art.Platform,"Purple Approach Step",purple+new Vector2(-3.3f,-3.4f),2.1f,0);
        CreateSpike(treasure,art.Spike,purple+new Vector2(-.9f,-.75f),0);
        CreateSpike(treasure,art.Spike,purple+new Vector2(.9f,-.75f),0);
        CreateGem(treasure,art.PurpleGem,purple,20);
    }

    private static void CreateWaypoint(Transform parent,Vector2 position,int order)
    {
        GameObject go=new($"Playtest Waypoint {order:00}"); go.transform.SetParent(parent); go.transform.position=position; go.AddComponent<AutomatedPlaytestWaypoint>().Configure(order);
    }

    private static void CreateBottomlessPit(Transform parent,string name,float centerX,float width)
    {
        GameObject pit=new(name); pit.transform.SetParent(parent); pit.transform.position=new Vector3(centerX,-7,0); BoxCollider2D trigger=pit.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(Mathf.Max(1,width),12); pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateWallsAndAbyss(Transform parent,Vector2 center,float width,float height,bool bottomless)
    {
        CreateBoundary(parent,"Left Mine Wall",center+Vector2.left*width*.5f,new Vector2(1,height)); CreateBoundary(parent,"Right Mine Wall",center+Vector2.right*width*.5f,new Vector2(1,height)); GameObject pit=new(bottomless?"Bottomless Abyss Death Zone":"Respawn Pit"); pit.transform.SetParent(parent); pit.transform.position=new Vector3(center.x,-11,0); BoxCollider2D trigger=pit.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(width,5); pit.AddComponent<DamageZone>().Configure(99);
    }
    private static void CreateBoundary(Transform parent,string name,Vector2 position,Vector2 size) { GameObject go=new(name); go.transform.SetParent(parent); go.transform.position=position; go.layer=LayerMask.NameToLayer("Ground"); go.AddComponent<BoxCollider2D>().size=size; }

    private static void CreateHud(GameObject hero,LevelSpec level,string instruction)
    {
        Canvas canvas=CreateCanvas("Level HUD"); TextMeshProUGUI title=Text(canvas.transform,"Level Title",$"LEVEL {level.Number}  |  {level.DisplayName}",27,TextAlignmentOptions.Center,Amber); Rect(title.rectTransform,new(.5f,1),new(.5f,1),new(0,-22),new(760,46)); TextMeshProUGUI hearts=Text(canvas.transform,"Heart Display","HEARTS",23,TextAlignmentOptions.Left,Color.white); Rect(hearts.rectTransform,new(0,1),new(0,1),new(22,-22),new(440,42)); TextMeshProUGUI lives=Text(canvas.transform,"Lives Display","LIVES",21,TextAlignmentOptions.Right,Color.white); Rect(lives.rectTransform,new(1,1),new(1,1),new(-22,-22),new(250,42)); hero.GetComponent<PlayerHealth>().ConfigureDisplays(hearts,lives);
        TextMeshProUGUI status=Text(canvas.transform,"Run Status","FIND THE HIDDEN BRONZE KEY",18,TextAlignmentOptions.Left,new Color32(255,208,112,255)); Rect(status.rectTransform,new(0,1),new(0,1),new(22,-66),new(620,38)); hero.GetComponent<MineRunInventory>().Configure(level.Number,status);
        TextMeshProUGUI instructions=Text(canvas.transform,"Instructions",instruction,17,TextAlignmentOptions.Center,Color.white); instructions.textWrappingMode=TextWrappingModes.NoWrap; instructions.outlineWidth=.18f; instructions.outlineColor=Color.black; Rect(instructions.rectTransform,new(.5f,0),new(.5f,0),new(0,22),new(1450,42));
    }

    private static void BuildOverview(Sprite background)
    {
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); Camera camera=CreateCameraBase(new Vector3(0,0,-10)); camera.orthographicSize=5; Canvas canvas=CreateCanvas("Bronze Mines Overview Canvas"); FullImage(canvas.transform,"Bronze Mines Overview Background",background,Color.white); FullImage(canvas.transform,"Readability Overlay",null,new Color(.02f,.03f,.06f,.34f)); TextMeshProUGUI heading=Text(canvas.transform,"Dungeon Heading","DUNGEON 1  —  BRONZE MINES",40,TextAlignmentOptions.Center,Color.white); Rect(heading.rectTransform,new(.5f,1),new(.5f,1),new(0,-28),new(1000,60)); TextMeshProUGUI balance=Text(canvas.transform,"Persistent Balance","",20,TextAlignmentOptions.Center,new Color32(130,255,165,255)); Rect(balance.rectTransform,new(.5f,1),new(.5f,1),new(0,-88),new(1200,40));
        GameObject levels=Panel(canvas.transform,"Eleven Tunnel Level Map",new Color(.025f,.035f,.06f,.78f)); Rect((RectTransform)levels.transform,new(.5f,.5f),new(.5f,.5f),new(0,-15),new(1540,620)); TextMeshProUGUI mapTitle=Text(levels.transform,"Map Rule","11 TUNNELS  •  VERTICAL → ANGLED → HORIZONTAL",23,TextAlignmentOptions.Center,Amber); Rect(mapTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-25),new(900,38));
        for(int i=0;i<Levels.Length;i++)
        {
            int row=i<6?0:1; int column=row==0?i:i-6; float spacing=row==0?235:270; float count=row==0?6:5; float x=(column-(count-1)*.5f)*spacing; float y=row==0?115:-115; CreateLevelNode(levels.transform,Levels[i],new Vector2(x,y));
        }
        TextMeshProUGUI gate=Text(levels.transform,"Level 11 Gate","LEVEL 11 REQUIRES LEVEL 10 + THE HIDDEN SILVER KEY",18,TextAlignmentOptions.Center,new Color32(210,220,235,255)); Rect(gate.rectTransform,new(.5f,0),new(.5f,0),new(0,22),new(900,35));
        GameObject shop=Panel(canvas.transform,"Shop Page",new Color(.025f,.035f,.06f,.94f)); Rect((RectTransform)shop.transform,new(.5f,.5f),new(.5f,.5f),new(0,-15),new(860,520)); TextMeshProUGUI shopTitle=Text(shop.transform,"Shop Title","MINER'S SUPPLY SHOP",34,TextAlignmentOptions.Center,Amber); Rect(shopTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-30),new(700,55)); TextMeshProUGUI status=Text(shop.transform,"Shop Status","Potions restore one heart.",20,TextAlignmentOptions.Center,Color.white); Rect(status.rectTransform,new(.5f,0),new(.5f,0),new(0,28),new(760,45)); MineShopController controller=canvas.gameObject.AddComponent<MineShopController>(); controller.Configure(levels,shop,balance,status); CreateActionButton(shop.transform,"Buy Life",$"EXTRA LIFE  —  {GameProgress.ExtraLifePrice} GREEN GEMS",new Vector2(0,110),controller.BuyExtraLife); CreateActionButton(shop.transform,"Buy Potion",$"HEALTH POTION (+1 HEART)  —  {GameProgress.HealthPotionPrice} GREEN GEMS",new Vector2(0,10),controller.BuyHealthPotion); CreateActionButton(shop.transform,"Buy Heart",$"+1 HEART CAPACITY  —  {GameProgress.HeartUpgradePrice} GREEN GEMS",new Vector2(0,-90),controller.BuyHeartUpgrade); CreateActionButton(canvas.transform,"Levels Tab","LEVELS",new Vector2(-130,-495),controller.ShowLevels); CreateActionButton(canvas.transform,"Shop Tab","SHOP",new Vector2(130,-495),controller.ShowShop); controller.ShowLevels(); new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); EditorSceneManager.SaveScene(scene,OverviewPath);
    }

    private static void CreateLevelNode(Transform parent,LevelSpec level,Vector2 position)
    {
        GameObject go=new($"Mineshaft {level.Number}",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=new Color32(166,99,48,245); Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(205,104)); TextMeshProUGUI label=Text(go.transform,"Label",$"{level.Number}\n{level.DisplayName}",17,TextAlignmentOptions.Center,Color.white); Stretch(label.rectTransform); go.AddComponent<MineLevelSelectButton>().Configure(level.Number,level.SceneName,level.DisplayName,label);
    }

    private static void CreateActionButton(Transform parent,string name,string label,Vector2 position,UnityEngine.Events.UnityAction action)
    {
        GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=Bronze; UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick,action); Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(500,72)); TextMeshProUGUI text=Text(go.transform,"Label",label,20,TextAlignmentOptions.Center,Color.white); Stretch(text.rectTransform);
    }

    private static Canvas CreateCanvas(string name) { GameObject go=new(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); Canvas canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; CanvasScaler scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; return canvas; }
    private static void FullImage(Transform parent,string name,Sprite sprite,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false); Image image=go.GetComponent<Image>(); image.sprite=sprite; image.color=color; image.raycastTarget=false; Stretch((RectTransform)go.transform); }
    private static GameObject Panel(Transform parent,string name,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Outline)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=color; Outline outline=go.GetComponent<Outline>(); outline.effectColor=new Color32(174,108,54,210); outline.effectDistance=new Vector2(2,-2); return go; }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,float size,TextAlignmentOptions alignment,Color color) { GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); TextMeshProUGUI text=go.GetComponent<TextMeshProUGUI>(); text.text=value; text.fontSize=size; text.fontStyle=FontStyles.Bold; text.alignment=alignment; text.color=color; text.raycastTarget=false; return text; }
    private static void Rect(RectTransform rect,Vector2 min,Vector2 max,Vector2 position,Vector2 size) { rect.anchorMin=min; rect.anchorMax=max; rect.pivot=(min+max)*.5f; rect.anchoredPosition=position; rect.sizeDelta=size; }
    private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }

    private static Sprite ImportSprite(string path,float ppu,bool readable=false)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter; if(importer==null) throw new InvalidDataException(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.isReadable=readable; importer.maxTextureSize=2048; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreatePixelAssets() { WritePlatform(); WriteGem(GreenGemPath,new Color32(11,91,56,255),new Color32(32,210,106,255),new Color32(145,255,184,255)); WriteGem(BlueGemPath,new Color32(18,63,139,255),new Color32(46,148,255,255),new Color32(176,230,255,255)); WriteGem(PurpleGemPath,new Color32(74,27,112,255),new Color32(177,72,234,255),new Color32(245,184,255,255)); WriteSpike(); WritePickaxe(); WriteKey(BronzeKeyPath,new Color32(106,58,24,255),new Color32(213,126,54,255),new Color32(255,193,94,255)); WriteKey(SilverKeyPath,new Color32(72,83,101,255),new Color32(172,190,211,255),new Color32(242,250,255,255)); WriteChest(); }
    private static Texture2D Texture(int w,int h) { Texture2D texture=new(w,h,TextureFormat.RGBA32,false); texture.filterMode=FilterMode.Point; texture.SetPixels32(new Color32[w*h]); return texture; }
    private static void Save(Texture2D texture,string path) { texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture); }
    private static void Fill(Texture2D texture,int x0,int y0,int x1,int y1,Color32 color) { for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++) texture.SetPixel(x,y,color); }
    private static void WritePlatform()
    {
        Texture2D t=Texture(96,32);
        Color32 deep=new(28,27,34,255),dark=new(44,45,55,255),rock=new(67,68,79,255),light=new(91,89,96,255);
        Color32 bronze=new(178,101,49,255),shine=new(235,158,75,255);

        for(int x=1;x<95;x++)
        {
            int bottom=3+((x*7+x/9)%4==0?1:0)+((x+3)%29==0?2:0);
            int top=28-((x*5+x/7)%5==0?2:0)-((x+11)%23==0?2:0);
            for(int y=bottom;y<top;y++)
            {
                int cell=(x/13+y/7)%4;
                t.SetPixel(x,y,cell==0?dark:cell==1?rock:cell==2?deep:light);
            }
        }

        int[] seams={15,31,49,68,84};
        foreach(int seam in seams)
        {
            for(int y=5;y<27;y++)
            {
                int x=seam+(y/5)%2;
                if(t.GetPixel(x,y).a>.1f && y%6!=0) t.SetPixel(x,y,deep);
            }
        }
        for(int x=5;x<92;x++)
        {
            int y=9+((x/9)%2);
            if(x%17>3 && t.GetPixel(x,y).a>.1f) t.SetPixel(x,y,deep);
            if((x*11)%23==0 && t.GetPixel(x,24).a>.1f) t.SetPixel(x,24,light);
        }

        DrawVein(t,new Vector2Int(4,17),new Vector2Int(17,12),bronze,shine);
        DrawVein(t,new Vector2Int(17,12),new Vector2Int(30,18),bronze,shine);
        DrawVein(t,new Vector2Int(30,18),new Vector2Int(44,10),bronze,shine);
        DrawVein(t,new Vector2Int(44,10),new Vector2Int(58,15),bronze,shine);
        DrawVein(t,new Vector2Int(58,15),new Vector2Int(73,8),bronze,shine);
        DrawVein(t,new Vector2Int(73,8),new Vector2Int(92,15),bronze,shine);
        DrawVein(t,new Vector2Int(30,18),new Vector2Int(24,26),bronze,shine);
        DrawVein(t,new Vector2Int(44,10),new Vector2Int(50,24),bronze,shine);
        DrawVein(t,new Vector2Int(73,8),new Vector2Int(80,23),bronze,shine);
        Save(t,PlatformPath);
    }

    private static void DrawVein(Texture2D texture,Vector2Int from,Vector2Int to,Color32 bronze,Color32 shine)
    {
        int steps=Mathf.Max(Mathf.Abs(to.x-from.x),Mathf.Abs(to.y-from.y));
        for(int i=0;i<=steps;i++)
        {
            int x=Mathf.RoundToInt(Mathf.Lerp(from.x,to.x,i/(float)steps));
            int y=Mathf.RoundToInt(Mathf.Lerp(from.y,to.y,i/(float)steps));
            if(x<0||x>=texture.width||y<0||y>=texture.height||texture.GetPixel(x,y).a<=.1f) continue;
            texture.SetPixel(x,y,bronze);
            if(i%4==0 && y+1<texture.height && texture.GetPixel(x,y+1).a>.1f) texture.SetPixel(x,y+1,shine);
        }
    }
    private static void WriteGem(string path,Color32 dark,Color32 color,Color32 light) { Texture2D t=Texture(32,40); for(int y=2;y<36;y++){int half=y<20?y/3:(38-y)/3;for(int x=16-half;x<=16+half;x++)t.SetPixel(x,y,x<16?dark:color);} Fill(t,16,10,19,29,light); Save(t,path); }
    private static void WriteSpike() { Texture2D t=Texture(40,24); Color32 d=new(63,43,38,255),b=new(179,104,54,255),l=new(239,174,88,255); for(int n=0;n<3;n++){int center=7+n*13;for(int y=2;y<21;y++){int half=y/4;for(int x=center-half;x<=center+half;x++)t.SetPixel(x,y,x<center?d:b);}t.SetPixel(center,3,l);}Fill(t,1,20,39,23,d);Save(t,SpikePath); }
    private static void WritePickaxe() { Texture2D t=Texture(28,34); Color32 wood=new(130,76,34,255),metal=new(176,184,184,255),shine=new(238,230,194,255);for(int i=0;i<25;i++){t.SetPixel(12+i/5,3+i,wood);t.SetPixel(13+i/5,3+i,wood);}for(int x=3;x<25;x++){int y=27-Mathf.Abs(14-x)/5;t.SetPixel(x,y,metal);t.SetPixel(x,y+1,shine);}Save(t,PickPath); }
    private static void WriteKey(string path,Color32 dark,Color32 color,Color32 light) { Texture2D t=Texture(36,20); for(int y=5;y<16;y++)for(int x=3;x<14;x++){float dx=x-8,dy=y-10;if(dx*dx+dy*dy<=25&&dx*dx+dy*dy>=9)t.SetPixel(x,y,x<8?dark:color);}Fill(t,12,9,31,13,color);Fill(t,25,5,29,10,color);Fill(t,29,5,33,13,dark);Fill(t,13,10,28,11,light);Save(t,path); }
    private static void WriteChest() { Texture2D t=Texture(44,32); Color32 dark=new(67,38,25,255),wood=new(132,70,35,255),bronze=new(202,121,52,255),light=new(255,184,83,255);Fill(t,3,4,41,26,dark);Fill(t,6,7,38,23,wood);Fill(t,3,14,41,18,bronze);Fill(t,19,12,25,22,light);Fill(t,7,7,37,10,bronze);Save(t,ChestPath); }
}
