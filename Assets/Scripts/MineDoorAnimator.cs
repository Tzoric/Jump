using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates a mine-door transition so callers can wait until the door is
/// fully open before moving the miner through it.
/// </summary>
[DisallowMultipleComponent]
public sealed class MineDoorAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer closedRenderer;
    [SerializeField] private SpriteRenderer openRenderer;
    [SerializeField] private Collider2D passageBlocker;
    [SerializeField, Min(.05f)] private float openingSeconds = .34f;
    [SerializeField, Min(.05f)] private float closingSeconds = .28f;
    [SerializeField, Range(.02f, .6f)] private float fallbackOpenWidthScale = .14f;
    [SerializeField, Range(0f, 1f)] private float fallbackOpenAlpha = .3f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool startOpen;

    private Color closedColor = Color.white;
    private Color openColor = Color.white;
    private Vector3 closedScale = Vector3.one;
    private bool presentationCached;
    private bool targetOpen;
    private Coroutine activeRoutine;

    public bool IsOpen { get; private set; }
    public bool IsAnimating { get; private set; }
    public bool HasOpened { get; private set; }
    public bool CanPass => IsOpen && !IsAnimating;
    public float CurrentOpenAmount { get; private set; }
    public float OpeningSeconds => openingSeconds;
    public float ClosingSeconds => closingSeconds;
    public SpriteRenderer ClosedRenderer => closedRenderer;
    public SpriteRenderer OpenRenderer => openRenderer;
    public Collider2D PassageBlocker => passageBlocker;

    public void Configure(SpriteRenderer closedState, SpriteRenderer openState,
        float openDuration = .34f, float closeDuration = .28f, Collider2D blocker = null)
    {
        closedRenderer = closedState != null ? closedState : GetComponent<SpriteRenderer>();
        openRenderer = openState;
        openingSeconds = Mathf.Max(.05f, openDuration);
        closingSeconds = Mathf.Max(.05f, closeDuration);
        passageBlocker = blocker;
        presentationCached = false;
        CachePresentation();
        ApplyNormalizedOpenAmount(startOpen ? 1f : 0f);
    }

    public IEnumerator OpenDoor()
    {
        return AnimateDoor(true);
    }

    public IEnumerator CloseDoor()
    {
        return AnimateDoor(false);
    }

    public IEnumerator SetDoorOpen(bool shouldOpen)
    {
        return AnimateDoor(shouldOpen);
    }

    public Coroutine PlayOpen()
    {
        return PlayTransition(true);
    }

    public Coroutine PlayClose()
    {
        return PlayTransition(false);
    }

    public void SetOpenImmediate(bool shouldOpen, bool countAsOpened = true)
    {
        CachePresentation();
        targetOpen = shouldOpen;
        IsAnimating = false;
        IsOpen = shouldOpen;
        if (shouldOpen && countAsOpened) HasOpened = true;
        ApplyNormalizedOpenAmount(shouldOpen ? 1f : 0f);
        SetBlockerForOpenState(shouldOpen);
    }

    /// <summary>Applies a deterministic authored pose without changing the logical state.</summary>
    public void ApplyNormalizedOpenAmount(float amount)
    {
        CachePresentation();
        CurrentOpenAmount = Mathf.Clamp01(amount);
        float eased = Mathf.SmoothStep(0f, 1f, CurrentOpenAmount);
        bool hasTwoStates = closedRenderer != null && openRenderer != null &&
            closedRenderer != openRenderer;

        if (hasTwoStates)
        {
            closedRenderer.enabled = eased < .999f;
            openRenderer.enabled = eased > .001f;
            closedRenderer.color = WithAlpha(closedColor, closedColor.a * (1f - eased));
            openRenderer.color = WithAlpha(openColor, openColor.a * eased);
        }
        else if (closedRenderer != null)
        {
            closedRenderer.enabled = true;
            closedRenderer.color = WithAlpha(closedColor,
                closedColor.a * Mathf.Lerp(1f, fallbackOpenAlpha, eased));
            Vector3 scale = closedScale;
            scale.x *= Mathf.Lerp(1f, fallbackOpenWidthScale, eased);
            closedRenderer.transform.localScale = scale;
        }
        else if (openRenderer != null)
        {
            openRenderer.enabled = eased > .001f;
            openRenderer.color = WithAlpha(openColor, openColor.a * eased);
        }
    }

    private void Awake()
    {
        if (closedRenderer == null) closedRenderer = GetComponent<SpriteRenderer>();
        CachePresentation();
        SetOpenImmediate(startOpen);
    }

    private void OnValidate()
    {
        openingSeconds = Mathf.Max(.05f, openingSeconds);
        closingSeconds = Mathf.Max(.05f, closingSeconds);
        fallbackOpenWidthScale = Mathf.Clamp(fallbackOpenWidthScale, .02f, .6f);
        fallbackOpenAlpha = Mathf.Clamp01(fallbackOpenAlpha);
    }

    private void OnDisable()
    {
        if (!IsAnimating) return;
        SetOpenImmediate(targetOpen);
        activeRoutine = null;
    }

    private Coroutine PlayTransition(bool shouldOpen)
    {
        if (!isActiveAndEnabled)
        {
            SetOpenImmediate(shouldOpen);
            return null;
        }

        activeRoutine = StartCoroutine(AnimateDoor(shouldOpen));
        return activeRoutine;
    }

    private IEnumerator AnimateDoor(bool shouldOpen)
    {
        while (IsAnimating) yield return null;
        if (IsOpen == shouldOpen)
        {
            ApplyNormalizedOpenAmount(shouldOpen ? 1f : 0f);
            SetBlockerForOpenState(shouldOpen);
            yield break;
        }

        targetOpen = shouldOpen;
        IsAnimating = true;
        if (!shouldOpen && passageBlocker != null) passageBlocker.enabled = true;

        float startAmount = CurrentOpenAmount;
        float endAmount = shouldOpen ? 1f : 0f;
        float duration = shouldOpen ? openingSeconds : closingSeconds;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            ApplyNormalizedOpenAmount(Mathf.Lerp(startAmount, endAmount, elapsed / duration));
            yield return null;
        }

        ApplyNormalizedOpenAmount(endAmount);
        IsOpen = shouldOpen;
        IsAnimating = false;
        if (shouldOpen) HasOpened = true;
        SetBlockerForOpenState(shouldOpen);
        activeRoutine = null;
    }

    private void CachePresentation()
    {
        if (presentationCached) return;
        if (closedRenderer == null) closedRenderer = GetComponent<SpriteRenderer>();
        if (closedRenderer != null)
        {
            closedColor = closedRenderer.color;
            closedScale = closedRenderer.transform.localScale;
        }
        if (openRenderer != null) openColor = openRenderer.color;
        presentationCached = true;
    }

    private void SetBlockerForOpenState(bool shouldOpen)
    {
        if (passageBlocker != null) passageBlocker.enabled = !shouldOpen;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
