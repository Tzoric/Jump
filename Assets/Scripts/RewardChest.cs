using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class RewardChest : MonoBehaviour
{
    public const float BlueGemChance = 0.50f;
    public const float PotionChance = 0.45f;
    public const float ExtraLifeChance = 0.05f;

    public bool IsOpened { get; private set; }
    public string LastReward { get; private set; }
    [SerializeField, Min(1)] private int levelNumber = 1;
    public int LevelNumber => levelNumber;

    public void Configure(int currentLevelNumber) => levelNumber = Mathf.Max(1, currentLevelNumber);

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (GameProgress.IsChestOpened(levelNumber))
        {
            IsOpened = true;
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().color = new Color32(105, 86, 70, 255);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOpened) return;
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null) return;
        if (inventory.LevelNumber != levelNumber || !inventory.TryUseBronzeKey())
        {
            inventory.ShowMessage("CHEST LOCKED — BRONZE KEY REQUIRED");
            return;
        }

        Open(inventory, Random.value);
    }

    public void Open(MineRunInventory inventory, float roll)
    {
        if (IsOpened || inventory == null) return;
        IsOpened = true;
        GameProgress.MarkChestOpened(levelNumber);
        roll = Mathf.Clamp01(roll);
        if (roll < BlueGemChance)
        {
            GameProgress.AddCrystals(5);
            LastReward = "BLUE GEM +5";
        }
        else if (roll < BlueGemChance + PotionChance)
        {
            GameProgress.AddHealthPotion();
            LastReward = "HEALTH POTION +1";
        }
        else
        {
            GameProgress.AddLife();
            LastReward = "EXTRA LIFE +1";
        }

        inventory.ShowMessage($"CHEST: {LastReward}");
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().color = new Color32(105, 86, 70, 255);
    }
}
