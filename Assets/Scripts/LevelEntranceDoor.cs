using System.Collections;
using UnityEngine;

public sealed class LevelEntranceDoor : MonoBehaviour
{
    [SerializeField] private HeroMovement hero;
    [SerializeField] private Vector3 gameplayPosition;
    [SerializeField, Min(.2f)] private float entranceSeconds = .9f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Animator animator;
    private MinerOutfitVisual outfit;
    private SpriteRenderer heroRenderer;
    private RigidbodyType2D originalBodyType;
    private bool originalColliderEnabled;
    private Color originalHeroColor;
    private Coroutine entranceRoutine;

    public HeroMovement Hero => hero;
    public Vector3 GameplayPosition => gameplayPosition;
    public float EntranceSeconds => entranceSeconds;
    public bool IsComplete { get; private set; }

    public void Configure(HeroMovement player, Vector3 playablePosition, float duration = .9f)
    {
        hero = player;
        gameplayPosition = playablePosition;
        entranceSeconds = Mathf.Max(.2f, duration);
    }

    private void Awake()
    {
        if (hero == null) hero = FindFirstObjectByType<HeroMovement>();
        if (hero == null)
        {
            enabled = false;
            return;
        }

        body = hero.GetComponent<Rigidbody2D>();
        bodyCollider = hero.GetComponent<Collider2D>();
        animator = hero.GetComponent<Animator>();
        outfit = hero.GetComponent<MinerOutfitVisual>();
        heroRenderer = outfit != null && outfit.VisualRenderer != null
            ? outfit.VisualRenderer
            : hero.GetComponent<SpriteRenderer>();
        originalBodyType = body == null ? RigidbodyType2D.Dynamic : body.bodyType;
        originalColliderEnabled = bodyCollider == null || bodyCollider.enabled;
        originalHeroColor = heroRenderer == null ? Color.white : heroRenderer.color;

        // The entrance is presentation-only. Batch and focused playtests need the
        // authored spawn immediately so the cinematic cannot undo their teleport.
        if (Application.isBatchMode)
        {
            CompleteEntrance();
            enabled = false;
            return;
        }

        hero.SetInputLocked(true);
        hero.GetComponent<PlayerHealth>()?.RestoreDamagePresentation();
        hero.transform.position = new Vector3(transform.position.x, transform.position.y - .2f,
            gameplayPosition.z);
        if (heroRenderer != null)
            heroRenderer.color = new Color(originalHeroColor.r, originalHeroColor.g,
                originalHeroColor.b, 0f);
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        if (animator != null) animator.SetFloat("Speed", 1f);
        outfit?.PlayWalkTowardCamera();
    }

    private void Start()
    {
        if (hero != null) entranceRoutine = StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        Vector3 start = hero.transform.position;
        float elapsed = 0f;
        while (elapsed < entranceSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceSeconds);
            hero.transform.position = Vector3.Lerp(start, gameplayPosition, t);
            if (heroRenderer != null)
                heroRenderer.color = new Color(originalHeroColor.r, originalHeroColor.g,
                    originalHeroColor.b, originalHeroColor.a * t);
            yield return null;
        }

        CompleteEntrance();
        entranceRoutine = null;
    }

    private void CompleteEntrance()
    {
        if (hero == null || IsComplete) return;
        hero.transform.position = gameplayPosition;
        if (body != null)
        {
            body.position = gameplayPosition;
            body.bodyType = originalBodyType;
            body.linearVelocity = Vector2.zero;
        }
        if (bodyCollider != null) bodyCollider.enabled = originalColliderEnabled;
        if (heroRenderer != null) heroRenderer.color = originalHeroColor;
        if (animator != null) animator.SetFloat("Speed", 0f);
        outfit?.ClearPerspectiveOverride();
        hero.GetComponent<PlayerHealth>()?.SetCheckpoint(gameplayPosition);
        hero.SetInputLocked(false);
        IsComplete = true;
    }

    private void OnDisable()
    {
        if (entranceRoutine != null)
        {
            StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }
        CompleteEntrance();
    }
}
