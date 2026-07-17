using System;
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

    private static bool playtestAccessEnabled;
    private static bool playtestRunActive;
    private static int playtestCrystals;
    private static int playtestLives;
    private static int playtestPotions;
    private static int playtestHeartUpgrades;
    private static int playtestHighestUnlockedLevel;
    private static bool playtestHasSilverKey;
    private static readonly bool[] PlaytestBronzeKeys = new bool[MaxMineLevel + 1];
    private static readonly bool[] PlaytestOpenedChests = new bool[MaxMineLevel + 1];

    public static event Action LevelAccessChanged;

    public static int Crystals => playtestRunActive
        ? playtestCrystals
        : PlayerPrefs.GetInt(CrystalsKey, 0);
    public static int Lives => playtestRunActive
        ? playtestLives
        : PlayerPrefs.GetInt(LivesKey, StartingLives);
    public static int HealthPotions => playtestRunActive
        ? playtestPotions
        : PlayerPrefs.GetInt(PotionsKey, 0);
    public static int MaxHearts => BaseHearts + (playtestRunActive
        ? playtestHeartUpgrades
        : PlayerPrefs.GetInt(HeartsUpgradeKey, 0));
    public static int HighestUnlockedLevel => playtestRunActive
        ? playtestHighestUnlockedLevel
        : PlayerPrefs.GetInt(HighestUnlockedLevelKey, 2);
    public static bool HasSilverKey => playtestRunActive
        ? playtestHasSilverKey
        : PlayerPrefs.GetInt(SilverKeyKey, 0) != 0;
    public static bool PlaytestAccessEnabled => playtestAccessEnabled;
    public static bool IsPlaytestRunActive => playtestRunActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        playtestAccessEnabled = false;
        playtestRunActive = false;
        Array.Clear(PlaytestBronzeKeys, 0, PlaytestBronzeKeys.Length);
        Array.Clear(PlaytestOpenedChests, 0, PlaytestOpenedChests.Length);
        LevelAccessChanged = null;
    }

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
        if (playtestRunActive)
        {
            playtestCrystals += amount;
            return;
        }
        PlayerPrefs.SetInt(CrystalsKey, Crystals + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendCrystals(int amount)
    {
        if (amount <= 0 || Crystals < amount) return false;
        if (playtestRunActive)
        {
            playtestCrystals -= amount;
            return true;
        }
        PlayerPrefs.SetInt(CrystalsKey, Crystals - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyExtraLife()
    {
        if (!SpendCrystals(ExtraLifePrice)) return false;
        if (playtestRunActive)
        {
            playtestLives++;
            return true;
        }
        PlayerPrefs.SetInt(LivesKey, Lives + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyHealthPotion()
    {
        if (!SpendCrystals(HealthPotionPrice)) return false;
        if (playtestRunActive)
        {
            playtestPotions++;
            return true;
        }
        PlayerPrefs.SetInt(PotionsKey, HealthPotions + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyHeartUpgrade()
    {
        if (!SpendCrystals(HeartUpgradePrice)) return false;
        if (playtestRunActive)
        {
            playtestHeartUpgrades++;
            return true;
        }
        PlayerPrefs.SetInt(HeartsUpgradeKey, PlayerPrefs.GetInt(HeartsUpgradeKey, 0) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddLife(int amount = 1)
    {
        if (amount <= 0) return;
        if (playtestRunActive)
        {
            playtestLives += amount;
            return;
        }
        PlayerPrefs.SetInt(LivesKey, Lives + amount);
        PlayerPrefs.Save();
    }

    public static void AddHealthPotion(int amount = 1)
    {
        if (amount <= 0) return;
        if (playtestRunActive)
        {
            playtestPotions += amount;
            return;
        }
        PlayerPrefs.SetInt(PotionsKey, HealthPotions + amount);
        PlayerPrefs.Save();
    }

    public static void RestartAfterGameOver()
    {
        if (playtestRunActive)
        {
            // A failed test tunnel discards only its in-memory sandbox. The real
            // save and session master key remain available on the overview.
            EndPlaytestRun();
            return;
        }

        SetPlaytestAccess(false);
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
            if (playtestRunActive)
            {
                playtestHighestUnlockedLevel = nextLevel;
                return;
            }
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextLevel);
            PlayerPrefs.Save();
        }
    }

    public static void CollectSilverKey()
    {
        if (playtestRunActive)
        {
            playtestHasSilverKey = true;
            return;
        }
        PlayerPrefs.SetInt(SilverKeyKey, 1);
        PlayerPrefs.Save();
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > MaxMineLevel) return false;
        if (playtestAccessEnabled) return true;
        if (levelNumber <= 2) return levelNumber >= 1;
        if (levelNumber == 11) return HasSilverKey && HighestUnlockedLevel >= 11;
        if (levelNumber == 12) return HasSilverKey && HighestUnlockedLevel >= 12;
        return levelNumber <= HighestUnlockedLevel;
    }

    public static bool HasBronzeKey(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        return playtestRunActive
            ? PlaytestBronzeKeys[safeLevel]
            : PlayerPrefs.GetInt($"Jump.BronzeKey.{safeLevel}", 0) != 0;
    }

    public static void CollectBronzeKey(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        if (playtestRunActive)
        {
            PlaytestBronzeKeys[safeLevel] = true;
            return;
        }
        PlayerPrefs.SetInt($"Jump.BronzeKey.{safeLevel}", 1);
        PlayerPrefs.Save();
    }

    public static bool IsChestOpened(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        return playtestRunActive
            ? PlaytestOpenedChests[safeLevel]
            : PlayerPrefs.GetInt($"Jump.ChestOpened.{safeLevel}", 0) != 0;
    }

    public static void MarkChestOpened(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        if (playtestRunActive)
        {
            PlaytestOpenedChests[safeLevel] = true;
            return;
        }
        PlayerPrefs.SetInt($"Jump.ChestOpened.{safeLevel}", 1);
        PlayerPrefs.Save();
    }

    public static bool ConsumeLife()
    {
        if (Lives <= 0) return false;
        int remaining = Lives - 1;
        if (playtestRunActive)
        {
            playtestLives = remaining;
            return remaining > 0;
        }
        PlayerPrefs.SetInt(LivesKey, remaining);
        PlayerPrefs.Save();
        return remaining > 0;
    }

    public static bool ConsumePotion()
    {
        if (HealthPotions <= 0) return false;
        if (playtestRunActive)
        {
            playtestPotions--;
            return true;
        }
        PlayerPrefs.SetInt(PotionsKey, HealthPotions - 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void SetPlaytestAccess(bool enabled)
    {
        if (playtestAccessEnabled == enabled) return;
        if (!enabled) EndPlaytestRun();
        playtestAccessEnabled = enabled;
        LevelAccessChanged?.Invoke();
    }

    public static bool TogglePlaytestAccess()
    {
        SetPlaytestAccess(!playtestAccessEnabled);
        return playtestAccessEnabled;
    }

    public static bool BeginPlaytestRun()
    {
        if (!playtestAccessEnabled || playtestRunActive) return false;

        playtestCrystals = PlayerPrefs.GetInt(CrystalsKey, 0);
        playtestLives = PlayerPrefs.GetInt(LivesKey, StartingLives);
        playtestPotions = PlayerPrefs.GetInt(PotionsKey, 0);
        playtestHeartUpgrades = PlayerPrefs.GetInt(HeartsUpgradeKey, 0);
        playtestHighestUnlockedLevel = PlayerPrefs.GetInt(HighestUnlockedLevelKey, 2);
        playtestHasSilverKey = PlayerPrefs.GetInt(SilverKeyKey, 0) != 0;
        for (int levelNumber = 1; levelNumber <= MaxMineLevel; levelNumber++)
        {
            PlaytestBronzeKeys[levelNumber] =
                PlayerPrefs.GetInt($"Jump.BronzeKey.{levelNumber}", 0) != 0;
            PlaytestOpenedChests[levelNumber] =
                PlayerPrefs.GetInt($"Jump.ChestOpened.{levelNumber}", 0) != 0;
        }

        playtestRunActive = true;
        return true;
    }

    public static void EndPlaytestRun()
    {
        playtestRunActive = false;
    }
}
