using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(DamageZone))]
public sealed class ProximityHiddenHazard : MonoBehaviour
{
    [SerializeField, Min(4f)] private float revealDistance = 10f;
    [SerializeField, Min(0.25f)] private float warningSeconds = 0.6f;
    [SerializeField, Range(0f, 0.25f)] private float hiddenAlpha = 0.08f;

    private SpriteRenderer spriteRenderer;
    private Collider2D damageCollider;
    private HeroMovement hero;
    private Color visibleColor;
    private float warningStartedAt = -1f;

    public float RevealDistance => revealDistance;
    public float WarningSeconds => warningSeconds;
    public bool IsRevealed => warningStartedAt >= 0f;
    public bool IsArmed => damageCollider != null && damageCollider.enabled;

    public void Configure(float distance, float warning)
    {
        revealDistance = Mathf.Max(4f, distance);
        warningSeconds = Mathf.Max(.25f, warning);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        damageCollider = GetComponent<Collider2D>();
        visibleColor = spriteRenderer.color;
        spriteRenderer.color = new Color(visibleColor.r, visibleColor.g, visibleColor.b, hiddenAlpha);
        damageCollider.enabled = false;
    }

    private void Update()
    {
        hero ??= FindFirstObjectByType<HeroMovement>();
        if (hero == null) return;

        if (warningStartedAt < 0f && Vector2.Distance(hero.transform.position, transform.position) <= revealDistance)
        {
            warningStartedAt = Time.time;
        }

        if (warningStartedAt < 0f) return;

        float progress = Mathf.Clamp01((Time.time - warningStartedAt) / warningSeconds);
        float pulse = progress < 1f ? .12f * Mathf.Sin(Time.time * 28f) : 0f;
        spriteRenderer.color = new Color(visibleColor.r, visibleColor.g, visibleColor.b,
            Mathf.Clamp01(Mathf.Lerp(hiddenAlpha, 1f, progress) + pulse));
        if (progress >= 1f) damageCollider.enabled = true;
    }
}
