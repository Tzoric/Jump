using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ends the temporary MINER sandbox whenever a lightweight dungeon overview is entered.
/// The Bronze overview gets the same behavior from MineShopController; Silver uses this
/// boundary because its test overview intentionally has no separate overview shop page.
/// </summary>
public sealed class DungeonOverviewBoundary : MonoBehaviour
{
    private void OnEnable()
    {
        GameProgress.EndPlaytestRun();
        OverviewArrival.Clear();

        if (GameProgress.Lives <= 0)
        {
            SceneManager.LoadScene("GameOver");
        }
    }
}
