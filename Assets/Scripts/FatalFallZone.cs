using UnityEngine;

/// <summary>
/// A semantic bottomless fall. Unlike ordinary spike damage, a fatal fall must
/// consume the attempt even when the miner is temporarily invulnerable after a
/// recent one-heart hit.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class FatalFallZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKill(other);
    }

    private void TryKill(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.KillFromFall())
        {
            Debug.Log($"FATAL FALL: '{name}' caught the player at {health.transform.position}.");
        }
    }
}
