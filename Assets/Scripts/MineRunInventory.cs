using TMPro;
using UnityEngine;

public sealed class MineRunInventory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private string dungeonId = GameProgress.BronzeDungeonId;
    [SerializeField, Min(1)] private int levelNumber = 1;

    // HasBronzeKey remains as a source-compatible alias for older Bronze
    // scenes. Inventory instances are now scoped to their configured dungeon.
    public bool HasBronzeKey => HasKey;
    public bool HasKey => KeyCount > 0;
    public int KeyCount => GameProgress.GetKeyCount(dungeonId, levelNumber);
    public string DungeonId => dungeonId;
    public int LevelNumber => levelNumber;
    public string StatusText => statusDisplay == null ? string.Empty : statusDisplay.text;
    public bool HasStatusDisplay => statusDisplay != null;

    private string KeyDisplayName => dungeonId.ToUpperInvariant();

    private void OnEnable() => RefreshProgressStatus();

    public void Configure(int currentLevelNumber, TextMeshProUGUI display)
    {
        dungeonId = GameProgress.BronzeDungeonId;
        levelNumber = Mathf.Max(1, currentLevelNumber);
        statusDisplay = display;
        RefreshProgressStatus();
    }

    public void Configure(string currentDungeonId, int currentLevelNumber,
        TextMeshProUGUI display)
    {
        dungeonId = GameProgress.NormalizeDungeonId(currentDungeonId);
        levelNumber = Mathf.Max(1, currentLevelNumber);
        statusDisplay = display;
        RefreshProgressStatus();
    }

    public void CollectBronzeKey()
    {
        if (dungeonId == GameProgress.BronzeDungeonId)
        {
            GameProgress.CollectBronzeKey(levelNumber);
        }
        else
        {
            GameProgress.TryCollectKey(dungeonId, levelNumber,
                GameProgress.LegacyBronzeKeyPickupId);
        }

        ShowKeyCollectedMessage();
    }

    // The legacy method now genuinely consumes one key, as its name promises.
    // Chests use TryUnlockChest so consumption and the durable chest claim are
    // committed together.
    public bool TryUseBronzeKey()
    {
        return TryUseKey();
    }

    public bool TryCollectKey(string pickupDungeonId, int pickupLevelNumber,
        string pickupId)
    {
        if (!MatchesScope(pickupDungeonId, pickupLevelNumber)) return false;
        bool collected = GameProgress.TryCollectKey(dungeonId, levelNumber, pickupId);
        if (collected) ShowKeyCollectedMessage();
        return collected;
    }

    public bool TryUseKey()
    {
        bool consumed = GameProgress.TryConsumeKey(dungeonId, levelNumber);
        if (consumed) RefreshProgressStatus();
        return consumed;
    }

    public bool TryUnlockChest(string chestId)
    {
        return GameProgress.TryUnlockChest(dungeonId, levelNumber, chestId);
    }

    public bool MatchesScope(string otherDungeonId, int otherLevelNumber)
    {
        return levelNumber == Mathf.Max(1, otherLevelNumber) &&
            dungeonId == GameProgress.NormalizeDungeonId(otherDungeonId);
    }

    public void ShowMessage(string message)
    {
        if (statusDisplay != null) statusDisplay.text = message;
    }

    public void RestoreProgressStatus() => RefreshProgressStatus();

    private void RefreshProgressStatus()
    {
        if (statusDisplay == null) return;

        bool legacyBronzeLevel = dungeonId == GameProgress.BronzeDungeonId;
        if (legacyBronzeLevel && GameProgress.IsChestOpened(levelNumber))
        {
            statusDisplay.text = RewardChest.OpenedPrompt;
            return;
        }

        int keyCount = KeyCount;
        statusDisplay.text = keyCount > 0
            ? $"{KeyDisplayName} KEYS  {keyCount} - FIND A CHEST"
            : $"FIND A HIDDEN {KeyDisplayName} KEY";
    }

    private void ShowKeyCollectedMessage()
    {
        ShowMessage($"{KeyDisplayName} KEY FOUND - KEYS  {KeyCount}");
    }
}
