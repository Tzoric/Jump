using UnityEngine;

public sealed class VerticalCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float fixedX;
    [SerializeField] private float minimumY = 2f;
    [SerializeField] private float maximumY = 33f;
    [SerializeField] private float verticalLookAhead = 1.4f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.16f;

    private float verticalVelocity;
    private LevelEntranceDoor entrance;

    public void Configure(Transform followTarget, float x, float minY, float maxY, float lookAhead)
    {
        target = followTarget;
        fixedX = x;
        minimumY = minY;
        maximumY = Mathf.Max(minY, maxY);
        verticalLookAhead = lookAhead;
    }

    private void Awake() => entrance = FindFirstObjectByType<LevelEntranceDoor>();

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float targetY = entrance != null && !entrance.IsComplete
            ? entrance.GameplayPosition.y
            : target.position.y;
        float desiredY = Mathf.Clamp(targetY + verticalLookAhead, minimumY, maximumY);
        float y = Mathf.SmoothDamp(transform.position.y, desiredY, ref verticalVelocity, smoothTime);
        transform.position = new Vector3(fixedX, y, transform.position.z);
    }
}
