using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GreenCrystalCollectible : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;

    public int Value => value;

    public void Configure(int crystalValue)
    {
        value = Mathf.Max(1, crystalValue);
    }

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        HeroMovement hero = other.GetComponentInParent<HeroMovement>();
        if (hero == null) return;
        GameProgress.AddCrystals(value);
        hero.GetComponent<MineRunInventory>()?.ShowMessage($"CRYSTAL +{value}");
        Destroy(gameObject);
    }
}
