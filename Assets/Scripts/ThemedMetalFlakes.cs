using UnityEngine;

/// <summary>
/// Adds a deterministic, sparse overlay of metallic flecks to a SpriteRenderer.
/// Change Theme to swap the whole surface from bronze to silver (or any later dungeon).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ThemedMetalFlakes : MonoBehaviour
{
    private const string FlakeRootName = "__ThemedMetalFlakes";
    private static Sprite fallbackFlakeSprite;

    [SerializeField] private DungeonVisualTheme theme;
    [SerializeField] private SpriteRenderer surfaceRenderer;
    [SerializeField, Min(0f)] private float densityMultiplier = 1f;
    [SerializeField, Min(0)] private int maximumFlakeCount = 384;
    [SerializeField] private Vector2 flakeSizeRange = new(.065f, .16f);
    [SerializeField, Range(0f, .45f)] private float edgeInset = .08f;
    [SerializeField] private int seedOffset;
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("Optional Sparkle")]
    [SerializeField] private bool animateSparkle = true;
    [SerializeField, Min(.05f)] private float sparkleRefreshSeconds = .12f;
    [SerializeField, Min(.1f)] private float sparkleCycleSeconds = 2.8f;
    [SerializeField, Range(0f, 1f)] private float sparkleStrength = .72f;

    [SerializeField, HideInInspector] private Transform flakeRoot;

    private SpriteRenderer[] flakeRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] restingColors = System.Array.Empty<Color>();
    private float[] sparklePhases = System.Array.Empty<float>();
    private bool rebuildRequested;
    private float nextSparkleAt;
    private float visibilityMultiplier = 1f;

    public DungeonVisualTheme Theme => theme;
    public SpriteRenderer SurfaceRenderer => surfaceRenderer;
    public int GeneratedFlakeCount => flakeRenderers.Length;
    public float EffectiveDensity => theme == null ? 0f : theme.MetalFlakeDensity * densityMultiplier;
    public int DeterministicSeed => BuildDeterministicSeed();
    public float VisibilityMultiplier => visibilityMultiplier;
    public Sprite EffectiveFlakeSprite => theme != null && theme.MetalFlakeSprite != null
        ? theme.MetalFlakeSprite
        : GetFallbackFlakeSprite();

    public void Configure(DungeonVisualTheme visualTheme, SpriteRenderer surface = null,
        float densityScale = 1f, int additionalSeed = 0)
    {
        theme = visualTheme;
        surfaceRenderer = surface != null ? surface : GetComponent<SpriteRenderer>();
        densityMultiplier = Mathf.Max(0f, densityScale);
        seedOffset = additionalSeed;
        RebuildNow();
    }

    /// <summary>The only call needed to change this surface from one dungeon metal to another.</summary>
    public void SetTheme(DungeonVisualTheme visualTheme)
    {
        theme = visualTheme;
        RebuildNow();
    }

    public void SetVisibilityMultiplier(float multiplier)
    {
        visibilityMultiplier = Mathf.Clamp01(multiplier);
        ApplySparkle(Application.isPlaying ? Time.unscaledTime : 0f);
    }

    public void RebuildNow()
    {
        rebuildRequested = false;
        if (surfaceRenderer == null) surfaceRenderer = GetComponent<SpriteRenderer>();

        int desiredCount = CalculateDesiredFlakeCount();
        if (desiredCount <= 0)
        {
            RemoveGeneratedFlakes();
            return;
        }

        EnsureFlakeRoot();
        ResizeChildren(desiredCount);

        flakeRenderers = new SpriteRenderer[desiredCount];
        restingColors = new Color[desiredCount];
        sparklePhases = new float[desiredCount];

        Vector2 surfaceSize = GetLocalSurfaceSize();
        Vector2 surfaceCenter = GetLocalSurfaceCenter();
        float usableWidth = surfaceSize.x * (1f - edgeInset * 2f);
        float usableHeight = surfaceSize.y * (1f - edgeInset * 2f);
        Sprite flakeSprite = EffectiveFlakeSprite;
        uint randomState = unchecked((uint)BuildDeterministicSeed());
        if (randomState == 0u) randomState = 0x6D2B79F5u;

        for (int i = 0; i < desiredCount; i++)
        {
            Transform flake = flakeRoot.GetChild(i);
            flake.name = $"Metal Flake {i + 1:00}";

            SpriteRenderer renderer = flake.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = flake.gameObject.AddComponent<SpriteRenderer>();
            flakeRenderers[i] = renderer;

            float x = (Next01(ref randomState) - .5f) * usableWidth;
            float y = (Next01(ref randomState) - .5f) * usableHeight;
            flake.localPosition = new Vector3(surfaceCenter.x + x, surfaceCenter.y + y, -.01f);
            flake.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-38f, 38f, Next01(ref randomState)));

            float size = Mathf.Lerp(flakeSizeRange.x, flakeSizeRange.y, Next01(ref randomState));
            Vector2 spriteSize = flakeSprite.bounds.size;
            float spriteWidth = Mathf.Max(.001f, spriteSize.x);
            float spriteHeight = Mathf.Max(.001f, spriteSize.y);
            float thinness = Mathf.Lerp(.28f, .66f, Next01(ref randomState));
            Vector3 surfaceScale = transform.lossyScale;
            float inverseWorldScaleX = 1f / Mathf.Max(.001f, Mathf.Abs(surfaceScale.x));
            float inverseWorldScaleY = 1f / Mathf.Max(.001f, Mathf.Abs(surfaceScale.y));
            flake.localScale = new Vector3(size / spriteWidth * inverseWorldScaleX,
                size * thinness / spriteHeight * inverseWorldScaleY, 1f);

            float brightness = Next01(ref randomState);
            Color resting = brightness < .22f
                ? Color.Lerp(theme.MetalShadow, theme.MetalBase, .5f + brightness * 2f)
                : Color.Lerp(theme.MetalBase, theme.MetalHighlight,
                    .16f + (brightness - .22f) * .88f);
            resting.a = Mathf.Lerp(.58f, .94f, Next01(ref randomState));
            restingColors[i] = resting;
            sparklePhases[i] = Next01(ref randomState);

            renderer.sprite = flakeSprite;
            CopySortingSettings(surfaceRenderer, renderer);
            renderer.color = WithMultipliedAlpha(resting, visibilityMultiplier);
            renderer.enabled = surfaceRenderer.enabled;
        }

        nextSparkleAt = 0f;
        ApplySparkle(Application.isPlaying ? Time.unscaledTime : 0f);
    }

    private void OnEnable()
    {
        if (surfaceRenderer == null) surfaceRenderer = GetComponent<SpriteRenderer>();
        RebuildNow();
    }

    private void OnValidate()
    {
        densityMultiplier = Mathf.Max(0f, densityMultiplier);
        maximumFlakeCount = Mathf.Max(0, maximumFlakeCount);
        flakeSizeRange.x = Mathf.Max(.005f, flakeSizeRange.x);
        flakeSizeRange.y = Mathf.Max(flakeSizeRange.x, flakeSizeRange.y);
        sparkleRefreshSeconds = Mathf.Max(.05f, sparkleRefreshSeconds);
        sparkleCycleSeconds = Mathf.Max(.1f, sparkleCycleSeconds);
        rebuildRequested = true;
    }

    private void Update()
    {
        if (rebuildRequested)
        {
            RebuildNow();
        }

        if (!Application.isPlaying || !animateSparkle || flakeRenderers.Length == 0) return;
        if (Time.unscaledTime < nextSparkleAt) return;

        nextSparkleAt = Time.unscaledTime + sparkleRefreshSeconds;
        ApplySparkle(Time.unscaledTime);
    }

    private int CalculateDesiredFlakeCount()
    {
        if (theme == null || surfaceRenderer == null || surfaceRenderer.sprite == null ||
            maximumFlakeCount <= 0)
        {
            return 0;
        }

        Vector2 size = GetLocalSurfaceSize();
        Vector3 worldScale = transform.lossyScale;
        float area = Mathf.Abs(size.x * worldScale.x * size.y * worldScale.y);
        float exactCount = area * theme.MetalFlakeDensity * densityMultiplier;
        if (exactCount <= 0f) return 0;
        return Mathf.Clamp(Mathf.CeilToInt(exactCount), 1, maximumFlakeCount);
    }

    private Vector2 GetLocalSurfaceSize()
    {
        if (surfaceRenderer == null || surfaceRenderer.sprite == null) return Vector2.zero;
        return surfaceRenderer.drawMode == SpriteDrawMode.Simple
            ? surfaceRenderer.sprite.bounds.size
            : surfaceRenderer.size;
    }

    private Vector2 GetLocalSurfaceCenter()
    {
        if (surfaceRenderer == null || surfaceRenderer.sprite == null) return Vector2.zero;
        return surfaceRenderer.sprite.bounds.center;
    }

    private void EnsureFlakeRoot()
    {
        if (flakeRoot != null && flakeRoot.parent == transform) return;

        Transform existing = transform.Find(FlakeRootName);
        if (existing != null)
        {
            flakeRoot = existing;
            return;
        }

        GameObject root = new(FlakeRootName);
        flakeRoot = root.transform;
        flakeRoot.SetParent(transform, false);
        flakeRoot.localPosition = Vector3.zero;
        flakeRoot.localRotation = Quaternion.identity;
        flakeRoot.localScale = Vector3.one;
    }

    private void ResizeChildren(int desiredCount)
    {
        while (flakeRoot.childCount < desiredCount)
        {
            GameObject child = new("Metal Flake");
            child.transform.SetParent(flakeRoot, false);
            child.AddComponent<SpriteRenderer>();
        }

        for (int i = flakeRoot.childCount - 1; i >= desiredCount; i--)
        {
            GameObject extra = flakeRoot.GetChild(i).gameObject;
            extra.SetActive(false);
            if (Application.isPlaying)
            {
                // Detaching makes repeated same-frame theme rebuilds safe even
                // though Unity defers the actual destruction until frame end.
                extra.transform.SetParent(null, false);
                Destroy(extra);
            }
            else DestroyImmediate(extra);
        }
    }

    private void RemoveGeneratedFlakes()
    {
        flakeRenderers = System.Array.Empty<SpriteRenderer>();
        restingColors = System.Array.Empty<Color>();
        sparklePhases = System.Array.Empty<float>();
        if (flakeRoot == null) flakeRoot = transform.Find(FlakeRootName);
        if (flakeRoot == null) return;

        GameObject root = flakeRoot.gameObject;
        flakeRoot = null;
        root.SetActive(false);
        if (Application.isPlaying) Destroy(root);
        else DestroyImmediate(root);
    }

    private void ApplySparkle(float time)
    {
        if (theme == null) return;
        int count = Mathf.Min(flakeRenderers.Length, restingColors.Length);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer renderer = flakeRenderers[i];
            if (renderer == null) continue;

            float sparkle = 0f;
            if (animateSparkle && Application.isPlaying)
            {
                float cycle = Mathf.Repeat(time / sparkleCycleSeconds + sparklePhases[i], 1f);
                float peak = Mathf.Max(0f, Mathf.Sin(cycle * Mathf.PI * 2f));
                sparkle = Mathf.Pow(peak, 12f) * sparkleStrength;
            }

            Color color = Color.Lerp(restingColors[i], theme.MetalGlint, sparkle);
            color.a = Mathf.Lerp(restingColors[i].a, theme.MetalGlint.a, sparkle) * visibilityMultiplier;
            renderer.color = color;
            renderer.enabled = surfaceRenderer != null && surfaceRenderer.enabled;
        }
    }

    private void CopySortingSettings(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder + sortingOrderOffset;
        destination.spriteSortPoint = source.spriteSortPoint;
        destination.maskInteraction = source.maskInteraction;
        destination.sharedMaterial = source.sharedMaterial;
    }

    private int BuildDeterministicSeed()
    {
        unchecked
        {
            uint hash = theme == null ? 2166136261u : (uint)theme.MetalFlakeSeed;
            hash = (hash ^ (uint)seedOffset) * 16777619u;
            Transform current = transform;
            while (current != null)
            {
                string objectName = current.name;
                for (int i = 0; i < objectName.Length; i++)
                    hash = (hash ^ objectName[i]) * 16777619u;
                current = current.parent;
            }

            Vector3 position = transform.localPosition;
            hash = (hash ^ (uint)Mathf.RoundToInt(position.x * 100f)) * 16777619u;
            hash = (hash ^ (uint)Mathf.RoundToInt(position.y * 100f)) * 16777619u;
            return (int)hash;
        }
    }

    private static float Next01(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0x00FFFFFFu) / 16777216f;
    }

    private static Color WithMultipliedAlpha(Color color, float multiplier)
    {
        color.a *= multiplier;
        return color;
    }

    private static Sprite GetFallbackFlakeSprite()
    {
        if (fallbackFlakeSprite != null) return fallbackFlakeSprite;

        const int width = 8;
        const int height = 4;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime Metal Flake",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            int horizontalInset = y == 0 || y == height - 1 ? 2 : y == 1 ? 1 : 0;
            for (int x = horizontalInset; x < width - horizontalInset; x++)
                pixels[y * width + x] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        fallbackFlakeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(.5f, .5f), width);
        fallbackFlakeSprite.name = "Runtime Metal Flake";
        fallbackFlakeSprite.hideFlags = HideFlags.HideAndDontSave;
        return fallbackFlakeSprite;
    }
}
