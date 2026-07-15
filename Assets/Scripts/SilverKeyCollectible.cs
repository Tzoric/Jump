using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SilverKeyCollectible : MonoBehaviour
{
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        MineRunInventory inventory = other.GetComponentInParent<MineRunInventory>();
        if (inventory == null) return;
        GameProgress.CollectSilverKey();
        inventory.ShowMessage("SILVER KEY FOUND — LEVEL 11 UNLOCKED");
        Destroy(gameObject);
    }
}
