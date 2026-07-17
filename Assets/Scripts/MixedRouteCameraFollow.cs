using UnityEngine;

public sealed class MixedRouteCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minimum;
    [SerializeField] private Vector2 maximum;
    [SerializeField] private float horizontalLookAhead = 1.8f;
    [SerializeField] private float upwardLookAhead = 1.2f;
    [SerializeField] private float downwardLookAhead = 2f;
    [SerializeField, Min(0f)] private float verticalDeadZone = 1.25f;
    [SerializeField, Min(.01f)] private float smoothTime = .22f;
    [SerializeField, Min(.01f)] private float lookAheadSmoothTime = .3f;
    [SerializeField, Min(.01f)] private float descentBlendTime = .28f;

    private Rigidbody2D targetBody;
    private ParachuteDescentController parachute;
    private LevelEntranceDoor entrance;
    private Vector3 velocity;
    private float framedY;
    private float horizontalOffset;
    private float horizontalOffsetVelocity;
    private float descentBlend;
    private float descentBlendVelocity;
    private bool initialized;

    public Vector2 Minimum => minimum;
    public Vector2 Maximum => maximum;
    public float VerticalDeadZone => verticalDeadZone;
    public float DownwardLookAhead => downwardLookAhead;

    public void Configure(Transform followTarget, Vector2 min, Vector2 max)
    {
        target = followTarget;
        minimum = min;
        maximum = max;
        CacheTarget();
    }

    private void Awake()
    {
        entrance = FindFirstObjectByType<LevelEntranceDoor>();
        CacheTarget();
    }

    private void Start() => SnapToTarget();

    private void CacheTarget()
    {
        if (target == null) return;
        targetBody = target.GetComponent<Rigidbody2D>();
        parachute = target.GetComponent<ParachuteDescentController>();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (targetBody == null) CacheTarget();

        if (!initialized)
        {
            SnapToTarget();
            return;
        }

        bool lookingDown = parachute != null && parachute.IsCameraTrackingDescent;
        descentBlend = Mathf.SmoothDamp(descentBlend, lookingDown ? 1f : 0f,
            ref descentBlendVelocity, descentBlendTime);

        float horizontalVelocity = targetBody == null ? 0f : targetBody.linearVelocityX;
        float requestedOffset = Mathf.Abs(horizontalVelocity) > .15f
            ? horizontalLookAhead * Mathf.Sign(horizontalVelocity)
            : 0f;
        horizontalOffset = Mathf.SmoothDamp(horizontalOffset, requestedOffset,
            ref horizontalOffsetVelocity, lookAheadSmoothTime);

        float verticalAhead = Mathf.Lerp(upwardLookAhead, -downwardLookAhead, descentBlend);
        Vector3 trackedPosition = entrance != null && !entrance.IsComplete
            ? entrance.GameplayPosition
            : target.position;
        float requestedY = trackedPosition.y + verticalAhead;
        if (requestedY > framedY + verticalDeadZone)
        {
            framedY = requestedY - verticalDeadZone;
        }
        else if (requestedY < framedY - verticalDeadZone)
        {
            framedY = requestedY + verticalDeadZone;
        }

        float routeX = trackedPosition.x + horizontalOffset;
        float descentX = parachute == null ? routeX : parachute.DescentCenterX;
        float requestedX = Mathf.Lerp(routeX, descentX, descentBlend);
        Vector3 desired = new(
            Mathf.Clamp(requestedX, minimum.x, maximum.x),
            Mathf.Clamp(framedY, minimum.y, maximum.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private void SnapToTarget()
    {
        if (target == null) return;
        CacheTarget();
        Vector3 trackedPosition = entrance != null && !entrance.IsComplete
            ? entrance.GameplayPosition
            : target.position;
        bool lookingDown = parachute != null && parachute.IsCameraTrackingDescent;
        descentBlend = lookingDown ? 1f : 0f;
        float verticalAhead = lookingDown ? -downwardLookAhead : upwardLookAhead;
        framedY = trackedPosition.y + verticalAhead;
        horizontalOffset = 0f;
        float routeX = trackedPosition.x + horizontalOffset;
        float requestedX = lookingDown && parachute != null ? parachute.DescentCenterX : routeX;
        transform.position = new Vector3(
            Mathf.Clamp(requestedX, minimum.x, maximum.x),
            Mathf.Clamp(framedY, minimum.y, maximum.y),
            transform.position.z);
        velocity = Vector3.zero;
        horizontalOffsetVelocity = 0f;
        descentBlendVelocity = 0f;
        initialized = true;
    }
}
