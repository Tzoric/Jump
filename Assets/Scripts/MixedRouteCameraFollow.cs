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
    [SerializeField, Min(.01f)] private float smoothTime = .16f;

    private Rigidbody2D targetBody;
    private ParachuteDescentController parachute;
    private Vector3 velocity;
    private float framedY;
    private bool initialized;
    private bool wasLookingDown;

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

    private void Awake() => CacheTarget();

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

        float facing = target.GetComponent<SpriteRenderer>()?.flipX == true ? -1f : 1f;
        bool lookingDown = parachute != null && parachute.IsCameraTrackingDescent;
        float verticalAhead = lookingDown ? -downwardLookAhead : upwardLookAhead;
        float requestedY = target.position.y + verticalAhead;
        if (lookingDown != wasLookingDown)
        {
            framedY = requestedY;
            velocity = Vector3.zero;
            wasLookingDown = lookingDown;
        }
        else if (requestedY > framedY + verticalDeadZone)
        {
            framedY = requestedY - verticalDeadZone;
        }
        else if (requestedY < framedY - verticalDeadZone)
        {
            framedY = requestedY + verticalDeadZone;
        }

        float requestedX = lookingDown
            ? parachute.DescentCenterX
            : target.position.x + horizontalLookAhead * facing;
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
        float facing = target.GetComponent<SpriteRenderer>()?.flipX == true ? -1f : 1f;
        bool lookingDown = parachute != null && parachute.IsCameraTrackingDescent;
        float verticalAhead = lookingDown ? -downwardLookAhead : upwardLookAhead;
        framedY = target.position.y + verticalAhead;
        float requestedX = lookingDown
            ? parachute.DescentCenterX
            : target.position.x + horizontalLookAhead * facing;
        transform.position = new Vector3(
            Mathf.Clamp(requestedX, minimum.x, maximum.x),
            Mathf.Clamp(framedY, minimum.y, maximum.y),
            transform.position.z);
        velocity = Vector3.zero;
        wasLookingDown = lookingDown;
        initialized = true;
    }
}
