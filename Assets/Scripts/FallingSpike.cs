using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class FallingSpike : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.5f)] private float detectionDistance = 8f;
    [SerializeField, Min(0.1f)] private float detectionHalfWidth = 1.1f;
    [SerializeField, Min(0.1f)] private float warningSeconds = 0.35f;
    [SerializeField, Min(0.1f)] private float resetSeconds = 2.5f;
    [SerializeField, Min(0.1f)] private float fallingGravity = 4f;

    private Rigidbody2D body;
    private Collider2D spikeCollider;
    private SpriteRenderer spriteRenderer;
    private HeroMovement player;
    private Vector3 armedPosition;
    private bool triggered;

    public bool IsTriggered => triggered;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spikeCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        armedPosition = transform.position;
        Arm();
    }

    private void Update()
    {
        if (triggered)
        {
            return;
        }

        player ??= FindFirstObjectByType<HeroMovement>();
        if (player == null)
        {
            return;
        }

        Vector2 offset = player.transform.position - transform.position;
        if (offset.y < 0f && offset.y >= -detectionDistance && Mathf.Abs(offset.x) <= detectionHalfWidth)
        {
            StartCoroutine(ReleaseRoutine());
        }
    }

    public void Configure(float range, float halfWidth, float warning, float reset, int damageAmount)
    {
        detectionDistance = Mathf.Max(0.5f, range);
        detectionHalfWidth = Mathf.Max(0.1f, halfWidth);
        warningSeconds = Mathf.Max(0.1f, warning);
        resetSeconds = Mathf.Max(0.1f, reset);
        damage = Mathf.Max(1, damageAmount);
    }

    private IEnumerator ReleaseRoutine()
    {
        triggered = true;
        Color original = spriteRenderer == null ? Color.white : spriteRenderer.color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.45f, 0.25f, 1f);
        }

        yield return new WaitForSeconds(warningSeconds);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = original;
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = fallingGravity;
        body.WakeUp();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggered || body.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage, transform.position);
        }

        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        spikeCollider.enabled = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        yield return new WaitForSeconds(resetSeconds);

        transform.position = armedPosition;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        spikeCollider.enabled = true;
        Arm();
    }

    private void Arm()
    {
        triggered = false;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.45f);
        Vector3 center = transform.position + Vector3.down * detectionDistance * 0.5f;
        Gizmos.DrawWireCube(center, new Vector3(detectionHalfWidth * 2f, detectionDistance, 0f));
    }
}
