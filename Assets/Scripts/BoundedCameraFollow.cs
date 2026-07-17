using UnityEngine;

public sealed class BoundedCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minimum;
    [SerializeField] private Vector2 maximum;
    [SerializeField] private Vector2 lookAhead = new(1.5f, 1f);
    [SerializeField] private float smoothTime = 0.18f;
    private Vector3 velocity;
    private LevelEntranceDoor entrance;

    public void Configure(Transform followTarget, Vector2 min, Vector2 max, Vector2 ahead)
    {
        target = followTarget;
        minimum = min;
        maximum = max;
        lookAhead = ahead;
    }

    private void Awake() => entrance = FindFirstObjectByType<LevelEntranceDoor>();

    private void LateUpdate()
    {
        if (target == null) return;
        float facing = target.GetComponent<SpriteRenderer>()?.flipX == true ? -1f : 1f;
        Vector3 trackedPosition = entrance != null && !entrance.IsComplete
            ? entrance.GameplayPosition
            : target.position;
        Vector3 desired = new(
            Mathf.Clamp(trackedPosition.x + lookAhead.x * facing, minimum.x, maximum.x),
            Mathf.Clamp(trackedPosition.y + lookAhead.y, minimum.y, maximum.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
