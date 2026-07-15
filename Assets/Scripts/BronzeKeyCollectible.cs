using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class BronzeKeyCollectible : MonoBehaviour
{
    [SerializeField, Min(1)] private int levelNumber = 1;

    public int LevelNumber => levelNumber;

    public void Configure(int currentLevelNumber) => levelNumber = Mathf.Max(1, currentLevelNumber);

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null) return;
        if (inventory.LevelNumber != levelNumber) return;
        inventory.CollectBronzeKey();
        Destroy(gameObject);
    }
}
