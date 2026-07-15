using UnityEngine;

public sealed class AutomatedPlaytestWaypoint : MonoBehaviour
{
    [SerializeField, Min(0)] private int order;

    public int Order => order;

    public void Configure(int routeOrder)
    {
        order = Mathf.Max(0, routeOrder);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
