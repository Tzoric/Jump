using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelExitDoor : MonoBehaviour
{
    public const string ExitPrompt = "PRESS X / UP / W TO EXIT LEVEL";

    [SerializeField] private string destinationScene = "DungeonOverview";
    [SerializeField, Min(0.2f)] private float entranceSeconds = 0.9f;
    [SerializeField, Min(0)] private int levelNumber;

    private readonly HashSet<Collider2D> nearbyPlayerColliders = new();
    private HeroMovement nearbyHero;
    private MineRunInventory nearbyInventory;

    public string DestinationScene => destinationScene;
    public bool IsUsed { get; private set; }
    public bool IsPlayerNearby => nearbyHero != null;
    public int LevelNumber => levelNumber;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
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
        StartCoroutine(EnterDoorRoutine(hero));
        return true;
    }

    private IEnumerator EnterDoorRoutine(HeroMovement hero)
    {
        hero.SetInputLocked(true);
        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        Collider2D bodyCollider = hero.GetComponent<Collider2D>();
        Animator animator = hero.GetComponent<Animator>();
        MinerOutfitVisual outfit = hero.GetComponent<MinerOutfitVisual>();
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        if (animator != null) animator.SetFloat("Speed", 1f);
        if (outfit != null) outfit.PlayWalkAway();

        Vector3 start = hero.transform.position;
        Vector3 destination = new(transform.position.x, transform.position.y - 0.65f, start.z);
        SpriteRenderer renderer = outfit != null && outfit.VisualRenderer != null
            ? outfit.VisualRenderer
            : hero.GetComponent<SpriteRenderer>();
        Color original = renderer == null ? Color.white : renderer.color;
        float elapsed = 0f;
        while (elapsed < entranceSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceSeconds);
            hero.transform.position = Vector3.Lerp(start, destination, t);
            if (renderer != null) renderer.color = new Color(original.r, original.g, original.b, 1f - t);
            yield return null;
        }

        OverviewArrival.Clear();
        GameProgress.CompleteLevel(levelNumber);
        SceneManager.LoadScene(destinationScene);
    }
}
