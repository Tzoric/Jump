using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class GreenCrystalCollectible : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<HeroMovement>() == null) return;
        GameProgress.AddCrystals(value);
        Destroy(gameObject);
    }
}
