using TMPro;
using UnityEngine;

public sealed class MineRunInventory : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField, Min(1)] private int levelNumber = 1;

    public bool HasBronzeKey => GameProgress.HasBronzeKey(levelNumber);
    public int LevelNumber => levelNumber;

    public void Configure(int currentLevelNumber, TextMeshProUGUI display)
    {
        levelNumber = Mathf.Max(1, currentLevelNumber);
        statusDisplay = display;
        RefreshKeyStatus();
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

    private void RefreshKeyStatus()
    {
        if (statusDisplay != null) statusDisplay.text = HasBronzeKey ? "BRONZE KEY READY" : "FIND THE HIDDEN BRONZE KEY";
    }
}
