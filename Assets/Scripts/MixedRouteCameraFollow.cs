using UnityEngine;

public sealed class MixedRouteCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minimum;
    [SerializeField] private Vector2 maximum;
    [SerializeField] private float horizontalLookAhead = 1.8f;
    [SerializeField] private float upwardLookAhead = 1.2f;
    [SerializeField] private float downwardLookAhead = 3.2f;
    [SerializeField, Min(.01f)] private float smoothTime = .16f;

    private Rigidbody2D targetBody;
    private ParachuteDescentController parachute;
    private Vector3 velocity;

    public Vector2 Minimum => minimum;
    public Vector2 Maximum => maximum;

    public void Configure(Transform followTarget, Vector2 min, Vector2 max)
    {
        target = followTarget;
        minimum = min;
        maximum = max;
        CacheTarget();
    }

    private void Awake() => CacheTarget();

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

        float facing = target.GetComponent<SpriteRenderer>()?.flipX == true ? -1f : 1f;
        bool lookingDown = (parachute != null && parachute.IsInDescentZone) ||
                           (targetBody != null && targetBody.linearVelocityY < -1f);
        float verticalAhead = lookingDown ? -downwardLookAhead : upwardLookAhead;
        Vector3 desired = new(
            Mathf.Clamp(target.position.x + horizontalLookAhead * facing, minimum.x, maximum.x),
            Mathf.Clamp(target.position.y + verticalAhead, minimum.y, maximum.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
