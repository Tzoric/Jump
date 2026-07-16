using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MineLevelSelectButton : MonoBehaviour
{
    [SerializeField, Min(1)] private int levelNumber = 1;
    [SerializeField] private string targetScene;
    [SerializeField] private string levelName;
    [SerializeField] private TextMeshProUGUI label;

    private Button button;

    public int LevelNumber => levelNumber;
    public string TargetScene => targetScene;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
        Refresh();
    }

    private void OnEnable() => Refresh();

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(LoadLevel);
    }

    public void Configure(int number, string sceneName, string displayName, TextMeshProUGUI displayLabel)
    {
        levelNumber = Mathf.Max(1, number);
        targetScene = sceneName;
        levelName = displayName;
        label = displayLabel;
        Refresh();
    }

    public void Refresh()
    {
        button ??= GetComponent<Button>();
        bool unlocked = GameProgress.IsLevelUnlocked(levelNumber);
        button.interactable = unlocked;
        if (label == null) return;
        string lockText = "LOCKED";
        if (levelNumber == 11)
            lockText = GameProgress.HighestUnlockedLevel < 11 ? "FINISH LEVEL 10" : "SILVER KEY";
        else if (levelNumber == 12)
            lockText = "FINISH LEVEL 11";
        label.text = unlocked ? $"{levelNumber}\n{levelName}" : $"{levelNumber}\n{lockText}";
    }

    private void LoadLevel()
    {
        if (GameProgress.IsLevelUnlocked(levelNumber) && !string.IsNullOrWhiteSpace(targetScene))
            SceneManager.LoadScene(targetScene);
    }
}
