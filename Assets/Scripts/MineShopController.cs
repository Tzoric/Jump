using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class MineShopController : MonoBehaviour
{
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI balanceDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private GameObject levelDefaultSelection;
    [SerializeField] private GameObject shopDefaultSelection;

    private GameObject pendingSelection;

    public GameObject LevelPanel => levelPanel;
    public GameObject ShopPanel => shopPanel;
    public bool IsShopVisible => shopPanel != null && shopPanel.activeSelf;

    private void OnEnable()
    {
        if (GameProgress.Lives <= 0)
        {
            OverviewArrival.Clear();
            SceneManager.LoadScene("GameOver");
            return;
        }

        if (OverviewArrival.ConsumeShopRequest()) ShowShop();
        else Refresh();
    }

    private void Start() => SelectPending();

    public void Configure(GameObject levels, GameObject shop, TextMeshProUGUI balance, TextMeshProUGUI status,
        GameObject firstLevelSelection = null, GameObject firstShopSelection = null)
    {
        levelPanel = levels;
        shopPanel = shop;
        balanceDisplay = balance;
        statusDisplay = status;
        levelDefaultSelection = firstLevelSelection;
        shopDefaultSelection = firstShopSelection;
        Refresh();
    }

    public void ShowLevels()
    {
        levelPanel.SetActive(true);
        shopPanel.SetActive(false);
        Select(levelDefaultSelection);
    }

    public void ShowShop()
    {
        levelPanel.SetActive(false);
        shopPanel.SetActive(true);
        statusDisplay.text = "Spend crystals collected in the shafts.";
        Refresh();
        Select(shopDefaultSelection);
    }

    public void BuyExtraLife() => Report(GameProgress.BuyExtraLife(), "Extra life purchased.");
    public void BuyHealthPotion() => Report(GameProgress.BuyHealthPotion(), "Health potion purchased. Press Y or H in a level to use it.");
    public void BuyHeartUpgrade() => Report(GameProgress.BuyHeartUpgrade(), "Permanent heart upgrade purchased.");

    private void Report(bool success, string message)
    {
        statusDisplay.text = success ? message : "Not enough green crystals.";
        Refresh();
    }

    private void Refresh()
    {
        if (balanceDisplay != null)
        {
            balanceDisplay.text = $"GREEN CRYSTALS  {GameProgress.Crystals}     LIVES  {GameProgress.Lives}     POTIONS  {GameProgress.HealthPotions}";
        }
    }

    private void Select(GameObject selection)
    {
        pendingSelection = selection;
        SelectPending();
    }

    private void SelectPending()
    {
        if (pendingSelection != null && EventSystem.current != null && pendingSelection.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(pendingSelection);
        }
    }
}
