using TMPro;
using UnityEngine;

public sealed class MineShopController : MonoBehaviour
{
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI balanceDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;

    private void OnEnable() => Refresh();

    public void Configure(GameObject levels, GameObject shop, TextMeshProUGUI balance, TextMeshProUGUI status)
    {
        levelPanel = levels;
        shopPanel = shop;
        balanceDisplay = balance;
        statusDisplay = status;
        Refresh();
    }

    public void ShowLevels()
    {
        levelPanel.SetActive(true);
        shopPanel.SetActive(false);
    }

    public void ShowShop()
    {
        levelPanel.SetActive(false);
        shopPanel.SetActive(true);
        statusDisplay.text = "Spend crystals collected in the shafts.";
        Refresh();
    }

    public void BuyExtraLife() => Report(GameProgress.BuyExtraLife(), "Extra life purchased.");
    public void BuyHealthPotion() => Report(GameProgress.BuyHealthPotion(), "Health potion purchased. Press H in a level to use it.");
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
}
