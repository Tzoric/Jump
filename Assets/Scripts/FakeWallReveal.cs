using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A secret-room wall that looks like the surrounding rock but never blocks the
/// player. Optionally, it becomes slightly translucent while the miner is inside.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FakeWallReveal : MonoBehaviour
{
    [SerializeField] private SpriteRenderer primaryRenderer;
    [SerializeField] private Transform trackedPlayer;
    [SerializeField] private bool fadeWhilePassing = true;
    [SerializeField, Range(.2f, 1f)] private float passingOpacity = .62f;
    [SerializeField, Min(.02f)] private float fadeSeconds = .16f;
    [SerializeField, Min(.02f)] private float restoreSeconds = .3f;
    [SerializeField, Min(0f)] private float detectionPadding = .08f;

    private readonly HashSet<Collider2D> touchingPlayerColliders = new();
    private SpriteRenderer[] visualRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] opaqueColors = System.Array.Empty<Color>();
    private ThemedMetalFlakes[] metalFlakeOverlays = System.Array.Empty<ThemedMetalFlakes>();
    private float opacityMultiplier = 1f;

    public bool IsPlayerInside { get; private set; }
    public bool IsCollisionFree { get; private set; }
    public float CurrentOpacityMultiplier => opacityMultiplier;
    public bool FadeWhilePassing => fadeWhilePassing;

    public void Configure(SpriteRenderer wallRenderer, bool fade = true,
        float fadedOpacity = .62f, float fadeDuration = .16f, float restoreDuration = .3f)
    {
        primaryRenderer = wallRenderer != null ? wallRenderer : GetComponent<SpriteRenderer>();
        fadeWhilePassing = fade;
        passingOpacity = Mathf.Clamp(fadedOpacity, .2f, 1f);
        fadeSeconds = Mathf.Max(.02f, fadeDuration);
        restoreSeconds = Mathf.Max(.02f, restoreDuration);
        CaptureVisuals();
        MakeCollidersNonBlocking();
        SetOpacityImmediate(1f);
    }

    public void SetTrackedPlayer(Transform player)
    {
        trackedPlayer = player;
    }

    public void SetOpacityImmediate(float multiplier)
    {
        opacityMultiplier = Mathf.Clamp01(multiplier);
        ApplyOpacity();
    }

    public void RestoreVisualImmediately()
    {
        IsPlayerInside = false;
        SetOpacityImmediate(1f);
    }

    private void Awake()
    {
        if (primaryRenderer == null) primaryRenderer = GetComponent<SpriteRenderer>();
        CaptureVisuals();
        MakeCollidersNonBlocking();
        SetOpacityImmediate(1f);
    }

    private void OnEnable()
    {
        MakeCollidersNonBlocking();
    }

    private void OnDisable()
    {
        touchingPlayerColliders.Clear();
        RestoreVisualImmediately();
    }

    private void OnValidate()
    {
        passingOpacity = Mathf.Clamp(passingOpacity, .2f, 1f);
        fadeSeconds = Mathf.Max(.02f, fadeSeconds);
        restoreSeconds = Mathf.Max(.02f, restoreSeconds);
        detectionPadding = Mathf.Max(0f, detectionPadding);
    }

    private void Update()
    {
        if (trackedPlayer == null)
        {
            HeroMovement hero = FindFirstObjectByType<HeroMovement>();
            if (hero != null) trackedPlayer = hero.transform;
        }

        IsPlayerInside = touchingPlayerColliders.Count > 0 || IsTrackedPlayerWithinWallBounds();
        float target = fadeWhilePassing && IsPlayerInside ? passingOpacity : 1f;
        float duration = target < opacityMultiplier ? fadeSeconds : restoreSeconds;
        opacityMultiplier = Mathf.MoveTowards(opacityMultiplier, target,
            Time.deltaTime / Mathf.Max(.02f, duration));
        ApplyOpacity();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<HeroMovement>() != null)
            touchingPlayerColliders.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        touchingPlayerColliders.Remove(other);
    }

    private bool IsTrackedPlayerWithinWallBounds()
    {
        if (trackedPlayer == null || primaryRenderer == null) return false;
        Bounds bounds = primaryRenderer.bounds;
        Vector3 point = trackedPlayer.position;
        return point.x >= bounds.min.x - detectionPadding && point.x <= bounds.max.x + detectionPadding &&
               point.y >= bounds.min.y - detectionPadding && point.y <= bounds.max.y + detectionPadding;
    }

    private void CaptureVisuals()
    {
        visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        opaqueColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++)
            opaqueColors[i] = visualRenderers[i].color;
        metalFlakeOverlays = GetComponentsInChildren<ThemedMetalFlakes>(true);
    }

    private void MakeCollidersNonBlocking()
    {
        Collider2D[] wallColliders = GetComponentsInChildren<Collider2D>(true);
        IsCollisionFree = true;
        for (int i = 0; i < wallColliders.Length; i++)
        {
            Collider2D wallCollider = wallColliders[i];
            if (wallCollider == null) continue;
            wallCollider.isTrigger = true;
            IsCollisionFree &= !wallCollider.enabled || wallCollider.isTrigger;
        }
    }

    private void ApplyOpacity()
    {
        int rendererCount = Mathf.Min(visualRenderers.Length, opaqueColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            SpriteRenderer renderer = visualRenderers[i];
            if (renderer == null) continue;
            Color color = opaqueColors[i];
            color.a *= opacityMultiplier;
            renderer.color = color;
        }

        for (int i = 0; i < metalFlakeOverlays.Length; i++)
        {
            if (metalFlakeOverlays[i] != null)
                metalFlakeOverlays[i].SetVisibilityMultiplier(opacityMultiplier);
        }
    }
}
