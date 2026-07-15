using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameOverController : MonoBehaviour
{
    [SerializeField] private string restartScene = "DungeonOverview";

    public string RestartScene => restartScene;

    public void RestartGame()
    {
        GameProgress.RestartAfterGameOver();
        SceneManager.LoadScene(restartScene);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            RestartGame();
    }
}
