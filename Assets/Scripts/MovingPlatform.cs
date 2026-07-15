using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 travelOffset = new(5f, 0f);
    [SerializeField, Min(0.1f)] private float speed = 2.5f;
    [SerializeField, Min(0f)] private float endpointPause = 0.5f;

    private Rigidbody2D body;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private bool waiting;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        startPosition = body.position;
        targetPosition = startPosition + travelOffset;
    }

    private void FixedUpdate()
    {
        if (waiting)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(body.position, targetPosition, speed * Time.fixedDeltaTime);
        body.MovePosition(next);

        if (Vector2.Distance(next, targetPosition) <= 0.01f)
        {
            StartCoroutine(PauseAndReverse());
        }
    }

    public void Configure(Vector2 offset, float movementSpeed, float pause)
    {
        travelOffset = offset;
        speed = Mathf.Max(0.1f, movementSpeed);
        endpointPause = Mathf.Max(0f, pause);
    }

    private IEnumerator PauseAndReverse()
    {
        waiting = true;
        yield return new WaitForSeconds(endpointPause);
        targetPosition = Vector2.Distance(targetPosition, startPosition) < 0.01f
            ? startPosition + travelOffset
            : startPosition;
        waiting = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody != null && IsStandingOnPlatform(collision))
        {
            collision.transform.SetParent(transform, true);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.parent == transform)
        {
            collision.transform.SetParent(null, true);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 from = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Vector3 to = from + (Vector3)travelOffset;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireCube(to, transform.lossyScale);
    }
}
