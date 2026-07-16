using UnityEngine;

public sealed class DamageZone : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;

    public void Configure(int damageAmount)
    {
        damage = Mathf.Max(1, damageAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.TakeDamage(damage, transform.position))
        {
            Debug.Log($"DAMAGE ZONE HIT: '{name}' at {transform.position} hit the player at {health.transform.position}.");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.TakeDamage(damage, transform.position))
            Debug.Log($"DAMAGE ZONE STAY: '{name}' at {transform.position} hit the player at {health.transform.position}.");
    }
}
