using System;
using System.Collections.Generic;
using System.Text;
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
    private const string InventoryEpochKey = "Jump.InventoryEpoch";
    private const string ScopedKeyCountPrefix = "Jump.Inventory.v2.KeyCount";
    private const string ScopedPickupPrefix = "Jump.Inventory.v2.Pickup";
    private const string ScopedChestPrefix = "Jump.Inventory.v2.Chest";

    public const string BronzeDungeonId = "bronze";
    public const string SilverDungeonId = "silver";
    public const string LegacyBronzeKeyPickupId = "legacy-bronze-key";
    public const string LegacyRewardChestId = "legacy-reward-chest";

    public const int MaxMineLevel = 12;
    public const int StartingLives = 3;
    public const int BaseHearts = 7;
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
    private static readonly Dictionary<string, int> PlaytestKeyCounts = new();
    private static readonly HashSet<string> PlaytestCollectedPickups = new();
    private static readonly HashSet<string> PlaytestOpenedChests = new();

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
        ClearPlaytestInventory();
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

        // Scoped inventory entries can contain authored pickup/chest IDs, so
        // PlayerPrefs cannot enumerate them safely. Advancing the epoch makes
        // every scoped entry from the previous run unreachable in one write.
        PlayerPrefs.SetInt(InventoryEpochKey, CurrentInventoryEpoch + 1);

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
        return GetKeyCount(BronzeDungeonId, safeLevel) > 0;
    }

    public static void CollectBronzeKey(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);

        // The original API represented one reusable Boolean key. Keep calls
        // idempotent while upgrading that key into the counted inventory.
        if (playtestRunActive)
        {
            string scope = ScopeIdentity(BronzeDungeonId, safeLevel);
            PlaytestCollectedPickups.Add(
                PickupIdentity(BronzeDungeonId, safeLevel, LegacyBronzeKeyPickupId));
            if (GetKeyCount(BronzeDungeonId, safeLevel) == 0)
            {
                PlaytestKeyCounts[scope] = 1;
            }
            return;
        }

        WritePersistedPickupCollected(BronzeDungeonId, safeLevel,
            LegacyBronzeKeyPickupId);
        if (LoadPersistedKeyCount(BronzeDungeonId, safeLevel) == 0)
        {
            WritePersistedKeyCount(BronzeDungeonId, safeLevel, 1);
        }
        PlayerPrefs.Save();
    }

    public static bool TryConsumeBronzeKey(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        return TryConsumeKey(BronzeDungeonId, safeLevel);
    }

    public static bool IsChestOpened(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        return IsChestOpened(BronzeDungeonId, safeLevel, LegacyRewardChestId);
    }

    public static void MarkChestOpened(int levelNumber)
    {
        int safeLevel = Mathf.Clamp(levelNumber, 1, MaxMineLevel);
        MarkChestOpened(BronzeDungeonId, safeLevel, LegacyRewardChestId);
    }

    public static int GetKeyCount(string dungeonId, int levelNumber)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string scope = ScopeIdentity(normalizedDungeon, safeLevel);

        if (playtestRunActive)
        {
            if (!PlaytestKeyCounts.TryGetValue(scope, out int count))
            {
                count = LoadPersistedKeyCount(normalizedDungeon, safeLevel);
                PlaytestKeyCounts[scope] = count;
            }
            return count;
        }

        return LoadPersistedKeyCount(normalizedDungeon, safeLevel);
    }

    public static bool HasKey(string dungeonId, int levelNumber)
    {
        return GetKeyCount(dungeonId, levelNumber) > 0;
    }

    public static bool IsKeyPickupCollected(string dungeonId, int levelNumber,
        string pickupId)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string normalizedPickup = NormalizeContentId(pickupId, LegacyBronzeKeyPickupId);
        string identity = PickupIdentity(normalizedDungeon, safeLevel, normalizedPickup);

        if (playtestRunActive)
        {
            if (PlaytestCollectedPickups.Contains(identity)) return true;
            if (!LoadPersistedPickupCollected(normalizedDungeon, safeLevel, normalizedPickup))
            {
                return false;
            }

            PlaytestCollectedPickups.Add(identity);
            return true;
        }

        return LoadPersistedPickupCollected(normalizedDungeon, safeLevel, normalizedPickup);
    }

    public static bool TryCollectKey(string dungeonId, int levelNumber, string pickupId)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string normalizedPickup = NormalizeContentId(pickupId, LegacyBronzeKeyPickupId);
        string pickupIdentity = PickupIdentity(normalizedDungeon, safeLevel, normalizedPickup);
        string scope = ScopeIdentity(normalizedDungeon, safeLevel);

        if (IsKeyPickupCollected(normalizedDungeon, safeLevel, normalizedPickup))
        {
            return false;
        }

        if (playtestRunActive)
        {
            PlaytestCollectedPickups.Add(pickupIdentity);
            PlaytestKeyCounts[scope] = GetKeyCount(normalizedDungeon, safeLevel) + 1;
            return true;
        }

        WritePersistedPickupCollected(normalizedDungeon, safeLevel, normalizedPickup);
        WritePersistedKeyCount(normalizedDungeon, safeLevel,
            LoadPersistedKeyCount(normalizedDungeon, safeLevel) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool TryConsumeKey(string dungeonId, int levelNumber)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        int count = GetKeyCount(normalizedDungeon, safeLevel);
        if (count <= 0) return false;

        int remaining = count - 1;
        if (playtestRunActive)
        {
            PlaytestKeyCounts[ScopeIdentity(normalizedDungeon, safeLevel)] = remaining;
            return true;
        }

        WritePersistedKeyCount(normalizedDungeon, safeLevel, remaining);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsChestOpened(string dungeonId, int levelNumber, string chestId)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string normalizedChest = NormalizeContentId(chestId, LegacyRewardChestId);
        string identity = ChestIdentity(normalizedDungeon, safeLevel, normalizedChest);

        if (playtestRunActive)
        {
            if (PlaytestOpenedChests.Contains(identity)) return true;
            if (!LoadPersistedChestOpened(normalizedDungeon, safeLevel, normalizedChest))
            {
                return false;
            }

            PlaytestOpenedChests.Add(identity);
            return true;
        }

        return LoadPersistedChestOpened(normalizedDungeon, safeLevel, normalizedChest);
    }

    public static void MarkChestOpened(string dungeonId, int levelNumber, string chestId)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string normalizedChest = NormalizeContentId(chestId, LegacyRewardChestId);
        string identity = ChestIdentity(normalizedDungeon, safeLevel, normalizedChest);

        if (playtestRunActive)
        {
            PlaytestOpenedChests.Add(identity);
            return;
        }

        WritePersistedChestOpened(normalizedDungeon, safeLevel, normalizedChest);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Claims a chest and consumes its key as one gameplay transaction. The
    /// caller should award the reward only after this method returns true.
    /// </summary>
    public static bool TryUnlockChest(string dungeonId, int levelNumber, string chestId)
    {
        string normalizedDungeon = NormalizeDungeonId(dungeonId);
        int safeLevel = Mathf.Max(1, levelNumber);
        string normalizedChest = NormalizeContentId(chestId, LegacyRewardChestId);
        string chestIdentity = ChestIdentity(normalizedDungeon, safeLevel, normalizedChest);

        if (IsChestOpened(normalizedDungeon, safeLevel, normalizedChest)) return false;

        int keyCount = GetKeyCount(normalizedDungeon, safeLevel);
        if (keyCount <= 0) return false;

        if (playtestRunActive)
        {
            PlaytestKeyCounts[ScopeIdentity(normalizedDungeon, safeLevel)] = keyCount - 1;
            PlaytestOpenedChests.Add(chestIdentity);
            return true;
        }

        // Both values are changed before the single flush so no second chest
        // interaction can observe a consumed key without the first chest claim.
        WritePersistedKeyCount(normalizedDungeon, safeLevel, keyCount - 1);
        WritePersistedChestOpened(normalizedDungeon, safeLevel, normalizedChest);
        PlayerPrefs.Save();
        return true;
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
        ClearPlaytestInventory();
        playtestRunActive = true;
        return true;
    }

    public static void EndPlaytestRun()
    {
        playtestRunActive = false;
        ClearPlaytestInventory();
    }

    public static string NormalizeDungeonId(string dungeonId)
    {
        return NormalizeContentId(dungeonId, BronzeDungeonId);
    }

    public static string NormalizeContentId(string contentId, string fallback)
    {
        string source = string.IsNullOrWhiteSpace(contentId) ? fallback : contentId;
        source = string.IsNullOrWhiteSpace(source) ? "default" : source.Trim();

        StringBuilder result = new(source.Length);
        bool separatorPending = false;
        foreach (char character in source)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && result.Length > 0) result.Append('-');
                result.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }

        return result.Length == 0 ? "default" : result.ToString();
    }

    private static int CurrentInventoryEpoch => PlayerPrefs.GetInt(InventoryEpochKey, 0);

    private static string ScopeIdentity(string dungeonId, int levelNumber)
    {
        return $"{NormalizeDungeonId(dungeonId)}.{Mathf.Max(1, levelNumber)}";
    }

    private static string PickupIdentity(string dungeonId, int levelNumber, string pickupId)
    {
        return $"{ScopeIdentity(dungeonId, levelNumber)}." +
            NormalizeContentId(pickupId, LegacyBronzeKeyPickupId);
    }

    private static string ChestIdentity(string dungeonId, int levelNumber, string chestId)
    {
        return $"{ScopeIdentity(dungeonId, levelNumber)}." +
            NormalizeContentId(chestId, LegacyRewardChestId);
    }

    private static string ScopedPreferenceKey(string prefix, string identity)
    {
        return $"{prefix}.{CurrentInventoryEpoch}.{identity}";
    }

    private static string KeyCountPreferenceKey(string dungeonId, int levelNumber)
    {
        return ScopedPreferenceKey(ScopedKeyCountPrefix,
            ScopeIdentity(dungeonId, levelNumber));
    }

    private static string PickupPreferenceKey(string dungeonId, int levelNumber, string pickupId)
    {
        return ScopedPreferenceKey(ScopedPickupPrefix,
            PickupIdentity(dungeonId, levelNumber, pickupId));
    }

    private static string ChestPreferenceKey(string dungeonId, int levelNumber, string chestId)
    {
        return ScopedPreferenceKey(ScopedChestPrefix,
            ChestIdentity(dungeonId, levelNumber, chestId));
    }

    private static bool IsLegacyBronzeScope(string dungeonId, int levelNumber)
    {
        return NormalizeDungeonId(dungeonId) == BronzeDungeonId &&
            levelNumber >= 1 && levelNumber <= MaxMineLevel;
    }

    private static bool IsLegacyPickup(string dungeonId, int levelNumber, string pickupId)
    {
        return IsLegacyBronzeScope(dungeonId, levelNumber) &&
            NormalizeContentId(pickupId, LegacyBronzeKeyPickupId) == LegacyBronzeKeyPickupId;
    }

    private static bool IsLegacyChest(string dungeonId, int levelNumber, string chestId)
    {
        return IsLegacyBronzeScope(dungeonId, levelNumber) &&
            NormalizeContentId(chestId, LegacyRewardChestId) == LegacyRewardChestId;
    }

    private static int LoadPersistedKeyCount(string dungeonId, int levelNumber)
    {
        string preferenceKey = KeyCountPreferenceKey(dungeonId, levelNumber);
        if (PlayerPrefs.HasKey(preferenceKey))
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(preferenceKey, 0));
        }

        if (!IsLegacyBronzeScope(dungeonId, levelNumber)) return 0;

        bool legacyKeyCollected =
            PlayerPrefs.GetInt($"Jump.BronzeKey.{levelNumber}", 0) != 0;
        bool legacyChestOpened =
            PlayerPrefs.GetInt($"Jump.ChestOpened.{levelNumber}", 0) != 0;
        if (!legacyKeyCollected && !legacyChestOpened) return 0;

        // In old saves the key Boolean stayed true after opening the chest. The
        // migrated consumable count must be zero for that already-open chest,
        // while the pickup marker stays true so the key never respawns.
        int migratedCount = legacyKeyCollected && !legacyChestOpened ? 1 : 0;
        WritePersistedKeyCount(dungeonId, levelNumber, migratedCount);
        WritePersistedPickupCollected(dungeonId, levelNumber, LegacyBronzeKeyPickupId);
        PlayerPrefs.Save();
        return migratedCount;
    }

    private static bool LoadPersistedPickupCollected(string dungeonId, int levelNumber,
        string pickupId)
    {
        string preferenceKey = PickupPreferenceKey(dungeonId, levelNumber, pickupId);
        if (PlayerPrefs.GetInt(preferenceKey, 0) != 0) return true;

        // Once a scoped count exists, the legacy Boolean is only a compatibility
        // mirror of that count; it no longer proves this particular pickup was
        // collected. Migration writes the scoped count and legacy pickup marker
        // together, so a missing marker here means the pickup is genuinely new.
        if (PlayerPrefs.HasKey(KeyCountPreferenceKey(dungeonId, levelNumber)))
        {
            return false;
        }

        if (!IsLegacyPickup(dungeonId, levelNumber, pickupId))
        {
            return false;
        }

        bool legacyKeyCollected =
            PlayerPrefs.GetInt($"Jump.BronzeKey.{levelNumber}", 0) != 0;
        bool legacyChestOpened =
            PlayerPrefs.GetInt($"Jump.ChestOpened.{levelNumber}", 0) != 0;
        if (!legacyKeyCollected && !legacyChestOpened) return false;

        WritePersistedPickupCollected(dungeonId, levelNumber, pickupId);
        if (!PlayerPrefs.HasKey(KeyCountPreferenceKey(dungeonId, levelNumber)))
        {
            WritePersistedKeyCount(dungeonId, levelNumber,
                legacyKeyCollected && !legacyChestOpened ? 1 : 0);
        }
        PlayerPrefs.Save();
        return true;
    }

    private static bool LoadPersistedChestOpened(string dungeonId, int levelNumber,
        string chestId)
    {
        string preferenceKey = ChestPreferenceKey(dungeonId, levelNumber, chestId);
        if (PlayerPrefs.GetInt(preferenceKey, 0) != 0) return true;

        if (!IsLegacyChest(dungeonId, levelNumber, chestId) ||
            PlayerPrefs.GetInt($"Jump.ChestOpened.{levelNumber}", 0) == 0)
        {
            return false;
        }

        WritePersistedChestOpened(dungeonId, levelNumber, chestId);
        PlayerPrefs.Save();
        return true;
    }

    private static void WritePersistedKeyCount(string dungeonId, int levelNumber, int count)
    {
        int safeCount = Mathf.Max(0, count);
        PlayerPrefs.SetInt(KeyCountPreferenceKey(dungeonId, levelNumber), safeCount);
        if (IsLegacyBronzeScope(dungeonId, levelNumber))
        {
            PlayerPrefs.SetInt($"Jump.BronzeKey.{levelNumber}", safeCount > 0 ? 1 : 0);
        }
    }

    private static void WritePersistedPickupCollected(string dungeonId, int levelNumber,
        string pickupId)
    {
        PlayerPrefs.SetInt(PickupPreferenceKey(dungeonId, levelNumber, pickupId), 1);
    }

    private static void WritePersistedChestOpened(string dungeonId, int levelNumber,
        string chestId)
    {
        PlayerPrefs.SetInt(ChestPreferenceKey(dungeonId, levelNumber, chestId), 1);
        if (IsLegacyChest(dungeonId, levelNumber, chestId))
        {
            PlayerPrefs.SetInt($"Jump.ChestOpened.{levelNumber}", 1);
        }
    }

    private static void ClearPlaytestInventory()
    {
        PlaytestKeyCounts.Clear();
        PlaytestCollectedPickups.Clear();
        PlaytestOpenedChests.Clear();
    }
}
