using UnityEngine;

/// <summary>
/// Shared art direction for one dungeon. Assigning a different theme is enough
/// to recolor procedural details such as metal flakes without changing a prefab.
/// </summary>
[CreateAssetMenu(menuName = "Jump/Dungeon Visual Theme", fileName = "DungeonVisualTheme")]
public sealed class DungeonVisualTheme : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string dungeonId = "bronze";
    [SerializeField] private string displayName = "Bronze Dungeon";

    [Header("Stone and Metal Palette")]
    [SerializeField] private Color rockTint = new Color32(91, 78, 70, 255);
    [SerializeField] private Color metalShadow = new Color32(80, 43, 25, 255);
    [SerializeField] private Color metalBase = new Color32(176, 97, 50, 255);
    [SerializeField] private Color metalHighlight = new Color32(241, 170, 91, 255);
    [SerializeField] private Color metalGlint = new Color32(255, 236, 184, 255);

    [Header("Lighting Palette")]
    [SerializeField] private Color ambientLight = new Color32(97, 109, 132, 255);
    [SerializeField] private Color keyLight = new Color32(255, 220, 166, 255);
    [SerializeField] private Color accentLight = new Color32(139, 193, 255, 255);

    [Header("Metal Flakes")]
    [SerializeField, Min(0f)] private float metalFlakeDensity = .42f;
    [SerializeField] private int metalFlakeSeed = 1701;

    [Header("Environment Sprites")]
    [SerializeField] private Sprite distantBackgroundSprite;
    [SerializeField] private Sprite foregroundRockPanelSprite;
    [SerializeField] private Sprite rockFillSprite;
    [SerializeField] private Sprite rockDetailSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite platformSprite;
    [SerializeField] private Sprite metalFlakeSprite;
    [SerializeField] private Sprite glintSprite;

    [Header("Gameplay Sprites")]
    [SerializeField] private Sprite spikeSprite;
    [SerializeField] private Sprite greenGemSprite;
    [SerializeField] private Sprite blueGemSprite;
    [SerializeField] private Sprite purpleGemSprite;
    [SerializeField] private Sprite keySprite;
    [SerializeField] private Sprite chestClosedSprite;
    [SerializeField] private Sprite chestOpenSprite;
    [SerializeField] private Sprite doorClosedSprite;
    [SerializeField] private Sprite doorOpenSprite;
    [SerializeField] private Sprite hangGliderSprite;

    public string DungeonId => dungeonId;
    public string DisplayName => displayName;
    public Color RockTint => rockTint;
    public Color MetalShadow => metalShadow;
    public Color MetalBase => metalBase;
    public Color MetalHighlight => metalHighlight;
    public Color MetalGlint => metalGlint;
    public Color AmbientLight => ambientLight;
    public Color KeyLight => keyLight;
    public Color AccentLight => accentLight;
    public float MetalFlakeDensity => metalFlakeDensity;
    public int MetalFlakeSeed => metalFlakeSeed;

    public Sprite DistantBackgroundSprite => distantBackgroundSprite;
    public Sprite ForegroundRockPanelSprite => foregroundRockPanelSprite;
    public Sprite RockFillSprite => rockFillSprite;
    public Sprite RockDetailSprite => rockDetailSprite;
    public Sprite WallSprite => wallSprite;
    public Sprite PlatformSprite => platformSprite;
    public Sprite MetalFlakeSprite => metalFlakeSprite;
    public Sprite GlintSprite => glintSprite;
    public Sprite SpikeSprite => spikeSprite;
    public Sprite GreenGemSprite => greenGemSprite;
    public Sprite BlueGemSprite => blueGemSprite;
    public Sprite PurpleGemSprite => purpleGemSprite;
    public Sprite KeySprite => keySprite;
    public Sprite ChestClosedSprite => chestClosedSprite;
    public Sprite ChestOpenSprite => chestOpenSprite;
    public Sprite DoorClosedSprite => doorClosedSprite;
    public Sprite DoorOpenSprite => doorOpenSprite;
    public Sprite HangGliderSprite => hangGliderSprite;

    /// <summary>Useful to editor builders that create the theme asset in code.</summary>
    public void ConfigureIdentity(string id, string dungeonName)
    {
        dungeonId = string.IsNullOrWhiteSpace(id) ? "dungeon" : id.Trim();
        displayName = string.IsNullOrWhiteSpace(dungeonName) ? dungeonId : dungeonName.Trim();
    }

    public void ConfigurePalette(Color stone, Color shadow, Color metal, Color highlight,
        Color glint, Color ambient, Color key, Color accent)
    {
        rockTint = stone;
        metalShadow = shadow;
        metalBase = metal;
        metalHighlight = highlight;
        metalGlint = glint;
        ambientLight = ambient;
        keyLight = key;
        accentLight = accent;
    }

    public void ConfigureMetalFlakes(float flakesPerSquareUnit, int deterministicSeed,
        Sprite flakeSprite, Sprite shineSprite = null)
    {
        metalFlakeDensity = Mathf.Max(0f, flakesPerSquareUnit);
        metalFlakeSeed = deterministicSeed;
        metalFlakeSprite = flakeSprite;
        glintSprite = shineSprite;
    }

    public void ConfigureEnvironmentSprites(Sprite distantBackground, Sprite foregroundPanel,
        Sprite rockFill, Sprite rockDetail, Sprite wall, Sprite platform, Sprite flake, Sprite shine)
    {
        distantBackgroundSprite = distantBackground;
        foregroundRockPanelSprite = foregroundPanel;
        rockFillSprite = rockFill;
        rockDetailSprite = rockDetail;
        wallSprite = wall;
        platformSprite = platform;
        metalFlakeSprite = flake;
        glintSprite = shine;
    }

    public void ConfigureGameplaySprites(Sprite spikes, Sprite greenGem, Sprite blueGem,
        Sprite purpleGem, Sprite key, Sprite chestClosed, Sprite chestOpen,
        Sprite doorClosed, Sprite doorOpen, Sprite glider = null)
    {
        spikeSprite = spikes;
        greenGemSprite = greenGem;
        blueGemSprite = blueGem;
        purpleGemSprite = purpleGem;
        keySprite = key;
        chestClosedSprite = chestClosed;
        chestOpenSprite = chestOpen;
        doorClosedSprite = doorClosed;
        doorOpenSprite = doorOpen;
        hangGliderSprite = glider;
    }

    private void OnValidate()
    {
        metalFlakeDensity = Mathf.Max(0f, metalFlakeDensity);
        if (string.IsNullOrWhiteSpace(dungeonId)) dungeonId = "dungeon";
        if (string.IsNullOrWhiteSpace(displayName)) displayName = dungeonId;
    }
}
