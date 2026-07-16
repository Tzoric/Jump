using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ParachuteDescentZone : MonoBehaviour
{
    [SerializeField, Min(20f)] private float minimumDepth = 24f;

    public float MinimumDepth => minimumDepth;

    public void Configure(float depth)
    {
        minimumDepth = Mathf.Max(20f, depth);
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        other.GetComponentInParent<ParachuteDescentController>()?.EnterDescentZone();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        other.GetComponentInParent<ParachuteDescentController>()?.ExitDescentZone();
    }
}
