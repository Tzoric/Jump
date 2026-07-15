using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class WeightedBreakablePlatform : MonoBehaviour
{
    [Header("Load")]
    [Tooltip("Seconds the platform survives under a player with an apparent weight of 1.")]
    [SerializeField, Min(0.25f)] private float standardLoadSeconds = 3f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 0.35f;

    [Header("Reset")]
    [SerializeField, Min(0.1f)] private float brokenResetSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float fallingGravity = 3f;

    private readonly HashSet<PlayerWeight> riders = new();
    private Rigidbody2D body;
    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Color originalColor;
    private float durability = 1f;
    private bool broken;

    public float Durability => durability;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        originalColor = spriteRenderer.color;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    private void FixedUpdate()
    {
        if (broken)
        {
            return;
        }

        riders.RemoveWhere(rider => rider == null);
        float totalApparentWeight = 0f;
        foreach (PlayerWeight rider in riders)
        {
            totalApparentWeight += rider.ApparentWeight;
        }

        if (totalApparentWeight > 0f)
        {
            durability -= Time.fixedDeltaTime * totalApparentWeight / standardLoadSeconds;
            if (durability <= 0f)
            {
                StartCoroutine(BreakAndResetRoutine());
                return;
            }
        }
        else
        {
            durability = Mathf.Min(1f, durability + recoveryPerSecond * Time.fixedDeltaTime);
        }

        UpdateDamageAppearance();
    }

    public void Configure(float loadSeconds, float recoverySpeed, float resetSeconds)
    {
        standardLoadSeconds = Mathf.Max(0.25f, loadSeconds);
        recoveryPerSecond = Mathf.Max(0f, recoverySpeed);
        brokenResetSeconds = Mathf.Max(0.1f, resetSeconds);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsStandingOnPlatform(collision))
        {
            return;
        }

        PlayerWeight weight = collision.collider.GetComponentInParent<PlayerWeight>();
        if (weight != null)
        {
            riders.Add(weight);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsStandingOnPlatform(collision))
        {
            PlayerWeight weight = collision.collider.GetComponentInParent<PlayerWeight>();
            if (weight != null)
            {
                riders.Add(weight);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerWeight weight = collision.collider.GetComponentInParent<PlayerWeight>();
        if (weight != null)
        {
            riders.Remove(weight);
        }
    }

    private IEnumerator BreakAndResetRoutine()
    {
        broken = true;
        durability = 0f;
        riders.Clear();
        spriteRenderer.color = new Color(0.45f, 0.25f, 0.2f, 1f);
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = fallingGravity;
        body.freezeRotation = false;

        yield return new WaitForSeconds(brokenResetSeconds);

        platformCollider.enabled = false;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.SetPositionAndRotation(startPosition, startRotation);
        durability = 1f;
        spriteRenderer.color = originalColor;
        platformCollider.enabled = true;
        broken = false;
    }

    private void UpdateDamageAppearance()
    {
        Color warning = new(0.85f, 0.38f, 0.22f, 1f);
        spriteRenderer.color = Color.Lerp(warning, originalColor, durability);

        if (durability < 0.4f)
        {
            float shake = (1f - durability) * 0.035f;
            transform.position = startPosition + (Vector3)(Random.insideUnitCircle * shake);
        }
        else if (body.bodyType == RigidbodyType2D.Kinematic)
        {
            transform.position = startPosition;
        }
    }

    private static bool IsStandingOnPlatform(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
