using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MineLevelBuilder
{
    private enum ShaftDirection { Vertical, Angled, Horizontal, Mixed }

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
        public readonly Sprite DiagonalBackdrop;
        public readonly Sprite BronzeKey;
        public readonly Sprite SilverKey;
        public readonly Sprite Chest;
        public readonly Sprite OpenChest;

        public ArtSet(Sprite platform, Sprite greenGem, Sprite blueGem, Sprite purpleGem, Sprite spike,
            Sprite door, Sprite backdrop, Sprite diagonalBackdrop, Sprite bronzeKey, Sprite silverKey, Sprite chest,
            Sprite openChest)
        {
            Platform = platform; GreenGem = greenGem; BlueGem = blueGem; PurpleGem = purpleGem;
            Spike = spike; Door = door; Backdrop = backdrop; DiagonalBackdrop = diagonalBackdrop; BronzeKey = bronzeKey;
            SilverKey = silverKey; Chest = chest; OpenChest = openChest;
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
        new(11, "Level11_TreasureVein", "TREASURE VEIN", ShaftDirection.Angled, 18),
        new(12, "Level12_DeepworksGauntlet", "DEEPWORKS GAUNTLET", ShaftDirection.Mixed, 60)
    };

    private const string OverviewPath = "Assets/Scenes/DungeonOverview.unity";
    private const string GameOverPath = "Assets/Scenes/GameOver.unity";
    private const string HeroPrefabPath = "Assets/PreFabs/Hero.prefab";
    private const string Art = "Assets/Art/Generated";
    private const string BackdropPath = Art + "/MineLevel1BronzeBackdrop.png";
    private const string DiagonalBackdropPath = Art + "/MineDiagonalBronzeBackdrop.png";
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
    private const string SlideMaterialPath = Art + "/BronzeRampSlide.physicsMaterial2D";
    private const string HeroMaterialPath = Art + "/HeroNoStick.physicsMaterial2D";
    private const string ParachutePath = Art + "/MinerParachute.png";
    private const string BronzeKeyPath = Art + "/BronzeKey.png";
    private const string SilverKeyPath = Art + "/SilverKey.png";
    private const string ChestPath = Art + "/BronzeRewardChest.png";
    private const string OpenChestPath = Art + "/BronzeRewardChestOpen.png";

    private static readonly Color32 Amber = new(244, 180, 82, 255);
    private static readonly Color32 Bronze = new(184, 113, 58, 255);

    [MenuItem("Jump/Level Tools/Build Bronze Mines Levels 1-12")]
    public static void Build()
    {
        EnsureFolders();
        CreatePixelAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite miner = ImportSprite(MinerCharacterPath, 1024f);
        Sprite animationSheet = ImportSprite(MinerAnimationSheetPath, 220f, true);
        Sprite parachute = ImportSprite(ParachutePath, 32f);
        CharacterOutfitDefinition minerOutfit = EnsureOutfitDefinition(animationSheet, null);
        PhysicsMaterial2D slideMaterial = EnsureSlideMaterial();
        PhysicsMaterial2D heroMaterial = EnsureHeroMaterial();
        GameObject heroPrefab = EnsureHeroPrefab(miner, minerOutfit, parachute, heroMaterial);
        ArtSet art = new(
            ImportSprite(PlatformPath, 16f), ImportSprite(GreenGemPath, 24f),
            ImportSprite(BlueGemPath, 24f), ImportSprite(PurpleGemPath, 24f),
            ImportSprite(SpikePath, 24f), ImportSprite(DoorPath, 256f),
            ImportSprite(BackdropPath, 32f), ImportSprite(DiagonalBackdropPath, 32f), ImportSprite(BronzeKeyPath, 24f),
            ImportSprite(SilverKeyPath, 24f), ImportSprite(ChestPath, 24f), ImportSprite(OpenChestPath, 24f));
        Sprite overview = ImportSprite(OverviewBackdropPath, 100f);

        foreach (LevelSpec level in Levels)
        {
            if (level.Number == 1) BuildLevel1(level, heroPrefab, art);
            else if (level.Number == 2) BuildLevel2(level, heroPrefab, art, slideMaterial);
            else if (level.Number == 12) BuildLevel12(level, heroPrefab, art);
            else BuildGeneratedLevel(level, heroPrefab, art);
        }
        BuildOverview(overview);
        BuildGameOver(overview);

        var buildScenes = new List<EditorBuildSettingsScene>
        {
            new(OverviewPath, true),
            new(GameOverPath, true)
        };
        foreach (LevelSpec level in Levels) buildScenes.Add(new EditorBuildSettingsScene(level.Path, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(OverviewPath, OpenSceneMode.Single);
        Debug.Log("Built Dungeon 1 — Bronze Mines: overview, shop, and Levels 1-12 including the mixed Deepworks Gauntlet.");
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

    private static PhysicsMaterial2D EnsureSlideMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(SlideMaterialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("Bronze Ramp Slide");
            AssetDatabase.CreateAsset(material, SlideMaterialPath);
        }
        material.friction = 0f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static PhysicsMaterial2D EnsureHeroMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(HeroMaterialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("Hero No Stick");
            AssetDatabase.CreateAsset(material, HeroMaterialPath);
        }
        material.friction = 0f;
        material.bounciness = 0f;
        material.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        material.bounceCombine = PhysicsMaterialCombine2D.Minimum;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject EnsureHeroPrefab(Sprite minerSprite, CharacterOutfitDefinition minerOutfit,
        Sprite parachuteSprite, PhysicsMaterial2D heroMaterial)
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
            foreach (Collider2D heroCollider in root.GetComponents<Collider2D>())
                if (!heroCollider.isTrigger) heroCollider.sharedMaterial = heroMaterial;
            root.GetComponent<HeroMovement>().ConfigureMovement(7.5f, 12f, .24f, .08f,
                9f, 14.75f, .26f);
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
            Transform parachute = CreateAccessory(root.transform, "Level 12 Parachute Canopy", parachuteSprite,
                new Vector3(0f, 1.15f, -.025f), 9);
            parachute.localScale = Vector3.one * 1.08f;
            SpriteRenderer parachuteRenderer = parachute.GetComponent<SpriteRenderer>();
            parachuteRenderer.enabled = false;
            (root.GetComponent<ParachuteDescentController>() ?? root.AddComponent<ParachuteDescentController>())
                .Configure(parachuteRenderer);
            (root.GetComponent<MinerOutfitVisual>() ?? root.AddComponent<MinerOutfitVisual>())
                .Configure(directionSource, miner.GetComponent<SpriteRenderer>(), null, minerOutfit);
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

    private static void BuildLevel1(LevelSpec level, GameObject prefab, ArtSet art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 1 - Bronze Shaft").transform;
        CreateBackdropAndLight(root, art.Backdrop, new Vector3(0,15.5f,5), new Vector2(1,1.25f));
        GameObject hero = SpawnHero(prefab, scene, new Vector2(3,-1.2f));
        CreateVerticalCamera(hero.transform, 31.5f);
        Transform route = new GameObject("Vertical Bronze Route").transform; route.SetParent(root);
        CreatePlatform(route, art.Platform, "Start Floor", new Vector2(3f,-2.7f), 5.2f, 0);
        Vector2[] positions =
        {
            new(-2.8f,.5f), new(2.8f,3.75f), new(-2.8f,7f), new(2.8f,10.25f),
            new(-2.8f,13.5f), new(2.8f,16.75f), new(-2.8f,20f), new(2.8f,23.25f),
            new(-2.8f,26.5f), new(2.8f,29.75f), new(-2.8f,33f)
        };
        for (int i=0;i<positions.Length;i++)
        {
            CreatePlatform(route, art.Platform, $"Bronze Ledge {i+1:00}", positions[i], 4.4f, 0);
            CreateWaypoint(root, new Vector2(positions[i].x, positions[i].y+1.4f), i+1);
        }
        CreateBronzeChallenge(root, art, level.Number, new Vector2(6.5f,17.8f), new Vector2(-6.3f,29.7f));
        CreateDoor(root, art, level.Number, new Vector2(-8.1f,35.55f));
        CreateWallsAndAbyss(root, new Vector2(0,15.5f),24,54,false);
        CreateHud(hero, level, "CLIMB TO THE EXIT     FIND THE BRONZE KEY AND CHEST");
        EditorSceneManager.SaveScene(scene, level.Path);
    }

    private static void BuildLevel2(LevelSpec level, GameObject prefab, ArtSet art, PhysicsMaterial2D slideMaterial)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 2 - Sliding Ascent").transform;
        CreateBackdropAndLight(root, art.DiagonalBackdrop, new Vector3(14,8,5), new Vector2(1.45f,1.2f));

        Vector2 resetPosition = new(-10f, 1.35f);
        GameObject hero = SpawnHero(prefab, scene, resetPosition);
        CreateAngledCamera(hero.transform, -8f, 38f, -4f, 18.5f);

        Transform upperRoute = new GameObject("Upper Rising Gap Route").transform;
        upperRoute.SetParent(root);
        Vector2[] upperPlatforms =
        {
            new(-10f,0f), new(-3.2f,2.5f), new(3.8f,5.4f), new(9.9f,7.7f),
            new(17.4f,10.8f), new(24f,13.2f), new(31.8f,16.5f), new(39f,19.1f)
        };
        float[] upperWidths = { 7.2f, 4.2f, 5.4f, 3.6f, 5f, 4f, 5.8f, 6.4f };
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
        const float rampAngle = 18f;
        float rampSlope = Mathf.Tan(rampAngle * Mathf.Deg2Rad);
        Vector2 rampNormal = new(-Mathf.Sin(rampAngle * Mathf.Deg2Rad), Mathf.Cos(rampAngle * Mathf.Deg2Rad));
        for (int i = 0; i < 8; i++)
        {
            float x = -10f + i * 7.2f;
            float y = -3.65f + (x + 10f) * rampSlope;
            Vector2 rampPosition = new(x, y);
            GameObject ramp = CreatePlatform(lowerRamp, art.Platform, $"Lower Reset Ramp {i + 1:00}",
                rampPosition, 7.7f, rampAngle);
            ramp.GetComponent<BoxCollider2D>().sharedMaterial = slideMaterial;
            if (i > 0 && i < 7)
                CreateSpike(lowerRamp, art.Spike, rampPosition + rampNormal * .72f, 0f);
        }

        float retryX = -15f;
        float retryY = -3.65f + (retryX + 10f) * rampSlope;
        GameObject retry = new("Ramp Bottom Retry — No Life Lost");
        retry.transform.SetParent(root);
        retry.transform.position = new Vector3(retryX, retryY - .8f, 0f);
        BoxCollider2D retryTrigger = retry.AddComponent<BoxCollider2D>();
        retryTrigger.isTrigger = true;
        retryTrigger.size = new Vector2(5.5f, 5f);
        retry.AddComponent<LevelRetryZone>().Configure(resetPosition,
            "The ramp returned you to the start. Take the upper platforms again.");

        CreateBronzeChallenge(root, art, level.Number, new Vector2(13.8f,12.4f), new Vector2(28f,19.7f));
        CreateDoor(root, art, level.Number, new Vector2(41.2f,21.65f));
        CreateBoundary(root, "Level 2 Left Mine Wall", new Vector2(-18f,8f), new Vector2(1f,50f));
        CreateBoundary(root, "Level 2 Right Mine Wall", new Vector2(46f,8f), new Vector2(1f,50f));
        CreateHud(hero, level,
            "CROSS THE UPPER GAPS     FALLS LEAD TO THE SPIKE RAMP     SLIDE TO THE BOTTOM TO RETRY");
        EditorSceneManager.SaveScene(scene,level.Path);
    }

    private static void BuildLevel12(LevelSpec level, GameObject prefab, ArtSet art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("Level 12 - Deepworks Mixed Gauntlet").transform;
        CreateGlobalMineLight(root);

        Vector2 cursor = new(-8f, -2.7f);
        var routePoints = new List<Vector2> { cursor };
        Transform route = new GameObject("Level 12 Seeded Mixed Route").transform;
        route.SetParent(root);
        CreatePlatform(route, art.Platform, "Deepworks Start Shelf", cursor, 8f, 0f);

        GameObject hero = SpawnHero(prefab, scene, cursor + Vector2.up * 1.5f);
        int waypointOrder = 0;
        MineRouteSectionType[] sectionOrder = CreateLevel12SectionOrder(12012);
        for (int index = 0; index < sectionOrder.Length; index++)
        {
            Vector2 entry = cursor;
            Transform section = new GameObject($"Mixed Section {index + 1:00} - {sectionOrder[index]}").transform;
            section.SetParent(route);

            float pathLength;
            switch (sectionOrder[index])
            {
                case MineRouteSectionType.VerticalUp:
                    cursor = BuildLevel12VerticalUp(section, art, cursor, ref waypointOrder, routePoints,
                        index, out pathLength);
                    break;
                case MineRouteSectionType.AngledUp:
                    cursor = BuildLevel12AngledUp(section, art, cursor, ref waypointOrder, routePoints,
                        index, index > 0 && sectionOrder[index - 1] == MineRouteSectionType.VerticalDown,
                        out pathLength);
                    break;
                case MineRouteSectionType.Horizontal:
                    cursor = BuildLevel12Horizontal(section, root, art, cursor, ref waypointOrder,
                        routePoints, index, out pathLength);
                    break;
                case MineRouteSectionType.VerticalDown:
                    cursor = BuildLevel12Descent(section, root, art, cursor, ref waypointOrder,
                        routePoints, index, out pathLength);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            section.gameObject.AddComponent<MineRouteSection>().Configure(index + 1, sectionOrder[index],
                entry, cursor, pathLength);
            CreateLevel12SectionBackdrop(section, art, sectionOrder[index], entry, cursor, index + 1);
        }

        Vector2 keyPosition = routePoints[Mathf.Clamp(routePoints.Count / 3, 0, routePoints.Count - 1)] +
                              new Vector2(10f, 3.8f);
        Vector2 chestPosition = routePoints[Mathf.Clamp(routePoints.Count * 4 / 5, 0, routePoints.Count - 1)] +
                                new Vector2(-10f, 4f);
        CreateBronzeChallenge(root, art, level.Number, keyPosition, chestPosition);
        CreateDoor(root, art, level.Number, cursor + new Vector2(5.2f, 2.55f));

        float minX = routePoints.Min(point => point.x) - 4f;
        float maxX = routePoints.Max(point => point.x) + 4f;
        float minY = routePoints.Min(point => point.y) - 4f;
        float maxY = routePoints.Max(point => point.y) + 4f;
        // Begin at the miner rather than the final route endpoint. MixedRouteCameraFollow
        // also snaps on Start so hand-authored variants cannot sweep across the map.
        Camera camera = CreateCameraBase(new Vector3(
            hero.transform.position.x + 1.8f, hero.transform.position.y + 1.2f, -10f));
        camera.gameObject.AddComponent<MixedRouteCameraFollow>().Configure(hero.transform,
            new Vector2(minX, minY), new Vector2(maxX, maxY));

        CreateHud(hero, level,
            "MIXED DEEPWORKS GAUNTLET     HOLD UP / W OR INTERACT FOR PARACHUTE     JUMP STAYS AVAILABLE AT LANDINGS");
        EditorSceneManager.SaveScene(scene, level.Path);
    }

    private static MineRouteSectionType[] CreateLevel12SectionOrder(int seed)
    {
        var result = new List<MineRouteSectionType>(12);
        var random = new System.Random(seed);
        MineRouteSectionType previous = MineRouteSectionType.VerticalDown;
        for (int round = 0; round < 3; round++)
        {
            var group = new List<MineRouteSectionType>
            {
                MineRouteSectionType.VerticalUp,
                MineRouteSectionType.AngledUp,
                MineRouteSectionType.Horizontal,
                MineRouteSectionType.VerticalDown
            };
            for (int index = group.Count - 1; index > 0; index--)
            {
                int swap = random.Next(index + 1);
                (group[index], group[swap]) = (group[swap], group[index]);
            }
            if ((round == 0 && group[0] == MineRouteSectionType.VerticalDown) || group[0] == previous)
            {
                int swap = group.FindIndex(type => type != MineRouteSectionType.VerticalDown && type != previous);
                (group[0], group[swap]) = (group[swap], group[0]);
            }
            result.AddRange(group);
            previous = group[^1];
        }
        return result.ToArray();
    }

    private static Vector2 BuildLevel12VerticalUp(Transform section, ArtSet art, Vector2 entry,
        ref int waypointOrder, ICollection<Vector2> routePoints, int sectionIndex, out float pathLength)
    {
        Vector2 previous = entry;
        pathLength = 0f;
        for (int step = 0; step < 7; step++)
        {
            Vector2 position = new(entry.x + (step % 2 == 0 ? -3f : 3f), entry.y + (step + 1) * 3.25f);
            CreatePlatform(section, art.Platform, $"Mixed Vertical Ledge {sectionIndex + 1:00}-{step + 1:00}",
                position, 4f, 0f);
            CreateWaypoint(section, position + Vector2.up * 1.45f, ++waypointOrder);
            if (step >= 2 && step % 2 == 0)
                CreateSpike(section, art.Spike, position + new Vector2(step % 4 == 0 ? 1f : -1f, .82f), 0f);
            if (step % 2 == 1) CreateGem(section, art.GreenGem, position + Vector2.up * 2f, 1);
            pathLength += Vector2.Distance(previous, position);
            previous = position;
            routePoints.Add(position);
        }
        return previous;
    }

    private static Vector2 BuildLevel12AngledUp(Transform section, ArtSet art, Vector2 entry,
        ref int waypointOrder, ICollection<Vector2> routePoints, int sectionIndex,
        bool followsDescent, out float pathLength)
    {
        Vector2 previous = entry;
        pathLength = 0f;
        // A diagonal ledge beginning directly above the wide landing shelf forms a low
        // ceiling: the miner hits its underside before gaining enough height. Move the
        // entire outgoing run just beyond the shelf edge after a descent, preserving the
        // same slope and jump spacing while leaving an unmistakable lower-right breakout.
        float breakoutOffset = followsDescent ? 1.9f : 0f;
        for (int step = 0; step < 7; step++)
        {
            Vector2 position = entry + new Vector2((step + 1) * 6.1f + breakoutOffset,
                (step + 1) * 2.7f);
            CreatePlatform(section, art.Platform, $"Mixed Diagonal Ledge {sectionIndex + 1:00}-{step + 1:00}",
                position, 4.5f - step * .08f, 18f);
            // The rotated platform's physical standing center is slightly lower than the
            // old marker. Keep the route marker inside the settled-ground tolerance so a
            // focused traversal recognizes the landing instead of jumping in place.
            CreateWaypoint(section, position + Vector2.up * 1.4f, ++waypointOrder);
            if (step > 1 && step % 2 == 1)
                CreateSpike(section, art.Spike, position + new Vector2(1f, .9f), 18f);
            if (step % 2 == 0) CreateGem(section, art.GreenGem, position + Vector2.up * 2.1f, 1);
            pathLength += Vector2.Distance(previous, position);
            previous = position;
            routePoints.Add(position);
        }
        return previous;
    }

    private static Vector2 BuildLevel12Horizontal(Transform section, Transform levelRoot, ArtSet art,
        Vector2 entry, ref int waypointOrder, ICollection<Vector2> routePoints, int sectionIndex,
        out float pathLength)
    {
        Vector2 previousPosition = entry;
        GameObject previousPlatform = CreatePlatform(section, art.Platform,
            $"Mixed Horizontal Hub {sectionIndex + 1:00}", entry, 6.5f, 0f);
        pathLength = 0f;
        for (int step = 0; step < 7; step++)
        {
            float yOffset = step % 3 == 0 ? .35f : step % 3 == 1 ? 1.25f : -.15f;
            Vector2 position = entry + new Vector2((step + 1) * 6.8f, yOffset);
            float width = 4.25f - (step % 3) * .3f;
            GameObject platform = CreatePlatform(section, art.Platform,
                $"Mixed Pit Ledge {sectionIndex + 1:00}-{step + 1:00}", position, width, 0f);
            CreateWaypoint(section, position + Vector2.up * 1.45f, ++waypointOrder);
            Physics2D.SyncTransforms();
            CreateLocalBottomlessPit(levelRoot, $"Level 12 Local Bottomless Pit {sectionIndex + 1:00}-{step + 1:00}",
                previousPlatform.GetComponent<BoxCollider2D>().bounds,
                platform.GetComponent<BoxCollider2D>().bounds);
            // Keep the final ledge clear so it is a readable, safe launch into a following
            // descent section instead of hiding a spike directly under the parachute cue.
            if (step > 1 && step < 6 && step % 2 == 0)
                CreateSpike(section, art.Spike, position + new Vector2(.8f, .82f), 0f);
            if (step % 2 == 1) CreateGem(section, art.GreenGem, position + Vector2.up * 2f, 1);
            pathLength += Vector2.Distance(previousPosition, position);
            previousPosition = position;
            previousPlatform = platform;
            routePoints.Add(position);
        }
        return previousPosition;
    }

    private static Vector2 BuildLevel12Descent(Transform section, Transform levelRoot, ArtSet art,
        Vector2 entry, ref int waypointOrder, ICollection<Vector2> routePoints, int sectionIndex,
        out float pathLength)
    {
        const float depth = 30f;
        CreatePlatform(section, art.Platform, $"Parachute Launch Shelf {sectionIndex + 1:00}", entry, 7f, 0f);

        GameObject zone = new($"Parachute Descent Zone {sectionIndex + 1:00}");
        zone.transform.SetParent(section);
        // End the chute trigger above both the landing stance and the first
        // outgoing ledge so Jump is restored before the next section begins.
        zone.transform.position = entry + Vector2.down * 11.5f;
        BoxCollider2D zoneCollider = zone.AddComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector2(18f, 24f);
        zone.AddComponent<ParachuteDescentZone>().Configure(depth);

        bool needsRightExit = sectionIndex < 11;
        CreateVisibleShaftWall(section, art.Platform, $"Descent Left Rock Wall {sectionIndex + 1:00}",
            entry + new Vector2(-9.5f, -15f), 32f);
        CreateVisibleShaftWall(section, art.Platform, $"Descent Right Rock Wall {sectionIndex + 1:00}",
            needsRightExit ? entry + new Vector2(9.5f, -9f) : entry + new Vector2(9.5f, -15f),
            needsRightExit ? 20f : 32f);

        float[] hazardDepths = { 6f, 11.5f, 17f, 22.5f };
        for (int hazardIndex = 0; hazardIndex < hazardDepths.Length; hazardIndex++)
        {
            // The first hazard is always on the left, directing the player off the open
            // right edge of the launch shelf and away from the preceding section's pit.
            // The last hazard in a transition shaft moves to the solid left
            // wall, leaving the lower-right breakout visually and physically clear.
            bool left = hazardIndex % 2 == 0 ||
                        (needsRightExit && hazardIndex == hazardDepths.Length - 1);
            Vector2 hazardPosition = entry + new Vector2(left ? -7.8f : 7.8f, -hazardDepths[hazardIndex]);
            CreateHiddenDescentSpike(section, art.Spike,
                $"Hidden Descent Spike {sectionIndex + 1:00}-{hazardIndex + 1:00}", hazardPosition,
                left ? -90f : 90f);
            Vector2 safePass = entry + new Vector2(left ? 3.8f : -3.8f, -hazardDepths[hazardIndex] - .4f);
            CreateWaypoint(section, safePass, ++waypointOrder, AutomatedWaypointMode.AirbornePass, 1.5f, true);
            routePoints.Add(safePass);
        }

        CreateOscillatingDescentHazard(section, art.Spike,
            $"Moving Spike Bar {sectionIndex + 1:00}-A", entry + new Vector2(0f, -9f), 4.6f, sectionIndex * .17f);
        CreateOscillatingDescentHazard(section, art.Spike,
            $"Moving Spike Bar {sectionIndex + 1:00}-B", entry + new Vector2(0f, -20f), 4.2f, .45f + sectionIndex * .11f);

        Vector2 bottom = entry + Vector2.down * depth;
        CreatePlatform(section, art.Platform, $"Parachute Landing Shelf {sectionIndex + 1:00}", bottom, 12f, 0f);
        CreateWaypoint(section, bottom + Vector2.up * 1.45f, ++waypointOrder);
        CreateGem(section, art.GreenGem, bottom + new Vector2(-3f, 2f), 1);
        CreateGem(section, art.GreenGem, bottom + new Vector2(3f, 2f), 1);
        routePoints.Add(bottom);
        pathLength = depth;
        return bottom;
    }

    private static void CreateLevel12SectionBackdrop(Transform parent, ArtSet art,
        MineRouteSectionType type, Vector2 entry, Vector2 exit, int order)
    {
        Vector2 center = (entry + exit) * .5f;
        Vector2 span = new(Mathf.Abs(exit.x - entry.x) + 22f, Mathf.Abs(exit.y - entry.y) + 18f);
        Sprite sprite = type == MineRouteSectionType.AngledUp ? art.DiagonalBackdrop : art.Backdrop;
        Vector2 nativeSize = type == MineRouteSectionType.AngledUp ? new Vector2(48f, 32f) : new Vector2(32f, 48f);
        bool descent = type == MineRouteSectionType.VerticalDown;
        SpriteRenderer renderer = CreateBackdrop(parent, sprite,
            descent
                ? $"Section {order:00} Dedicated Descent Shaft Backdrop"
                : $"Section {order:00} {type} Backdrop",
            new Vector3(center.x, center.y, 5f),
            new Vector2(Mathf.Max(.8f, span.x / nativeSize.x), Mathf.Max(.8f, span.y / nativeSize.y)),
            0f, descent ? -90 : -100);
        if (descent)
            renderer.color = new Color32(105, 122, 145, 255);
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
            float topY = .5f + (level.Segments-1)*3.25f;
            CreateBackdropAndLight(root,art.Backdrop,new Vector3(0,topY*.5f,5),new Vector2(1,Mathf.Max(1,(topY+12)/58f)));
            hero=SpawnHero(prefab,scene,new Vector2(3,-1.2f));
            CreateVerticalCamera(hero.transform,Mathf.Max(2,topY-2));
            BuildVerticalRoute(root,level,art,out keyPosition,out chestPosition);
            CreateWallsAndAbyss(root,new Vector2(0,topY*.5f),24,topY+25,false);
        }
        else if (level.Direction == ShaftDirection.Angled)
        {
            float maxX=-3.2f+(level.Segments-1)*6.5f;
            float maxY=-.1f+(level.Segments-1)*2.9f;
            CreateBackdropAndLight(root,art.DiagonalBackdrop,new Vector3(maxX*.5f,maxY*.5f,5),
                new Vector2(Mathf.Max(1,(maxX+20)/48f),Mathf.Max(1,(maxY+15)/32f)));
            hero=SpawnHero(prefab,scene,new Vector2(-7.5f,-2));
            CreateAngledCamera(hero.transform,-5,maxX,0,Mathf.Max(0,maxY-1));
            BuildAngledRoute(root,level,art,out keyPosition,out chestPosition);
            CreateWallsAndAbyss(root,new Vector2(maxX*.5f,maxY*.5f),maxX+28,maxY+30,false);
        }
        else
        {
            float maxX=-1.3f+(level.Segments-1)*6.8f;
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
        CreatePlatform(route,art.Platform,"Start Floor",new Vector2(3f,-2.7f),5.2f,0);
        float width=Mathf.Max(3.7f,5.2f-level.Number*.12f);
        const float standardRouteSpikeOffset = 1.1f;
        const float standardSafeLandingOffset = .4f;
        const float level10RouteSpikeOffset = 1.25f;
        const float level10SafeLandingOffset = .5f;
        const float level10LandingRadius = .3f;
        float topY=0;
        for(int i=0;i<level.Segments;i++)
        {
            float y=.5f+i*3.25f; topY=y;
            float routeHalfWidth=level.Number==10?3.1f:2.8f;
            float x=i%2==0?-routeHalfWidth:routeHalfWidth;
            CreatePlatform(route,art.Platform,$"Vertical Ledge {i+1:00}",new Vector2(x,y),width,0);
            bool hasRouteSpike=i>1 && i%(level.Number>=8?2:3)==0;
            float routeSpikeOffset=level.Number==10?level10RouteSpikeOffset:standardRouteSpikeOffset;
            float safeLandingOffset=level.Number==10?level10SafeLandingOffset:standardSafeLandingOffset;
            float spikeOffset=hasRouteSpike
                ? level.Number==10?Mathf.Sign(x)*routeSpikeOffset:(i%4==0?routeSpikeOffset:-routeSpikeOffset)
                : 0f;
            float safeWaypointOffset=hasRouteSpike?-Mathf.Sign(spikeOffset)*safeLandingOffset:0f;
            CreateWaypoint(root,new Vector2(x+safeWaypointOffset,y+1.4f),i+1,
                radius:level.Number==10?level10LandingRadius:.65f,requirePowerJump:level.Number==10);
            if(hasRouteSpike) CreateSpike(root,art.Spike,new Vector2(x+spikeOffset,y+.8f),0);
            if(level.Number<11 && i>2 && i%4==1) CreateGem(root,art.GreenGem,new Vector2(x,y+2.1f),1);
        }
        CreateDoor(root,art,level.Number,new Vector2(level.Segments%2==0?8.1f:-8.1f,topY+2.55f));
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
            last=new Vector2(-3.2f+i*6.5f,-.1f+i*2.9f);
            float width=Mathf.Max(4.6f,6.3f-level.Number*.1f);
            CreatePlatform(route,art.Platform,$"Angled Ramp {i+1:00}",last,width,18);
            CreateWaypoint(root,last+new Vector2(0,1.6f),i+1);
            if(i>1 && i%(level.Number>=8?2:3)==0) CreateSpike(root,art.Spike,last+new Vector2(1.1f,.9f),18);
            if(level.Number<11 && i>1 && i%3==1) CreateGem(root,art.GreenGem,last+new Vector2(0,2.3f),1);
        }
        CreateDoor(root,art,level.Number,last+new Vector2(3.2f,2.7f));
        keyPosition=new Vector2(last.x*.48f,last.y*.48f+4.3f);
        chestPosition=last+new Vector2(-1.5f,4.6f);
    }

    private static void BuildHorizontalRoute(Transform root,LevelSpec level,ArtSet art,out Vector2 keyPosition,out Vector2 chestPosition)
    {
        Transform route=new GameObject("Horizontal Bottomless Route").transform; route.SetParent(root);
        Vector2 lastPosition=new(-8,-2.7f);
        GameObject previousPlatform=CreatePlatform(route,art.Platform,"Start Shelf",lastPosition,7.2f,0);
        for(int i=0;i<level.Segments;i++)
        {
            float y=i%3==0?-1f:i%3==1?.85f:-.15f;
            Vector2 position=new(-1.3f+i*6.8f,y);
            float width=Mathf.Max(3.5f,4.8f-level.Number*.08f);
            GameObject platform=CreatePlatform(route,art.Platform,$"Pit Ledge {i+1:00}",position,width,0);
            CreateWaypoint(root,position+Vector2.up*1.5f,i+1);
            Physics2D.SyncTransforms();
            Bounds previousBounds=previousPlatform.GetComponent<BoxCollider2D>().bounds;
            Bounds platformBounds=platform.GetComponent<BoxCollider2D>().bounds;
            CreateBottomlessPit(root,$"Bottomless Pit {i+1:00}",previousBounds.max.x,platformBounds.min.x);
            if(i>0 && i%(level.Number>=8?2:3)==0) CreateSpike(root,art.Spike,position+new Vector2(.7f,.8f),0);
            if(i>1 && i%3==1) CreateGem(root,art.GreenGem,position+new Vector2(0,2.1f),1);
            lastPosition=position;
            previousPlatform=platform;
        }
        CreateDoor(root,art,level.Number,lastPosition+new Vector2(3.6f,2.6f));
        keyPosition=lastPosition*.42f+new Vector2(0,5.2f);
        chestPosition=lastPosition+new Vector2(-2,4.6f);
    }

    private static GameObject SpawnHero(GameObject prefab,Scene scene,Vector2 position)
    {
        GameObject hero=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); hero.name="Miner Hero"; hero.transform.position=position; return hero;
    }

    private static void CreateBackdropAndLight(Transform parent,Sprite sprite,Vector3 position,Vector2 scale,float angle=0f)
    {
        CreateBackdrop(parent,sprite,"Bronze Mine Backdrop",position,scale,angle);
        CreateGlobalMineLight(parent);
    }

    private static SpriteRenderer CreateBackdrop(Transform parent,Sprite sprite,string name,Vector3 position,
        Vector2 scale,float angle=0f,int sortingOrder=-100)
    {
        GameObject backdrop=new(name); backdrop.transform.SetParent(parent); backdrop.transform.position=position; backdrop.transform.rotation=Quaternion.Euler(0,0,angle); backdrop.transform.localScale=new Vector3(scale.x,scale.y,1);
        SpriteRenderer renderer=backdrop.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=sortingOrder; return renderer;
    }

    private static void CreateGlobalMineLight(Transform parent)
    {
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
        GameObject go=new("Bronze Spike - 1 Heart"); go.transform.SetParent(parent); go.transform.position=position; go.transform.rotation=Quaternion.Euler(0,0,angle); SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=5; SpikeHitboxGeometry.AddCollider(go); go.AddComponent<DamageZone>().Configure(1);
    }

    private static void CreateHiddenDescentSpike(Transform parent,Sprite sprite,string name,Vector2 position,float angle)
    {
        GameObject go=new(name); go.transform.SetParent(parent); go.transform.position=position; go.transform.rotation=Quaternion.Euler(0,0,angle); go.transform.localScale=new Vector3(1.5f,1.25f,1f);
        SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=6;
        SpikeHitboxGeometry.AddCollider(go);
        go.AddComponent<DamageZone>().Configure(1);
        go.AddComponent<ProximityHiddenHazard>().Configure(10f,.6f);
    }

    private static void CreateOscillatingDescentHazard(Transform parent,Sprite sprite,string name,Vector2 position,float travelDistance,float phase)
    {
        GameObject go=new(name); go.transform.SetParent(parent); go.transform.position=position; go.transform.localScale=new Vector3(2.3f,1.15f,1f);
        SpriteRenderer renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=6;
        SpikeHitboxGeometry.AddCollider(go);
        go.AddComponent<DamageZone>().Configure(1);
        Rigidbody2D body=go.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.gravityScale=0f;
        go.AddComponent<OscillatingHazard>().Configure(Vector2.right,travelDistance,.24f,phase);
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
        GameObject chest=new("Bronze Key Reward Chest"); chest.transform.SetParent(challenge); chest.transform.position=chestPosition; SpriteRenderer chestRenderer=chest.AddComponent<SpriteRenderer>(); chestRenderer.sprite=art.Chest; chestRenderer.sortingOrder=8; BoxCollider2D chestTrigger=chest.AddComponent<BoxCollider2D>(); chestTrigger.isTrigger=true; chestTrigger.size=new Vector2(2.2f,1.8f); chest.AddComponent<RewardChest>().Configure(levelNumber,art.OpenChest);
    }

    private static void CreateSilverKeyChallenge(Transform parent,ArtSet art,Vector2 position)
    {
        Transform headBumpChallenge=new GameObject("Intentional Head-Bump Challenge").transform;
        headBumpChallenge.SetParent(parent);
        Transform secret=new GameObject("Hard Hidden Silver Key Route").transform;
        secret.SetParent(headBumpChallenge);
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
            Vector2 p=new(-1+i*4.4f,-.2f+i*1.9f+(i%2==0?2.6f:0));
            CreateGem(treasure,art.GreenGem,p,1);
            if(i%4==2) CreateSpike(treasure,art.Spike,p+Vector2.down*.9f,22);
        }
        int[] blueIndices={3,6,9,12,15};
        foreach(int i in blueIndices)
        {
            Vector2 shelf=new(-3.2f+i*6.5f,-.1f+i*2.9f+4.5f);
            CreatePlatform(treasure,art.Platform,$"Difficult Blue Gem Perch {i}",shelf+Vector2.down*1.2f,2.5f,0);
            CreateGem(treasure,art.BlueGem,shelf,5);
            CreateSpike(treasure,art.Spike,shelf+new Vector2(-1,-.75f),0);
        }
        Vector2 purple=new(-3.2f+15*6.5f,-.1f+15*2.9f+8.2f);
        CreatePlatform(treasure,art.Platform,"Extreme Purple Gem Perch",purple+Vector2.down*1.2f,2.2f,0);
        CreatePlatform(treasure,art.Platform,"Purple Approach Step",purple+new Vector2(-3.3f,-3.4f),2.1f,0);
        CreateSpike(treasure,art.Spike,purple+new Vector2(-.9f,-.75f),0);
        CreateSpike(treasure,art.Spike,purple+new Vector2(.9f,-.75f),0);
        CreateGem(treasure,art.PurpleGem,purple,20);
    }

    private static void CreateWaypoint(Transform parent,Vector2 position,int order,
        AutomatedWaypointMode mode=AutomatedWaypointMode.GroundedLanding,float radius=.65f,
        bool deployParachute=false,bool requirePowerJump=false)
    {
        GameObject go=new($"Playtest Waypoint {order:00}"); go.transform.SetParent(parent); go.transform.position=position; go.AddComponent<AutomatedPlaytestWaypoint>().Configure(order,mode,radius,deployParachute,requirePowerJump);
    }

    private static void CreateBottomlessPit(Transform parent,string name,float leftEdge,float rightEdge)
    {
        const float edgeClearance=.08f;
        float safeLeft=leftEdge+edgeClearance;
        float safeRight=rightEdge-edgeClearance;
        float width=safeRight-safeLeft;
        if(width<=.25f) throw new InvalidDataException($"{name} has no meaningful visible gap ({width:0.00} units).");
        GameObject pit=new(name); pit.transform.SetParent(parent); pit.transform.position=new Vector3((safeLeft+safeRight)*.5f,-7,0); BoxCollider2D trigger=pit.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(width,6); pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateLocalBottomlessPit(Transform parent,string name,Bounds leftPlatform,Bounds rightPlatform)
    {
        const float edgeClearance=.08f;
        float left=leftPlatform.max.x+edgeClearance;
        float right=rightPlatform.min.x-edgeClearance;
        float width=right-left;
        if(width<=.25f) throw new InvalidDataException($"{name} has no meaningful visible gap ({width:0.00} units).");
        float top=Mathf.Min(leftPlatform.max.y,rightPlatform.max.y)-.45f;
        // A shallow lethal strip catches any fall through the visible gap immediately while
        // leaving deep parachute shafts free of stale triggers from the preceding section.
        const float height=2.5f;
        GameObject pit=new(name); pit.transform.SetParent(parent); pit.transform.position=new Vector3((left+right)*.5f,top-height*.5f,0f);
        BoxCollider2D trigger=pit.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(width,height);
        pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateWallsAndAbyss(Transform parent,Vector2 center,float width,float height,bool bottomless)
    {
        CreateBoundary(parent,"Left Mine Wall",center+Vector2.left*width*.5f,new Vector2(1,height)); CreateBoundary(parent,"Right Mine Wall",center+Vector2.right*width*.5f,new Vector2(1,height)); GameObject pit=new(bottomless?"Bottomless Abyss Death Zone":"Respawn Pit"); pit.transform.SetParent(parent); pit.transform.position=new Vector3(center.x,-11,0); BoxCollider2D trigger=pit.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(width,5); pit.AddComponent<DamageZone>().Configure(99);
    }

    private static void CreateVisibleShaftWall(Transform parent, Sprite rockSprite, string name,
        Vector2 position, float height)
    {
        GameObject wall = new(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.layer = LayerMask.NameToLayer("Ground");
        wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, height);

        GameObject face = new(name + " Bronze Vein Face");
        face.transform.SetParent(wall.transform, false);
        face.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // MineRockBronzePlatform is 6x2 world units at import size. Rotating and
        // scaling it this way makes a one-unit-wide illustrated wall that matches
        // the physical collider and the established bronze-veined rock style.
        face.transform.localScale = new Vector3(height / 6f, .5f, 1f);
        SpriteRenderer renderer = face.AddComponent<SpriteRenderer>();
        renderer.sprite = rockSprite;
        renderer.sortingOrder = 1;
    }

    private static void CreateBoundary(Transform parent,string name,Vector2 position,Vector2 size) { GameObject go=new(name); go.transform.SetParent(parent); go.transform.position=position; go.layer=LayerMask.NameToLayer("Ground"); go.AddComponent<BoxCollider2D>().size=size; }

    private static void CreateHud(GameObject hero,LevelSpec level,string instruction)
    {
        Canvas canvas=CreateCanvas("Level HUD");
        TextMeshProUGUI title=Text(canvas.transform,"Level Title",$"LEVEL {level.Number}  |  {level.DisplayName}",27,TextAlignmentOptions.Center,Amber);
        Rect(title.rectTransform,new(.5f,1),new(.5f,1),new(0,-22),new(760,46));
        TextMeshProUGUI hearts=Text(canvas.transform,"Heart Display","HEARTS",23,TextAlignmentOptions.Left,Color.white);
        Rect(hearts.rectTransform,new(0,1),new(0,1),new(22,-22),new(440,42));
        TextMeshProUGUI lives=Text(canvas.transform,"Lives Display","LIVES",21,TextAlignmentOptions.Right,Color.white);
        Rect(lives.rectTransform,new(1,1),new(1,1),new(-22,-22),new(250,42));
        hero.GetComponent<PlayerHealth>().ConfigureDisplays(hearts,lives);

        TextMeshProUGUI status=Text(canvas.transform,"Run Status","FIND THE HIDDEN BRONZE KEY",18,
            TextAlignmentOptions.Left,new Color32(255,208,112,255));
        Rect(status.rectTransform,new(0,1),new(0,1),new(22,-66),new(760,38));
        hero.GetComponent<MineRunInventory>().Configure(level.Number,status);

        string controls=$"{instruction}\n"+
            "CONTROLLER BUTTONS LOAD AT RUNTIME  |  REMAP: OVERVIEW > CONTROLS\n"+
            "KEYBOARD: ARROWS / A-D MOVE  |  SHIFT RUN + SPACE JUMP  |  UP / W INTERACT  |  H POTION  |  ESC PAUSE  |  BACKSPACE SHOP";
        TextMeshProUGUI instructions=Text(canvas.transform,"Instructions",controls,15,TextAlignmentOptions.Center,Color.white);
        instructions.textWrappingMode=TextWrappingModes.NoWrap;
        instructions.outlineWidth=.18f;
        instructions.outlineColor=Color.black;
        Rect(instructions.rectTransform,new(.5f,0),new(.5f,0),new(0,48),new(1880,86));

        GameObject pause=Panel(canvas.transform,"Pause Menu",new Color(.025f,.035f,.06f,.97f));
        Rect((RectTransform)pause.transform,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(780,520));
        TextMeshProUGUI pauseTitle=Text(pause.transform,"Pause Heading","PAUSED",52,TextAlignmentOptions.Center,Amber);
        Rect(pauseTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-52),new(650,75));
        TextMeshProUGUI pauseHelp=Text(pause.transform,"Pause Controls",
            "START / ESC: RESUME     |     BACK / BACKSPACE: RETURN TO OVERVIEW SHOP",18,
            TextAlignmentOptions.Center,Color.white);
        Rect(pauseHelp.rectTransform,new(.5f,.5f),new(.5f,.5f),new(0,65),new(720,55));
        TextMeshProUGUI retreatWarning=Text(pause.transform,"Retreat Warning",
            "Returning keeps collected rewards but does not complete this level.",17,
            TextAlignmentOptions.Center,new Color32(205,216,230,255));
        Rect(retreatWarning.rectTransform,new(.5f,.5f),new(.5f,.5f),new(0,15),new(700,45));

        MineLevelMenuController menu=canvas.gameObject.AddComponent<MineLevelMenuController>();
        Button resume=CreateActionButton(pause.transform,"Resume Button","RESUME",new Vector2(0,-75),menu.ResumeGame);
        CreateActionButton(pause.transform,"Return To Shop Button","RETURN TO OVERVIEW / SHOP",
            new Vector2(0,-165),menu.ReturnToOverview);
        menu.Configure(pause,resume.gameObject);
        canvas.gameObject.AddComponent<MineControlHintDisplay>().Configure(instructions,pauseHelp,instruction);
        CreateEventSystem(null);
    }

    private static void BuildOverview(Sprite background)
    {
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera camera=CreateCameraBase(new Vector3(0,0,-10));
        camera.orthographicSize=5;
        Canvas canvas=CreateCanvas("Bronze Mines Overview Canvas");
        FullImage(canvas.transform,"Bronze Mines Overview Background",background,Color.white);
        FullImage(canvas.transform,"Readability Overlay",null,new Color(.02f,.03f,.06f,.34f));
        TextMeshProUGUI heading=Text(canvas.transform,"Dungeon Heading","DUNGEON 1  —  BRONZE MINES",40,TextAlignmentOptions.Center,Color.white);
        Rect(heading.rectTransform,new(.5f,1),new(.5f,1),new(0,-28),new(1000,60));
        TextMeshProUGUI balance=Text(canvas.transform,"Persistent Balance","",20,TextAlignmentOptions.Center,new Color32(130,255,165,255));
        Rect(balance.rectTransform,new(.5f,1),new(.5f,1),new(0,-88),new(1200,40));

        GameObject levels=Panel(canvas.transform,"Twelve Tunnel Level Map",new Color(.025f,.035f,.06f,.78f));
        Rect((RectTransform)levels.transform,new(.5f,.5f),new(.5f,.5f),new(0,-15),new(1540,620));
        TextMeshProUGUI mapTitle=Text(levels.transform,"Map Rule","12 TUNNELS  •  VERTICAL → ANGLED → HORIZONTAL → MIXED FINALE",23,TextAlignmentOptions.Center,Amber);
        Rect(mapTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-25),new(1100,38));
        GameObject firstLevelSelection=null;
        for(int i=0;i<Levels.Length;i++)
        {
            int row=i/6; int column=i%6; const float spacing=235f; const float count=6f; float x=(column-(count-1)*.5f)*spacing; float y=row==0?115:-115; Button node=CreateLevelNode(levels.transform,Levels[i],new Vector2(x,y)); if(i==0) firstLevelSelection=node.gameObject;
        }
        TextMeshProUGUI gate=Text(levels.transform,"Final Gates","LEVEL 11: FINISH LEVEL 10 + FIND THE SILVER KEY     •     LEVEL 12: FINISH LEVEL 11",18,TextAlignmentOptions.Center,new Color32(210,220,235,255));
        Rect(gate.rectTransform,new(.5f,0),new(.5f,0),new(0,22),new(1250,35));

        GameObject shop=Panel(canvas.transform,"Shop Page",new Color(.025f,.035f,.06f,.94f));
        Rect((RectTransform)shop.transform,new(.5f,.5f),new(.5f,.5f),new(0,-15),new(860,520));
        TextMeshProUGUI shopTitle=Text(shop.transform,"Shop Title","MINER'S SUPPLY SHOP",34,TextAlignmentOptions.Center,Amber);
        Rect(shopTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-30),new(700,55));
        TextMeshProUGUI status=Text(shop.transform,"Shop Status","Potions restore one heart.",20,TextAlignmentOptions.Center,Color.white);
        Rect(status.rectTransform,new(.5f,0),new(.5f,0),new(0,28),new(760,45));
        MineShopController controller=canvas.gameObject.AddComponent<MineShopController>();
        Button buyLife=CreateActionButton(shop.transform,"Buy Life",$"EXTRA LIFE  —  {GameProgress.ExtraLifePrice} GREEN GEMS",new Vector2(0,110),controller.BuyExtraLife);
        CreateActionButton(shop.transform,"Buy Potion",$"HEALTH POTION (+1 HEART)  —  {GameProgress.HealthPotionPrice} GREEN GEMS",new Vector2(0,10),controller.BuyHealthPotion);
        CreateActionButton(shop.transform,"Buy Heart",$"+1 HEART CAPACITY  —  {GameProgress.HeartUpgradePrice} GREEN GEMS",new Vector2(0,-90),controller.BuyHeartUpgrade);

        GameObject controls=Panel(canvas.transform,"Controller Mapping Page",new Color(.025f,.035f,.06f,.96f));
        Rect((RectTransform)controls.transform,new(.5f,.5f),new(.5f,.5f),new(0,-15),new(1120,650));
        TextMeshProUGUI controlsTitle=Text(controls.transform,"Controls Title","CONTROLLER BUTTON MAPPING",34,TextAlignmentOptions.Center,Amber);
        Rect(controlsTitle.rectTransform,new(.5f,1),new(.5f,1),new(0,-28),new(900,50));
        TextMeshProUGUI controllerName=Text(controls.transform,"Active Controller","ACTIVE CONTROLLER",19,TextAlignmentOptions.Center,new Color32(130,255,165,255));
        Rect(controllerName.rectTransform,new(.5f,1),new(.5f,1),new(0,-82),new(900,38));
        TextMeshProUGUI controlsHelp=Text(controls.transform,"Mapping Help",
            "SELECT AN ACTION, RELEASE THE SUBMIT BUTTON, THEN PRESS ITS NEW CONTROLLER BUTTON.\n"+
            "STICK / D-PAD MOVEMENT, KEYBOARD, AND UI NAVIGATION STAY FIXED.  LOGITECH F310: X MODE IS RECOMMENDED.",
            16,TextAlignmentOptions.Center,Color.white);
        Rect(controlsHelp.rectTransform,new(.5f,1),new(.5f,1),new(0,-126),new(1020,66));
        TextMeshProUGUI mappingStatus=Text(controls.transform,"Mapping Status",
            "MAPPINGS SAVE AUTOMATICALLY FOR EACH CONTROLLER MODEL.",16,TextAlignmentOptions.Center,new Color32(205,216,230,255));
        Rect(mappingStatus.rectTransform,new(.5f,0),new(.5f,0),new(0,25),new(1020,44));

        MineControlsController mappings=controls.AddComponent<MineControlsController>();
        Button run=CreateActionButton(controls.transform,"Map Run Button","RUN",new Vector2(-270,95),mappings.RebindRun);
        Button jump=CreateActionButton(controls.transform,"Map Jump Button","JUMP",new Vector2(270,95),mappings.RebindJump);
        Button interact=CreateActionButton(controls.transform,"Map Interact Button","INTERACT / PARACHUTE",new Vector2(-270,15),mappings.RebindInteract);
        Button potion=CreateActionButton(controls.transform,"Map Potion Button","HEALTH POTION",new Vector2(270,15),mappings.RebindPotion);
        Button pauseButton=CreateActionButton(controls.transform,"Map Pause Button","PAUSE",new Vector2(-270,-65),mappings.RebindPause);
        Button home=CreateActionButton(controls.transform,"Map Home Button","RETURN TO SHOP",new Vector2(270,-65),mappings.RebindHome);
        Button restore=CreateActionButton(controls.transform,"Restore Default Mappings Button","RESTORE DEFAULTS FOR THIS CONTROLLER",new Vector2(0,-165),mappings.RestoreDefaults);
        mappings.Configure(controllerName,mappingStatus,run,jump,interact,potion,pauseButton,home,restore);

        CreateActionButton(canvas.transform,"Levels Tab","LEVELS",new Vector2(-300,-495),controller.ShowLevels);
        CreateActionButton(canvas.transform,"Shop Tab","SHOP",new Vector2(0,-495),controller.ShowShop);
        CreateActionButton(canvas.transform,"Controls Tab","CONTROLS",new Vector2(300,-495),controller.ShowControls);
        controller.Configure(levels,shop,controls,balance,status,firstLevelSelection,buyLife.gameObject,run.gameObject);
        controller.ShowLevels();
        CreateEventSystem(firstLevelSelection);
        EditorSceneManager.SaveScene(scene,OverviewPath);
    }

    private static void BuildGameOver(Sprite background)
    {
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera camera=CreateCameraBase(new Vector3(0,0,-10));
        camera.orthographicSize=5;
        Canvas canvas=CreateCanvas("Game Over Canvas");
        FullImage(canvas.transform,"Bronze Mines Background",background,new Color(.55f,.55f,.55f,1f));
        FullImage(canvas.transform,"Game Over Dark Overlay",null,new Color(.015f,.02f,.035f,.82f));
        GameObject panel=Panel(canvas.transform,"Game Over Panel",new Color(.04f,.045f,.06f,.96f));
        Rect((RectTransform)panel.transform,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new Vector2(820,490));
        TextMeshProUGUI heading=Text(panel.transform,"Game Over Heading","GAME OVER",62,TextAlignmentOptions.Center,new Color32(255,142,90,255));
        Rect(heading.rectTransform,new(.5f,1),new(.5f,1),new(0,-55),new(700,90));
        TextMeshProUGUI message=Text(panel.transform,"Out Of Lives Message","OUT OF LIVES",30,TextAlignmentOptions.Center,Color.white);
        Rect(message.rectTransform,new(.5f,.5f),new(.5f,.5f),new Vector2(0,45),new Vector2(600,55));
        TextMeshProUGUI resetWarning=Text(panel.transform,"Progress Reset Message","Restart begins a new run and resets gems, upgrades, keys, chests, and unlocked tunnels.",20,TextAlignmentOptions.Center,new Color32(205,216,230,255));
        Rect(resetWarning.rectTransform,new(.5f,.5f),new(.5f,.5f),new Vector2(0,-18),new Vector2(710,64));
        GameOverController controller=canvas.gameObject.AddComponent<GameOverController>();
        Button restart=CreateActionButton(panel.transform,"Restart Button",$"RESTART  -  {GameProgress.StartingLives} LIVES",new Vector2(0,-125),controller.RestartGame);
        TextMeshProUGUI shortcut=Text(panel.transform,"Restart Shortcut","PRESS A / ENTER / SPACE",16,TextAlignmentOptions.Center,new Color32(170,180,195,255));
        Rect(shortcut.rectTransform,new(.5f,0),new(.5f,0),new(0,28),new(500,32));
        CreateEventSystem(restart.gameObject);
        EditorSceneManager.SaveScene(scene,GameOverPath);
    }

    private static Button CreateLevelNode(Transform parent,LevelSpec level,Vector2 position)
    {
        GameObject go=new($"Mineshaft {level.Number}",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=new Color32(166,99,48,245); Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(205,104)); TextMeshProUGUI label=Text(go.transform,"Label",$"{level.Number}\n{level.DisplayName}",17,TextAlignmentOptions.Center,Color.white); Stretch(label.rectTransform); go.AddComponent<MineLevelSelectButton>().Configure(level.Number,level.SceneName,level.DisplayName,label);
        return go.GetComponent<Button>();
    }

    private static Button CreateActionButton(Transform parent,string name,string label,Vector2 position,UnityEngine.Events.UnityAction action)
    {
        GameObject go=new(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=Bronze; UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick,action); Rect((RectTransform)go.transform,new(.5f,.5f),new(.5f,.5f),position,new(500,72)); TextMeshProUGUI text=Text(go.transform,"Label",label,20,TextAlignmentOptions.Center,Color.white); Stretch(text.rectTransform);
        return go.GetComponent<Button>();
    }

    private static EventSystem CreateEventSystem(GameObject firstSelected)
    {
        GameObject go=new("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        EventSystem eventSystem=go.GetComponent<EventSystem>();
        eventSystem.firstSelectedGameObject=firstSelected;
        InputSystemUIInputModule inputModule=go.GetComponent<InputSystemUIInputModule>();
        if(inputModule.actionsAsset==null) inputModule.AssignDefaultActions();
        return eventSystem;
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

    private static void CreatePixelAssets() { WritePlatform(); WriteGem(GreenGemPath,new Color32(11,91,56,255),new Color32(32,210,106,255),new Color32(145,255,184,255)); WriteGem(BlueGemPath,new Color32(18,63,139,255),new Color32(46,148,255,255),new Color32(176,230,255,255)); WriteGem(PurpleGemPath,new Color32(74,27,112,255),new Color32(177,72,234,255),new Color32(245,184,255,255)); WriteSpike(); WriteParachute(); WriteKey(BronzeKeyPath,new Color32(106,58,24,255),new Color32(213,126,54,255),new Color32(255,193,94,255)); WriteKey(SilverKeyPath,new Color32(72,83,101,255),new Color32(172,190,211,255),new Color32(242,250,255,255)); WriteChest(); WriteOpenChest(); }
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
    private static void WriteSpike() { Texture2D t=Texture(40,24); Color32 d=new(63,43,38,255),b=new(179,104,54,255),l=new(239,174,88,255); for(int n=0;n<3;n++){int center=7+n*13;for(int y=3;y<22;y++){int half=(21-y)/4;for(int x=center-half;x<=center+half;x++)t.SetPixel(x,y,x<center?d:b);}t.SetPixel(center,20,l);}Fill(t,1,1,39,4,d);Save(t,SpikePath); }
    private static void WriteParachute()
    {
        Texture2D t=Texture(96,64);
        Color32 dark=new(70,48,38,255),bronze=new(170,92,43,255),light=new(236,157,77,255),silver=new(181,192,199,255),rope=new(154,104,52,255);
        for(int y=26;y<57;y++)
        {
            float normalized=(y-26)/30f;
            int half=Mathf.RoundToInt(Mathf.Sin(normalized*Mathf.PI*.5f)*43f);
            for(int x=48-half;x<=48+half;x++)
            {
                int panel=Mathf.Clamp((x-(48-half))*5/Mathf.Max(1,half*2+1),0,4);
                Color32 color=panel%2==0?bronze:dark;
                if(y>=54) color=light;
                t.SetPixel(x,y,color);
            }
        }
        for(int rib=-2;rib<=2;rib++)
        {
            int targetX=48+rib*18;
            for(int step=0;step<25;step++)
            {
                int x=Mathf.RoundToInt(Mathf.Lerp(48,targetX,step/24f));
                int y=31+step;
                t.SetPixel(x,y,silver);
            }
        }
        for(int step=0;step<28;step++)
        {
            int left=Mathf.RoundToInt(Mathf.Lerp(10,45,step/27f));
            int right=Mathf.RoundToInt(Mathf.Lerp(86,51,step/27f));
            int y=27-step;
            t.SetPixel(left,y,rope); t.SetPixel(right,y,rope);
        }
        Fill(t,43,0,53,4,dark);
        Save(t,ParachutePath);
    }
    private static void WriteKey(string path,Color32 dark,Color32 color,Color32 light) { Texture2D t=Texture(36,20); for(int y=5;y<16;y++)for(int x=3;x<14;x++){float dx=x-8,dy=y-10;if(dx*dx+dy*dy<=25&&dx*dx+dy*dy>=9)t.SetPixel(x,y,x<8?dark:color);}Fill(t,12,9,31,13,color);Fill(t,25,5,29,10,color);Fill(t,29,5,33,13,dark);Fill(t,13,10,28,11,light);Save(t,path); }
    private static void WriteChest() { Texture2D t=Texture(44,32); Color32 dark=new(67,38,25,255),wood=new(132,70,35,255),bronze=new(202,121,52,255),light=new(255,184,83,255);Fill(t,3,4,41,26,dark);Fill(t,6,7,38,23,wood);Fill(t,3,14,41,18,bronze);Fill(t,19,12,25,22,light);Fill(t,7,7,37,10,bronze);Save(t,ChestPath); }
    private static void WriteOpenChest()
    {
        Texture2D t=Texture(44,40);
        Color32 shadow=new(30,20,18,255),dark=new(67,38,25,255),wood=new(132,70,35,255);
        Color32 bronze=new(202,121,52,255),light=new(255,184,83,255);

        // Raised lid and bright rim make the persisted claimed state unmistakable.
        Fill(t,4,24,40,35,dark); Fill(t,7,27,37,32,wood); Fill(t,4,23,40,27,bronze);
        Fill(t,7,25,37,27,light);
        Fill(t,3,4,41,21,dark); Fill(t,6,7,38,18,wood); Fill(t,5,17,39,23,shadow);
        Fill(t,3,13,41,17,bronze); Fill(t,19,11,25,19,light);
        Fill(t,8,20,36,23,new Color32(14,12,14,255));
        Save(t,OpenChestPath);
    }
}
