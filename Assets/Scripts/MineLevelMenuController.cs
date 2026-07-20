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
    [SerializeField] private MidLevelShopController midLevelShop;

    private LevelExitDoor exitDoor;
    private PlayerHealth playerHealth;
    private bool pauseVisible;
    private bool ownsTimeScale;
    private float timeScaleBeforeOverlay = 1f;

    public static bool IsPaused { get; private set; }
    public GameObject PausePanel => pausePanel;
    public string HomeScene => homeScene;
    public MidLevelShopController MidLevelShop => midLevelShop;
    public bool IsPauseVisible => pauseVisible;
    public bool IsShopVisible => midLevelShop != null && midLevelShop.IsShopVisible;

    /// <summary>
    /// Legacy-compatible level menu setup. A shop can be attached afterward by
    /// calling MidLevelShopController.Configure or RegisterMidLevelShop.
    /// </summary>
    public void Configure(GameObject panel, GameObject defaultResumeSelection,
        string overviewScene = DefaultHomeScene)
    {
        pausePanel = panel;
        resumeSelection = defaultResumeSelection;
        homeScene = string.IsNullOrWhiteSpace(overviewScene) ? DefaultHomeScene : overviewScene;
        CloseAllOverlays();
    }

    private void Awake()
    {
        exitDoor = FindFirstObjectByType<LevelExitDoor>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (midLevelShop != null) midLevelShop.RegisterWithMenu(this);
        CloseAllOverlays();
    }

    private void Update()
    {
        if (IsTransitionLocked) return;

        if (MineInput.HomePressed)
        {
            // Authored mine levels receive an in-place shop. Keeping the old
            // destination fallback means older/test scenes without one retain
            // their established Home/Back behavior.
            if (midLevelShop != null && midLevelShop.CanOpen)
                ToggleShop();
            else
                ReturnToOverview();
            return;
        }

        if (!MineInput.PausePressed) return;

        // Escape/Start closes the shop instead of replacing it with the pause
        // panel in the same frame. A second press can then open Pause normally.
        if (IsShopVisible)
            CloseShop();
        else
            TogglePause();
    }

    public void RegisterMidLevelShop(MidLevelShopController shop)
    {
        if (shop == null) return;
        if (midLevelShop != null && midLevelShop != shop)
            midLevelShop.SetVisibleFromMenu(false);

        midLevelShop = shop;
        midLevelShop.SetVisibleFromMenu(false);
    }

    public void TogglePause()
    {
        if (IsShopVisible)
        {
            CloseShop();
            return;
        }

        SetPaused(!pauseVisible);
    }

    public void PauseGame() => SetPaused(true);

    public void ResumeGame() => CloseAllOverlays();

    public void SetPaused(bool paused)
    {
        if (paused && IsTransitionLocked) return;

        if (paused)
        {
            SetShopVisible(false);
            SetPauseVisible(true);
            SetSimulationPaused(true);
            Select(resumeSelection);
        }
        else
        {
            // SetPaused(false) keeps its original "resume everything" meaning,
            // which is useful for existing Resume button listeners and tests.
            CloseAllOverlays();
        }
    }

    public void ToggleShop()
    {
        if (IsShopVisible) CloseShop();
        else OpenShop();
    }

    public void OpenShop()
    {
        if (IsTransitionLocked || midLevelShop == null || !midLevelShop.CanOpen) return;

        SetPauseVisible(false);
        SetSimulationPaused(true);
        SetShopVisible(true);
    }

    public void CloseShop()
    {
        if (midLevelShop == null || pauseVisible) return;
        SetShopVisible(false);
        SetSimulationPaused(false);
        Select(null);
    }

    public void ReturnToOverview()
    {
        if (IsTransitionLocked) return;
        CloseAllOverlays();
        OverviewArrival.RequestShop();
        SceneManager.LoadScene(homeScene);
    }

    private bool IsTransitionLocked =>
        (exitDoor != null && exitDoor.IsUsed) || (playerHealth != null && !playerHealth.CanAct);

    private void CloseAllOverlays()
    {
        SetPauseVisible(false);
        SetShopVisible(false);
        SetSimulationPaused(false);
        Select(null);
    }

    private void SetPauseVisible(bool visible)
    {
        pauseVisible = visible;
        if (pausePanel != null) pausePanel.SetActive(visible);
    }

    private void SetShopVisible(bool visible)
    {
        if (midLevelShop != null) midLevelShop.SetVisibleFromMenu(visible);
    }

    private void SetSimulationPaused(bool paused)
    {
        if (paused)
        {
            if (!ownsTimeScale)
            {
                timeScaleBeforeOverlay = Time.timeScale;
                ownsTimeScale = true;
            }

            Time.timeScale = 0f;
            IsPaused = true;
            return;
        }

        if (ownsTimeScale)
        {
            // Only restore the value this menu replaced. This avoids forcing a
            // custom slow-motion scale back to 1 when the overlay closes.
            Time.timeScale = timeScaleBeforeOverlay;
            ownsTimeScale = false;
        }

        IsPaused = false;
    }

    private static void Select(GameObject selection)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(
            selection != null && selection.activeInHierarchy ? selection : null);
    }

    private void OnDisable()
    {
        CloseAllOverlays();
    }
}
