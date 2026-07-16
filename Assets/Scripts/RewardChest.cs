using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class RewardChest : MonoBehaviour
{
    public const float BlueGemChance = 0.50f;
    public const float PotionChance = 0.45f;
    public const float ExtraLifeChance = 0.05f;
    public const string OpenPrompt = "PRESS X / UP / W TO OPEN CHEST";
    public const string LockedPrompt = "CHEST LOCKED - BRONZE KEY REQUIRED";
    public const string OpenedPrompt = "CHEST ALREADY OPENED";

    [SerializeField, Min(1)] private int levelNumber = 1;
    [SerializeField] private Sprite openedSprite;

    private readonly HashSet<Collider2D> nearbyPlayerColliders = new();
    private MineRunInventory nearbyInventory;

    public bool IsOpened { get; private set; }
    public string LastReward { get; private set; }
    public int LevelNumber => levelNumber;
    public Sprite OpenedSprite => openedSprite;
    public bool IsPlayerNearby => nearbyInventory != null;

    public void Configure(int currentLevelNumber, Sprite openStateSprite = null)
    {
        levelNumber = Mathf.Max(1, currentLevelNumber);
        openedSprite = openStateSprite;
        if (Application.isPlaying)
        {
            IsOpened = GameProgress.IsChestOpened(levelNumber);
            if (IsOpened) ApplyOpenedPresentation();
        }
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        IsOpened = GameProgress.IsChestOpened(levelNumber);
        if (IsOpened) ApplyOpenedPresentation();
    }

    private void Update()
    {
        if (!MineLevelMenuController.IsPaused && nearbyInventory != null && MineInput.InteractPressed)
        {
            TryInteract(nearbyInventory, Random.value);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null || inventory.LevelNumber != levelNumber) return;

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
        if (inventory == null || inventory.LevelNumber != levelNumber) return false;
        PlayerHealth health = inventory.GetComponent<PlayerHealth>();
        if (health != null && !health.CanAct) return false;
        if (IsOpened)
        {
            inventory.ShowMessage(OpenedPrompt);
            return false;
        }

        if (!inventory.TryUseBronzeKey())
        {
            inventory.ShowMessage(LockedPrompt);
            return false;
        }

        AwardReward(roll);
        GameProgress.MarkChestOpened(levelNumber);
        IsOpened = true;
        ApplyOpenedPresentation();
        inventory.ShowMessage($"CHEST OPENED: {LastReward}");
        return true;
    }

    // Kept as a small compatibility wrapper for any authored events or tests that
    // already call Open directly. Normal gameplay uses TryInteract after X/Up/W.
    public void Open(MineRunInventory inventory, float roll) => TryInteract(inventory, roll);

    private void ShowInteractionPrompt(MineRunInventory inventory)
    {
        if (IsOpened) inventory.ShowMessage(OpenedPrompt);
        else inventory.ShowMessage(inventory.HasBronzeKey ? OpenPrompt : LockedPrompt);
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

    private void ApplyOpenedPresentation()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (openedSprite != null)
        {
            renderer.sprite = openedSprite;
            renderer.color = Color.white;
        }
        else
        {
            renderer.color = new Color32(105, 86, 70, 255);
        }

        // Keep the trigger active so replayed levels explain that this one-time
        // reward was already claimed instead of silently behaving like a broken chest.
        GetComponent<Collider2D>().enabled = true;
    }
}
