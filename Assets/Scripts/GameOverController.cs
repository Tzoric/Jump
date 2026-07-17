using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverController : MonoBehaviour
{
    [SerializeField] private string restartScene = "DungeonOverview";

    public string RestartScene => restartScene;

    private void Start()
    {
        if (!GameProgress.IsPlaytestRunActive) return;

        foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label.name == "Progress Reset Message")
            {
                label.text = "Playtest restart discards this test tunnel and restores your real save.";
                break;
            }
        }
    }

    public void RestartGame()
    {
        OverviewArrival.Clear();
        GameProgress.RestartAfterGameOver();
        SceneManager.LoadScene(restartScene);
    }

    private void Update()
    {
        if (MineInput.ConfirmPressed)
            RestartGame();
    }
}
