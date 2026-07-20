using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// An in-scene shop overlay. It deliberately contains no movement or scene
/// reload logic, so opening it preserves the hero and every level object in
/// place. MineLevelMenuController owns modal state and time scale.
/// </summary>
public sealed class MidLevelShopController : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI balanceDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private MineLevelMenuController levelMenu;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject defaultSelection;

    private bool isShopVisible;

    public GameObject ShopPanel => shopPanel;
    public TextMeshProUGUI BalanceDisplay => balanceDisplay;
    public TextMeshProUGUI StatusDisplay => statusDisplay;
    public MineLevelMenuController LevelMenu => levelMenu;
    public PlayerHealth PlayerHealth => playerHealth;
    public GameObject DefaultSelection => defaultSelection;
    public bool IsShopVisible => isShopVisible && shopPanel != null && shopPanel.activeSelf;
    public bool CanOpen => shopPanel != null;

    /// <summary>
    /// Wires a level-local shop. The component should live beside the menu
    /// controller (normally on the HUD canvas), not on shopPanel itself.
    /// </summary>
    public void Configure(GameObject panel, TextMeshProUGUI balance,
        TextMeshProUGUI status, MineLevelMenuController menu,
        PlayerHealth heroHealth, GameObject firstSelection = null)
    {
        shopPanel = panel;
        balanceDisplay = balance;
        statusDisplay = status;
        levelMenu = menu;
        playerHealth = heroHealth;
        defaultSelection = firstSelection;

        if (levelMenu != null) levelMenu.RegisterMidLevelShop(this);
        else SetVisibleFromMenu(false);
        Refresh();
    }

    private void Awake()
    {
        if (levelMenu == null) levelMenu = FindFirstObjectByType<MineLevelMenuController>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (levelMenu != null) levelMenu.RegisterMidLevelShop(this);
        else SetVisibleFromMenu(false);
    }

    private void OnDisable()
    {
        // Covers a HUD/canvas being disabled independently while the shop is
        // open. Normal menu-driven hiding clears isShopVisible before this runs.
        if (isShopVisible && levelMenu != null && levelMenu.isActiveAndEnabled)
            levelMenu.CloseShop();
    }

    public void RegisterWithMenu(MineLevelMenuController menu)
    {
        if (menu == null) return;
        levelMenu = menu;
        levelMenu.RegisterMidLevelShop(this);
    }

    public void ToggleShop()
    {
        if (levelMenu != null) levelMenu.ToggleShop();
    }

    public void ShowShop()
    {
        if (levelMenu != null) levelMenu.OpenShop();
    }

    public void HideShop()
    {
        if (levelMenu != null) levelMenu.CloseShop();
    }

    public void BuyExtraLife()
    {
        Report(GameProgress.BuyExtraLife(), "Extra life purchased.");
    }

    public void BuyHealthPotion()
    {
        Report(GameProgress.BuyHealthPotion(),
            $"Health potion purchased. Press {MineInput.GetControllerBindingDisplayName(MineButtonAction.Potion)} or H to use it.");
    }

    public void BuyHeartUpgrade()
    {
        bool purchased = GameProgress.BuyHeartUpgrade();
        if (purchased && playerHealth != null)
            playerHealth.SyncHeartCapacityFromProgress();
        Report(purchased, "Permanent heart upgrade purchased and filled.");
    }

    /// <summary>Explicit shop button action; Home/Back never invokes this path.</summary>
    public void ReturnToOverview()
    {
        if (levelMenu != null) levelMenu.ReturnToOverview();
    }

    public void Refresh()
    {
        if (balanceDisplay != null)
        {
            string mode = GameProgress.IsPlaytestRunActive ? "MINER SANDBOX     " : string.Empty;
            balanceDisplay.text =
                $"{mode}GREEN CRYSTALS  {GameProgress.Crystals}     " +
                $"LIVES  {GameProgress.Lives}     POTIONS  {GameProgress.HealthPotions}     " +
                $"HEARTS  {GameProgress.MaxHearts}";
        }

        if (playerHealth != null) playerHealth.RefreshHud();
    }

    internal void SetVisibleFromMenu(bool visible)
    {
        isShopVisible = visible;
        if (shopPanel != null) shopPanel.SetActive(visible);

        if (visible)
        {
            if (statusDisplay != null)
                statusDisplay.text = "Spend crystals without leaving the level.";
            Refresh();
            SelectDefault();
            return;
        }

        if (EventSystem.current == null || shopPanel == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null &&
            (selected == shopPanel || selected.transform.IsChildOf(shopPanel.transform)))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void Report(bool success, string message)
    {
        if (statusDisplay != null)
            statusDisplay.text = success ? message : "Not enough green crystals.";
        Refresh();
    }

    private void SelectDefault()
    {
        if (EventSystem.current != null && defaultSelection != null &&
            defaultSelection.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelection);
        }
    }
}
