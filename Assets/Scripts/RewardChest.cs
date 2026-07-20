using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RewardChestState
{
    Closed,
    Unlocking,
    Opening,
    Open
}

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class RewardChest : MonoBehaviour
{
    public const float BlueGemChance = 0.50f;
    public const float PotionChance = 0.45f;
    public const float ExtraLifeChance = 0.05f;
    public static string OpenPrompt =>
        $"PRESS {MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact)} / UP / W TO OPEN CHEST";
    public const string LockedPrompt = "CHEST LOCKED - BRONZE KEY REQUIRED";
    public const string OpenedPrompt = "CHEST ALREADY OPENED";

    [Header("Persistent identity")]
    [SerializeField] private string dungeonId = GameProgress.BronzeDungeonId;
    [SerializeField, Min(1)] private int levelNumber = 1;
    [SerializeField] private string chestId = GameProgress.LegacyRewardChestId;

    [Header("Opening animation")]
    [SerializeField] private Sprite unlockingSprite;
    [SerializeField] private Sprite openingSprite;
    [SerializeField] private Sprite openedSprite;
    [SerializeField, Min(0f)] private float unlockDuration = 0.18f;
    [SerializeField, Min(0f)] private float openingDuration = 0.32f;

    private readonly HashSet<Collider2D> nearbyPlayerColliders = new();
    private MineRunInventory nearbyInventory;
    private SpriteRenderer chestRenderer;
    private Sprite closedSprite;
    private Color closedColor = Color.white;
    private Vector3 restScale = Vector3.one;
    private Coroutine openingRoutine;

    public bool IsOpened { get; private set; }
    public string LastReward { get; private set; }
    public string DungeonId => dungeonId;
    public int LevelNumber => levelNumber;
    public string ChestId => chestId;
    public Sprite UnlockingSprite => unlockingSprite;
    public Sprite OpeningSprite => openingSprite;
    public Sprite OpenedSprite => openedSprite;
    public float UnlockDuration => unlockDuration;
    public float OpeningDuration => openingDuration;
    public float TotalOpeningDuration => unlockDuration + openingDuration;
    public RewardChestState State { get; private set; } = RewardChestState.Closed;
    public bool IsAnimating => State == RewardChestState.Unlocking ||
        State == RewardChestState.Opening;
    public bool IsPlayerNearby => nearbyInventory != null;

    public void Configure(int currentLevelNumber, Sprite openStateSprite = null)
    {
        Configure(GameProgress.BronzeDungeonId, currentLevelNumber,
            GameProgress.LegacyRewardChestId, openStateSprite);
    }

    public void Configure(string currentDungeonId, int currentLevelNumber,
        string uniqueChestId, Sprite openStateSprite = null)
    {
        dungeonId = GameProgress.NormalizeDungeonId(currentDungeonId);
        levelNumber = Mathf.Max(1, currentLevelNumber);
        chestId = GameProgress.NormalizeContentId(uniqueChestId,
            GameProgress.LegacyRewardChestId);
        openedSprite = openStateSprite;

        if (Application.isPlaying) ApplyConfiguredProgressState();
    }

    public void Configure(string currentDungeonId, int currentLevelNumber,
        string uniqueChestId, Sprite unlockStateSprite, Sprite openingStateSprite,
        Sprite openStateSprite, float unlockSeconds, float openingSeconds)
    {
        dungeonId = GameProgress.NormalizeDungeonId(currentDungeonId);
        levelNumber = Mathf.Max(1, currentLevelNumber);
        chestId = GameProgress.NormalizeContentId(uniqueChestId,
            GameProgress.LegacyRewardChestId);
        unlockingSprite = unlockStateSprite;
        openingSprite = openingStateSprite;
        openedSprite = openStateSprite;
        unlockDuration = Mathf.Max(0f, unlockSeconds);
        openingDuration = Mathf.Max(0f, openingSeconds);

        if (Application.isPlaying) ApplyConfiguredProgressState();
    }

    public void ConfigureAnimation(Sprite unlockStateSprite, Sprite openingStateSprite,
        Sprite openStateSprite, float unlockSeconds = 0.18f,
        float openingSeconds = 0.32f)
    {
        unlockingSprite = unlockStateSprite;
        openingSprite = openingStateSprite;
        openedSprite = openStateSprite;
        unlockDuration = Mathf.Max(0f, unlockSeconds);
        openingDuration = Mathf.Max(0f, openingSeconds);
    }

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        CachePresentation();
        dungeonId = GameProgress.NormalizeDungeonId(dungeonId);
        chestId = GameProgress.NormalizeContentId(chestId,
            GameProgress.LegacyRewardChestId);
        ApplyConfiguredProgressState();
    }

    private void Update()
    {
        if (!MineLevelMenuController.IsPaused && nearbyInventory != null &&
            MineInput.InteractPressed)
        {
            TryInteract(nearbyInventory, Random.value);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null || !inventory.MatchesScope(dungeonId, levelNumber)) return;

        nearbyPlayerColliders.Add(other);
        nearbyInventory = inventory;
        ShowInteractionPrompt(inventory);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (nearbyInventory != null) return;
        OnTriggerEnter2D(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!nearbyPlayerColliders.Remove(other) || nearbyPlayerColliders.Count > 0) return;

        MineRunInventory inventory = nearbyInventory;
        nearbyInventory = null;
        inventory?.RestoreProgressStatus();
    }

    public bool TryInteract(MineRunInventory inventory, float roll)
    {
        if (inventory == null || !inventory.MatchesScope(dungeonId, levelNumber)) return false;
        PlayerHealth health = inventory.GetComponent<PlayerHealth>();
        if (health != null && !health.CanAct) return false;

        if (IsOpened || GameProgress.IsChestOpened(dungeonId, levelNumber, chestId))
        {
            IsOpened = true;
            if (State == RewardChestState.Closed) CompleteOpeningPresentation();
            inventory.ShowMessage(OpenedPrompt);
            return false;
        }

        // This claims the unique chest and consumes exactly one scoped key
        // before any reward is awarded. A second interaction cannot win a race.
        if (!inventory.TryUnlockChest(chestId))
        {
            inventory.ShowMessage(LockedPromptForDungeon());
            return false;
        }

        IsOpened = true;
        State = RewardChestState.Unlocking;
        AwardReward(roll);
        inventory.ShowMessage($"CHEST OPENED: {LastReward}");
        BeginOpeningAnimation();
        return true;
    }

    // Compatibility wrapper for authored events and the original smoke test.
    public void Open(MineRunInventory inventory, float roll) => TryInteract(inventory, roll);

    private void ShowInteractionPrompt(MineRunInventory inventory)
    {
        if (IsOpened) inventory.ShowMessage(OpenedPrompt);
        else inventory.ShowMessage(inventory.HasKey ? OpenPrompt : LockedPromptForDungeon());
    }

    private string LockedPromptForDungeon()
    {
        if (dungeonId == GameProgress.BronzeDungeonId) return LockedPrompt;
        return $"CHEST LOCKED - {dungeonId.ToUpperInvariant()} KEY REQUIRED";
    }

    private void AwardReward(float roll)
    {
        roll = Mathf.Clamp01(roll);
        if (roll < BlueGemChance)
        {
            GameProgress.AddCrystals(5);
            LastReward = "BLUE GEM +5";
        }
        else if (roll < BlueGemChance + PotionChance)
        {
            GameProgress.AddHealthPotion();
            LastReward = "HEALTH POTION +1";
        }
        else
        {
            GameProgress.AddLife();
            LastReward = "EXTRA LIFE +1";
        }
    }

    private void BeginOpeningAnimation()
    {
        CachePresentation();
        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        State = RewardChestState.Unlocking;
        if (unlockingSprite != null) chestRenderer.sprite = unlockingSprite;

        // Existing Bronze scenes provide only an open sprite. Apply that sprite
        // immediately for compatibility, while the squash/shine still animates.
        if (unlockingSprite == null && openingSprite == null)
        {
            ApplyOpenedSprite();
        }

        if (!isActiveAndEnabled || TotalOpeningDuration <= 0f)
        {
            CompleteOpeningPresentation();
            return;
        }

        openingRoutine = StartCoroutine(OpeningRoutine());
    }

    private IEnumerator OpeningRoutine()
    {
        yield return AnimatePhase(unlockDuration, 0.045f, 0.06f);

        State = RewardChestState.Opening;
        if (openingSprite != null) chestRenderer.sprite = openingSprite;
        else ApplyOpenedSprite();

        yield return AnimatePhase(openingDuration, 0.08f, 0.12f);
        CompleteOpeningPresentation();
    }

    private IEnumerator AnimatePhase(float duration, float horizontalStretch,
        float verticalSquash)
    {
        if (duration <= 0f) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = new Vector3(
                restScale.x * (1f + pulse * horizontalStretch),
                restScale.y * (1f - pulse * verticalSquash),
                restScale.z);
            chestRenderer.color = Color.Lerp(closedColor, Color.white, 0.55f * pulse);
            yield return null;
        }
    }

    private void ApplyConfiguredProgressState()
    {
        CachePresentation();
        IsOpened = GameProgress.IsChestOpened(dungeonId, levelNumber, chestId);
        if (IsOpened)
        {
            CompleteOpeningPresentation();
            return;
        }

        State = RewardChestState.Closed;
        transform.localScale = restScale;
        if (closedSprite != null) chestRenderer.sprite = closedSprite;
        chestRenderer.color = closedColor;
    }

    private void CompleteOpeningPresentation()
    {
        CachePresentation();
        IsOpened = true;
        State = RewardChestState.Open;
        transform.localScale = restScale;
        ApplyOpenedSprite();
        GetComponent<Collider2D>().enabled = true;
        openingRoutine = null;
    }

    private void ApplyOpenedSprite()
    {
        if (openedSprite != null)
        {
            chestRenderer.sprite = openedSprite;
            chestRenderer.color = Color.white;
        }
        else
        {
            chestRenderer.color = new Color32(105, 86, 70, 255);
        }
    }

    private void CachePresentation()
    {
        if (chestRenderer == null)
        {
            chestRenderer = GetComponent<SpriteRenderer>();
            closedSprite = chestRenderer.sprite;
            closedColor = chestRenderer.color;
            restScale = transform.localScale;
        }
    }

    private void OnDisable()
    {
        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
            openingRoutine = null;
        }

        if (IsOpened) CompleteOpeningPresentation();
    }
}
