using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class OverviewArrival
{
    private static bool showShopRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState() => showShopRequested = false;

    public static bool IsShopRequested => showShopRequested;

    public static void RequestShop() => showShopRequested = true;

    public static bool ConsumeShopRequest()
    {
        bool requested = showShopRequested;
        showShopRequested = false;
        return requested;
    }

    public static void Clear() => showShopRequested = false;
}

public sealed class MineLevelMenuController : MonoBehaviour
{
    public const string DefaultHomeScene = "DungeonOverview";

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resumeSelection;
    [SerializeField] private string homeScene = DefaultHomeScene;

    private LevelExitDoor exitDoor;
    private PlayerHealth playerHealth;

    public static bool IsPaused { get; private set; }
    public GameObject PausePanel => pausePanel;
    public string HomeScene => homeScene;

    public void Configure(GameObject panel, GameObject defaultResumeSelection,
        string overviewScene = DefaultHomeScene)
    {
        pausePanel = panel;
        resumeSelection = defaultResumeSelection;
        homeScene = string.IsNullOrWhiteSpace(overviewScene) ? DefaultHomeScene : overviewScene;
        ApplyPaused(false);
    }

    private void Awake()
    {
        exitDoor = FindFirstObjectByType<LevelExitDoor>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        ApplyPaused(false);
    }

    private void Update()
    {
        if (IsTransitionLocked) return;

        if (MineInput.HomePressed)
        {
            ReturnToOverview();
            return;
        }

        if (MineInput.PausePressed) TogglePause();
    }

    public void TogglePause() => SetPaused(!IsPaused);

    public void PauseGame() => SetPaused(true);

    public void ResumeGame() => SetPaused(false);

    public void SetPaused(bool paused)
    {
        if (paused && IsTransitionLocked) return;
        ApplyPaused(paused);
    }

    public void ReturnToOverview()
    {
        if (IsTransitionLocked) return;
        ApplyPaused(false);
        OverviewArrival.RequestShop();
        SceneManager.LoadScene(homeScene);
    }

    private bool IsTransitionLocked =>
        (exitDoor != null && exitDoor.IsUsed) || (playerHealth != null && !playerHealth.CanAct);

    private void ApplyPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(paused);

        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(paused ? resumeSelection : null);
    }

    private void OnDisable()
    {
        if (IsPaused) ApplyPaused(false);
    }
}
