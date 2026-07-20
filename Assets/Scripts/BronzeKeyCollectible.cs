using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class BronzeKeyCollectible : MonoBehaviour
{
    [SerializeField] private string dungeonId = GameProgress.BronzeDungeonId;
    [SerializeField, Min(1)] private int levelNumber = 1;
    [SerializeField] private string pickupId = GameProgress.LegacyBronzeKeyPickupId;

    public string DungeonId => dungeonId;
    public int LevelNumber => levelNumber;
    public string PickupId => pickupId;
    public bool WasCollected => GameProgress.IsKeyPickupCollected(
        dungeonId, levelNumber, pickupId);

    public void Configure(int currentLevelNumber)
    {
        Configure(GameProgress.BronzeDungeonId, currentLevelNumber,
            GameProgress.LegacyBronzeKeyPickupId);
    }

    public void Configure(string currentDungeonId, int currentLevelNumber,
        string uniquePickupId)
    {
        dungeonId = GameProgress.NormalizeDungeonId(currentDungeonId);
        levelNumber = Mathf.Max(1, currentLevelNumber);
        pickupId = GameProgress.NormalizeContentId(uniquePickupId,
            GameProgress.LegacyBronzeKeyPickupId);

        if (Application.isPlaying)
        {
            gameObject.SetActive(!WasCollected);
        }
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        dungeonId = GameProgress.NormalizeDungeonId(dungeonId);
        pickupId = GameProgress.NormalizeContentId(pickupId,
            GameProgress.LegacyBronzeKeyPickupId);
        if (WasCollected) gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null || !inventory.MatchesScope(dungeonId, levelNumber)) return;

        if (inventory.TryCollectKey(dungeonId, levelNumber, pickupId) || WasCollected)
        {
            // The pickup marker persists separately from the consumable count,
            // so spending this key can never make the pickup respawn.
            Destroy(gameObject);
        }
    }
}
