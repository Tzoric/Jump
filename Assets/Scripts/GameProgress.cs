using UnityEngine;

public static class GameProgress
{
    private const string CrystalsKey = "Jump.GreenCrystals";
    private const string LivesKey = "Jump.Lives";
    private const string PotionsKey = "Jump.HealthPotions";
    private const string HeartsUpgradeKey = "Jump.HeartsUpgrade";
    private const string InitializedKey = "Jump.ProgressInitialized";

    public const int StartingLives = 3;
    public const int BaseHearts = 5;
    public const int ExtraLifePrice = 10;
    public const int HealthPotionPrice = 5;
    public const int HeartUpgradePrice = 25;

    public static int Crystals => PlayerPrefs.GetInt(CrystalsKey, 0);
    public static int Lives => PlayerPrefs.GetInt(LivesKey, StartingLives);
    public static int HealthPotions => PlayerPrefs.GetInt(PotionsKey, 0);
    public static int MaxHearts => BaseHearts + PlayerPrefs.GetInt(HeartsUpgradeKey, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (PlayerPrefs.GetInt(InitializedKey, 0) != 0)
        {
            return;
        }

        PlayerPrefs.SetInt(InitializedKey, 1);
        PlayerPrefs.SetInt(LivesKey, StartingLives);
        PlayerPrefs.Save();
    }

    public static void AddCrystals(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(CrystalsKey, Crystals + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendCrystals(int amount)
    {
        if (amount <= 0 || Crystals < amount) return false;
        PlayerPrefs.SetInt(CrystalsKey, Crystals - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyExtraLife()
    {
        if (!SpendCrystals(ExtraLifePrice)) return false;
        PlayerPrefs.SetInt(LivesKey, Lives + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyHealthPotion()
    {
        if (!SpendCrystals(HealthPotionPrice)) return false;
        PlayerPrefs.SetInt(PotionsKey, HealthPotions + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyHeartUpgrade()
    {
        if (!SpendCrystals(HeartUpgradePrice)) return false;
        PlayerPrefs.SetInt(HeartsUpgradeKey, PlayerPrefs.GetInt(HeartsUpgradeKey, 0) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool ConsumeLife()
    {
        if (Lives <= 0) return false;
        PlayerPrefs.SetInt(LivesKey, Lives - 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool ConsumePotion()
    {
        if (HealthPotions <= 0) return false;
        PlayerPrefs.SetInt(PotionsKey, HealthPotions - 1);
        PlayerPrefs.Save();
        return true;
    }
}
