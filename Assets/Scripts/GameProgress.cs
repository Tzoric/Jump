using UnityEngine;

public static class GameProgress
{
    private const string CrystalsKey = "Jump.GreenCrystals";
    private const string LivesKey = "Jump.Lives";
    private const string PotionsKey = "Jump.HealthPotions";
    private const string HeartsUpgradeKey = "Jump.HeartsUpgrade";
    private const string InitializedKey = "Jump.ProgressInitialized";
    private const string HighestUnlockedLevelKey = "Jump.HighestUnlockedLevel";
    private const string SilverKeyKey = "Jump.SilverKey";

    public const int MaxMineLevel = 12;
    public const int StartingLives = 3;
    public const int BaseHearts = 5;
    public const int ExtraLifePrice = 25;
    public const int HealthPotionPrice = 3;
    public const int HeartUpgradePrice = 25;

    public static int Crystals => PlayerPrefs.GetInt(CrystalsKey, 0);
    public static int Lives => PlayerPrefs.GetInt(LivesKey, StartingLives);
    public static int HealthPotions => PlayerPrefs.GetInt(PotionsKey, 0);
    public static int MaxHearts => BaseHearts + PlayerPrefs.GetInt(HeartsUpgradeKey, 0);
    public static int HighestUnlockedLevel => PlayerPrefs.GetInt(HighestUnlockedLevelKey, 2);
    public static bool HasSilverKey => PlayerPrefs.GetInt(SilverKeyKey, 0) != 0;

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

    public static void AddLife(int amount = 1)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(LivesKey, Lives + amount);
        PlayerPrefs.Save();
    }

    public static void AddHealthPotion(int amount = 1)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(PotionsKey, HealthPotions + amount);
        PlayerPrefs.Save();
    }

    public static void RestartAfterGameOver()
    {
        // Game Over starts a genuinely new run. Delete only Jump progression
        // keys so future non-progression preferences (audio, controls, etc.) are
        // not accidentally erased with PlayerPrefs.DeleteAll().
        PlayerPrefs.DeleteKey(CrystalsKey);
        PlayerPrefs.DeleteKey(LivesKey);
        PlayerPrefs.DeleteKey(PotionsKey);
        PlayerPrefs.DeleteKey(HeartsUpgradeKey);
        PlayerPrefs.DeleteKey(InitializedKey);
        PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
        PlayerPrefs.DeleteKey(SilverKeyKey);

        for (int levelNumber = 1; levelNumber <= MaxMineLevel; levelNumber++)
        {
            PlayerPrefs.DeleteKey($"Jump.BronzeKey.{levelNumber}");
            PlayerPrefs.DeleteKey($"Jump.ChestOpened.{levelNumber}");
        }

        PlayerPrefs.SetInt(InitializedKey, 1);
        PlayerPrefs.SetInt(LivesKey, StartingLives);
        PlayerPrefs.Save();
    }

    public static void CompleteLevel(int levelNumber)
    {
        if (levelNumber <= 0) return;
        int nextLevel = Mathf.Clamp(levelNumber + 1, 2, MaxMineLevel);
        if (nextLevel > HighestUnlockedLevel)
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextLevel);
            PlayerPrefs.Save();
        }
    }

    public static void CollectSilverKey()
    {
        PlayerPrefs.SetInt(SilverKeyKey, 1);
        PlayerPrefs.Save();
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > MaxMineLevel) return false;
        if (levelNumber <= 2) return levelNumber >= 1;
        if (levelNumber == 11) return HasSilverKey && HighestUnlockedLevel >= 11;
        if (levelNumber == 12) return HasSilverKey && HighestUnlockedLevel >= 12;
        return levelNumber <= HighestUnlockedLevel;
    }

    public static bool HasBronzeKey(int levelNumber) =>
        PlayerPrefs.GetInt($"Jump.BronzeKey.{Mathf.Max(1, levelNumber)}", 0) != 0;

    public static void CollectBronzeKey(int levelNumber)
    {
        PlayerPrefs.SetInt($"Jump.BronzeKey.{Mathf.Max(1, levelNumber)}", 1);
        PlayerPrefs.Save();
    }

    public static bool IsChestOpened(int levelNumber) =>
        PlayerPrefs.GetInt($"Jump.ChestOpened.{Mathf.Max(1, levelNumber)}", 0) != 0;

    public static void MarkChestOpened(int levelNumber)
    {
        PlayerPrefs.SetInt($"Jump.ChestOpened.{Mathf.Max(1, levelNumber)}", 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeLife()
    {
        if (Lives <= 0) return false;
        int remaining = Lives - 1;
        PlayerPrefs.SetInt(LivesKey, remaining);
        PlayerPrefs.Save();
        return remaining > 0;
    }

    public static bool ConsumePotion()
    {
        if (HealthPotions <= 0) return false;
        PlayerPrefs.SetInt(PotionsKey, HealthPotions - 1);
        PlayerPrefs.Save();
        return true;
    }
}
