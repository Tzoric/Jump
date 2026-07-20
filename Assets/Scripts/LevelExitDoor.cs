using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelExitDoor : MonoBehaviour
{
    public static string ExitPrompt =>
        $"PRESS {MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact)} / UP / W TO EXIT LEVEL";

    [SerializeField] private string destinationScene = "DungeonOverview";
    [SerializeField, Min(0.2f)] private float entranceSeconds = 0.9f;
    [SerializeField, Min(0)] private int levelNumber;
    [SerializeField] private MineDoorAnimator doorAnimator;

    private readonly HashSet<Collider2D> nearbyPlayerColliders = new();
    private HeroMovement nearbyHero;
    private MineRunInventory nearbyInventory;
    private Coroutine entranceRoutine;
    private HeroMovement transitioningHero;
    private Rigidbody2D transitioningBody;
    private Collider2D transitioningCollider;
    private Animator transitioningAnimator;
    private MinerOutfitVisual transitioningOutfit;
    private SpriteRenderer transitioningRenderer;
    private RigidbodyType2D originalBodyType;
    private bool originalColliderEnabled;
    private Color originalHeroColor = Color.white;
    private Vector3 originalHeroPosition;
    private bool sceneLoadCommitted;

    public string DestinationScene => destinationScene;
    public MineDoorAnimator DoorAnimator => doorAnimator;
    public bool IsUsed { get; private set; }
    public bool IsPlayerNearby => nearbyHero != null;
    public int LevelNumber => levelNumber;
    public bool TraversalStarted { get; private set; }
    public bool DoorOpenedBeforeTraversal { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (doorAnimator == null) doorAnimator = GetComponent<MineDoorAnimator>();
    }

    private void Update()
    {
        if (!MineLevelMenuController.IsPaused && nearbyHero != null && MineInput.InteractPressed)
        {
            TryInteract(nearbyHero);
        }
    }

    public void Configure(string sceneName)
    {
        destinationScene = sceneName;
    }

    public void Configure(string sceneName, int completedLevelNumber)
    {
        destinationScene = sceneName;
        levelNumber = Mathf.Max(0, completedLevelNumber);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterNearbyPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!nearbyPlayerColliders.Contains(other)) RegisterNearbyPlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!nearbyPlayerColliders.Remove(other) || nearbyPlayerColliders.Count > 0) return;

        MineRunInventory inventory = nearbyInventory;
        nearbyHero = null;
        nearbyInventory = null;
        if (!IsUsed) inventory?.RestoreProgressStatus();
    }

    private void OnDisable()
    {
        if (entranceRoutine != null)
        {
            StopCoroutine(entranceRoutine);
            entranceRoutine = null;
        }
        if (doorAnimator != null) doorAnimator.SetOpenImmediate(false);
        if (!sceneLoadCommitted) RestoreInterruptedTraversal();
        if (!IsUsed) nearbyInventory?.RestoreProgressStatus();
        nearbyPlayerColliders.Clear();
        nearbyHero = null;
        nearbyInventory = null;
    }

    private void RegisterNearbyPlayer(Collider2D other)
    {
        HeroMovement hero = other.GetComponentInParent<HeroMovement>();
        if (IsUsed || hero == null || (nearbyHero != null && nearbyHero != hero)) return;

        nearbyPlayerColliders.Add(other);
        nearbyHero = hero;
        nearbyInventory = hero.GetComponent<MineRunInventory>();
        nearbyInventory?.ShowMessage(ExitPrompt);
    }

    public bool TryInteract(HeroMovement hero)
    {
        PlayerHealth health = hero == null ? null : hero.GetComponent<PlayerHealth>();
        if (IsUsed || hero == null || hero != nearbyHero || !hero.IsGrounded ||
            (health != null && !health.CanAct))
        {
            return false;
        }

        IsUsed = true;
        entranceRoutine = StartCoroutine(EnterDoorRoutine(hero));
        return true;
    }

    private IEnumerator EnterDoorRoutine(HeroMovement hero)
    {
        transitioningHero = hero;
        hero.SetInputLocked(true);
        // Door entry owns the intentional fade. Cancel any in-progress damage
        // flash first so it cannot overwrite the cutscene opacity.
        hero.GetComponent<PlayerHealth>()?.RestoreDamagePresentation();
        transitioningBody = hero.GetComponent<Rigidbody2D>();
        transitioningCollider = hero.GetComponent<Collider2D>();
        transitioningAnimator = hero.GetComponent<Animator>();
        transitioningOutfit = hero.GetComponent<MinerOutfitVisual>();
        transitioningRenderer = transitioningOutfit != null && transitioningOutfit.VisualRenderer != null
            ? transitioningOutfit.VisualRenderer
            : hero.GetComponent<SpriteRenderer>();
        originalBodyType = transitioningBody == null
            ? RigidbodyType2D.Dynamic
            : transitioningBody.bodyType;
        originalColliderEnabled = transitioningCollider == null || transitioningCollider.enabled;
        originalHeroColor = transitioningRenderer == null ? Color.white : transitioningRenderer.color;
        originalHeroPosition = hero.transform.position;

        if (transitioningCollider != null) transitioningCollider.enabled = false;
        if (transitioningBody != null)
        {
            transitioningBody.linearVelocity = Vector2.zero;
            transitioningBody.bodyType = RigidbodyType2D.Kinematic;
        }

        if (transitioningAnimator != null) transitioningAnimator.SetFloat("Speed", 0f);
        if (doorAnimator != null)
        {
            if (doorAnimator.isActiveAndEnabled)
                yield return doorAnimator.OpenDoor();
            else
                doorAnimator.SetOpenImmediate(true);

            DoorOpenedBeforeTraversal = doorAnimator.CanPass;
        }

        TraversalStarted = true;
        if (transitioningAnimator != null) transitioningAnimator.SetFloat("Speed", 1f);
        if (transitioningOutfit != null) transitioningOutfit.PlayWalkAway();

        Vector3 start = hero.transform.position;
        Vector3 destination = new(transform.position.x, transform.position.y - 0.65f, start.z);
        float elapsed = 0f;
        while (elapsed < entranceSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceSeconds);
            hero.transform.position = Vector3.Lerp(start, destination, t);
            if (transitioningRenderer != null)
                transitioningRenderer.color = new Color(originalHeroColor.r, originalHeroColor.g,
                    originalHeroColor.b, originalHeroColor.a * (1f - t));
            yield return null;
        }

        if (transitioningAnimator != null) transitioningAnimator.SetFloat("Speed", 0f);
        if (doorAnimator != null)
        {
            if (doorAnimator.isActiveAndEnabled)
                yield return doorAnimator.CloseDoor();
            else
                doorAnimator.SetOpenImmediate(false);
        }

        OverviewArrival.Clear();
        GameProgress.CompleteLevel(levelNumber);
        sceneLoadCommitted = true;
        entranceRoutine = null;
        SceneManager.LoadScene(destinationScene);
    }

    private void RestoreInterruptedTraversal()
    {
        if (transitioningHero == null) return;

        transitioningHero.transform.position = originalHeroPosition;
        transitioningHero.SetInputLocked(false);
        if (transitioningBody != null)
        {
            transitioningBody.position = originalHeroPosition;
            transitioningBody.bodyType = originalBodyType;
            transitioningBody.linearVelocity = Vector2.zero;
        }
        if (transitioningCollider != null) transitioningCollider.enabled = originalColliderEnabled;
        if (transitioningRenderer != null) transitioningRenderer.color = originalHeroColor;
        if (transitioningAnimator != null) transitioningAnimator.SetFloat("Speed", 0f);
        transitioningOutfit?.ClearPerspectiveOverride();
        transitioningHero.GetComponent<PlayerHealth>()?.RestoreDamagePresentation();

        transitioningHero = null;
        transitioningBody = null;
        transitioningCollider = null;
        transitioningAnimator = null;
        transitioningOutfit = null;
        transitioningRenderer = null;
        IsUsed = false;
        TraversalStarted = false;
        DoorOpenedBeforeTraversal = false;
    }
}
