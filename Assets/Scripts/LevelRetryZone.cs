using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelRetryZone : MonoBehaviour
{
    [SerializeField] private Vector3 resetPosition;
    [SerializeField] private string message = "Back to the start — try the upper route again.";

    public Vector3 ResetPosition => resetPosition;

    public void Configure(Vector3 position, string retryMessage = null)
    {
        resetPosition = position;
        if (!string.IsNullOrWhiteSpace(retryMessage)) message = retryMessage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HeroMovement movement = other.GetComponentInParent<HeroMovement>();
        if (movement == null) return;

        Rigidbody2D body = movement.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = resetPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            movement.transform.position = resetPosition;
        }

        MineRunInventory inventory = movement.GetComponent<MineRunInventory>();
        if (inventory != null) inventory.ShowMessage(message);
    }
}
