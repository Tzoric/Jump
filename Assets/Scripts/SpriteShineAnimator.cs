using UnityEngine;

/// <summary>
/// Lightweight sprite-only glint for gems, keys and metal hazards. It uses no
/// shader or PBR dependency, so it behaves the same in built-in and URP 2D renderers.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteShineAnimator : MonoBehaviour
{
    private const string GlintObjectName = "__SpriteShineGlint";
    private static Sprite fallbackGlintSprite;

    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private Sprite glintSprite;
    [SerializeField] private Color glintColor = new(1f, .94f, .72f, .82f);
    [SerializeField, Min(.1f)] private float cycleSeconds = 2.25f;
    [SerializeField, Range(0f, 1f)] private float phaseOffset;
    [SerializeField] private Vector2 pathDirection = new(1f, .2f);
    [SerializeField, Min(0f)] private float travelDistance;
    [SerializeField, Min(.01f)] private float glintScale = .2f;
    [SerializeField, Range(0f, 1f)] private float minimumScale = .55f;
    [SerializeField, Min(1f)] private float edgeSoftness = 2.4f;
    [SerializeField] private float glintRotationDegrees = -18f;
    [SerializeField] private int sortingOrderOffset = 1;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool previewInEditMode;
    [SerializeField] private bool playOnEnable = true;

    [SerializeField, HideInInspector] private SpriteRenderer glintRenderer;

    private bool isPlaying;

    public SpriteRenderer SourceRenderer => sourceRenderer;
    public SpriteRenderer GlintRenderer => glintRenderer;
    public float CycleSeconds => cycleSeconds;
    public float PhaseOffset => phaseOffset;
    public float NormalizedCycle { get; private set; }
    public float CurrentGlintAlpha { get; private set; }
    public bool IsAnimating => isPlaying && enabled && gameObject.activeInHierarchy;
    public Sprite EffectiveGlintSprite => glintSprite != null ? glintSprite : GetFallbackGlintSprite();

    public void Configure(SpriteRenderer source, Sprite shineSprite, Color shineColor,
        float secondsPerCycle = 2.25f, float phase = 0f)
    {
        sourceRenderer = source != null ? source : GetComponent<SpriteRenderer>();
        glintSprite = shineSprite;
        glintColor = shineColor;
        cycleSeconds = Mathf.Max(.1f, secondsPerCycle);
        phaseOffset = Mathf.Repeat(phase, 1f);
        EnsureGlintRenderer();
        ApplyNormalizedCycle(phaseOffset);
    }

    public void Configure(DungeonVisualTheme theme, SpriteRenderer source = null,
        float secondsPerCycle = 2.25f, float phase = 0f)
    {
        Configure(source, theme == null ? null : theme.GlintSprite,
            theme == null ? glintColor : theme.MetalGlint, secondsPerCycle, phase);
    }

    public void SetPhaseOffset(float normalizedOffset)
    {
        phaseOffset = Mathf.Repeat(normalizedOffset, 1f);
    }

    public void SetPlaying(bool shouldPlay)
    {
        isPlaying = shouldPlay;
        if (glintRenderer != null) glintRenderer.enabled = shouldPlay;
    }

    /// <summary>Deterministically samples the animation; useful for tests and authored poses.</summary>
    public void ApplyNormalizedCycle(float normalizedTime)
    {
        EnsureGlintRenderer();
        NormalizedCycle = Mathf.Repeat(normalizedTime, 1f);
        if (glintRenderer == null || sourceRenderer == null || sourceRenderer.sprite == null)
        {
            CurrentGlintAlpha = 0f;
            return;
        }

        glintRenderer.sprite = EffectiveGlintSprite;
        Vector2 direction = pathDirection.sqrMagnitude < .001f ? Vector2.right : pathDirection.normalized;
        float distance = travelDistance > 0f ? travelDistance : CalculateAutomaticTravel(direction);
        float position = Mathf.Lerp(-distance * .5f, distance * .5f, NormalizedCycle);
        glintRenderer.transform.localPosition = new Vector3(direction.x * position,
            direction.y * position, -.015f);
        glintRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, glintRotationDegrees);

        float envelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(NormalizedCycle * Mathf.PI)), edgeSoftness);
        float pulseScale = Mathf.Lerp(minimumScale, 1f, envelope) * glintScale;
        glintRenderer.transform.localScale = new Vector3(pulseScale, pulseScale, 1f);

        Color color = glintColor;
        color.a *= envelope;
        glintRenderer.color = color;
        CurrentGlintAlpha = color.a;
        CopyRendererSettings();
        glintRenderer.enabled = isPlaying || (!Application.isPlaying && previewInEditMode);
    }

    public void RefreshVisual()
    {
        EnsureGlintRenderer();
        ApplyNormalizedCycle(NormalizedCycle);
    }

    private void OnEnable()
    {
        if (sourceRenderer == null) sourceRenderer = GetComponent<SpriteRenderer>();
        isPlaying = playOnEnable;
        EnsureGlintRenderer();
        ApplyNormalizedCycle(phaseOffset);
    }

    private void OnDisable()
    {
        if (glintRenderer != null) glintRenderer.enabled = false;
    }

    private void OnValidate()
    {
        cycleSeconds = Mathf.Max(.1f, cycleSeconds);
        phaseOffset = Mathf.Repeat(phaseOffset, 1f);
        glintScale = Mathf.Max(.01f, glintScale);
        minimumScale = Mathf.Clamp01(minimumScale);
        edgeSoftness = Mathf.Max(1f, edgeSoftness);
    }

    private void Update()
    {
        if (!Application.isPlaying && !previewInEditMode) return;
        if (!isPlaying) return;

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        float normalized = Mathf.Repeat(now / cycleSeconds + phaseOffset, 1f);
        ApplyNormalizedCycle(normalized);
    }

    private void EnsureGlintRenderer()
    {
        if (sourceRenderer == null) sourceRenderer = GetComponent<SpriteRenderer>();
        if (glintRenderer != null && glintRenderer.transform.parent == transform) return;

        Transform existing = transform.Find(GlintObjectName);
        if (existing == null)
        {
            GameObject glint = new(GlintObjectName);
            existing = glint.transform;
            existing.SetParent(transform, false);
        }

        glintRenderer = existing.GetComponent<SpriteRenderer>();
        if (glintRenderer == null) glintRenderer = existing.gameObject.AddComponent<SpriteRenderer>();
        CopyRendererSettings();
    }

    private float CalculateAutomaticTravel(Vector2 direction)
    {
        Vector2 size = sourceRenderer.drawMode == SpriteDrawMode.Simple
            ? sourceRenderer.sprite.bounds.size
            : sourceRenderer.size;
        return Mathf.Abs(direction.x) * size.x + Mathf.Abs(direction.y) * size.y;
    }

    private void CopyRendererSettings()
    {
        if (sourceRenderer == null || glintRenderer == null) return;
        glintRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        glintRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
        glintRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
        glintRenderer.maskInteraction = sourceRenderer.maskInteraction;
        glintRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        glintRenderer.flipX = sourceRenderer.flipX;
        glintRenderer.flipY = sourceRenderer.flipY;
    }

    private static Sprite GetFallbackGlintSprite()
    {
        if (fallbackGlintSprite != null) return fallbackGlintSprite;

        const int size = 9;
        const int center = size / 2;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Sprite Glint",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - center);
                int dy = Mathf.Abs(y - center);
                float axial = dx == 0 ? 1f - dy / (float)(center + 1) :
                    dy == 0 ? 1f - dx / (float)(center + 1) : 0f;
                float diamond = Mathf.Clamp01(1f - (dx + dy) / 3f) * .7f;
                float alpha = Mathf.Clamp01(Mathf.Max(axial, diamond));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        fallbackGlintSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(.5f, .5f), size);
        fallbackGlintSprite.name = "Runtime Sprite Glint";
        fallbackGlintSprite.hideFlags = HideFlags.HideAndDontSave;
        return fallbackGlintSprite;
    }
}
