using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class OscillatingHazard : MonoBehaviour
{
    [SerializeField] private Vector2 axis = Vector2.right;
    [SerializeField, Min(0.5f)] private float travelDistance = 4f;
    [SerializeField, Min(0.05f)] private float cyclesPerSecond = .28f;
    [SerializeField] private float phase;

    private Rigidbody2D body;
    private Vector2 origin;

    public float TravelDistance => travelDistance;

    public void Configure(Vector2 motionAxis, float distance, float cycles, float phaseOffset)
    {
        axis = motionAxis.sqrMagnitude < .01f ? Vector2.right : motionAxis.normalized;
        travelDistance = Mathf.Max(.5f, distance);
        cyclesPerSecond = Mathf.Max(.05f, cycles);
        phase = phaseOffset;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        origin = body.position;
    }

    private void FixedUpdate()
    {
        float wave = Mathf.Sin((Time.time * cyclesPerSecond + phase) * Mathf.PI * 2f);
        body.MovePosition(origin + axis.normalized * (wave * travelDistance));
    }
}
