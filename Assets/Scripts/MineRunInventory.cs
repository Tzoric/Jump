using TMPro;
using UnityEngine;

public sealed class MineRunInventory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField, Min(1)] private int levelNumber = 1;

    public bool HasBronzeKey => GameProgress.HasBronzeKey(levelNumber);
    public int LevelNumber => levelNumber;
    public string StatusText => statusDisplay == null ? string.Empty : statusDisplay.text;
    public bool HasStatusDisplay => statusDisplay != null;

    private void OnEnable() => RefreshProgressStatus();

    public void Configure(int currentLevelNumber, TextMeshProUGUI display)
    {
        levelNumber = Mathf.Max(1, currentLevelNumber);
        statusDisplay = display;
        RefreshProgressStatus();
    }

    public void CollectBronzeKey()
    {
        GameProgress.CollectBronzeKey(levelNumber);
        ShowMessage("BRONZE KEY FOUND — FIND THE CHEST");
    }

    public bool TryUseBronzeKey()
    {
        return HasBronzeKey;
    }

    public void ShowMessage(string message)
    {
        if (statusDisplay != null) statusDisplay.text = message;
    }

    public void RestoreProgressStatus() => RefreshProgressStatus();

    private void RefreshProgressStatus()
    {
        if (statusDisplay == null) return;
        if (GameProgress.IsChestOpened(levelNumber)) statusDisplay.text = RewardChest.OpenedPrompt;
        else statusDisplay.text = HasBronzeKey
            ? "BRONZE KEY READY - FIND THE CHEST"
            : "FIND THE HIDDEN BRONZE KEY";
    }
}
