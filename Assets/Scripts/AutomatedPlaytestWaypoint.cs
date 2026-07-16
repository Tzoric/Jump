using UnityEngine;

public enum AutomatedWaypointMode
{
    GroundedLanding,
    AirbornePass
}

public sealed class AutomatedPlaytestWaypoint : MonoBehaviour
{
    [SerializeField, Min(0)] private int order;
    [SerializeField] private AutomatedWaypointMode mode;
    [SerializeField, Min(.25f)] private float reachRadius = .65f;
    [SerializeField] private bool deployParachute;
    [SerializeField] private bool usePowerJump;

    public int Order => order;
    public AutomatedWaypointMode Mode => mode;
    public float ReachRadius => reachRadius;
    public bool DeployParachute => deployParachute;
    public bool UsePowerJump => usePowerJump;

    public void Configure(int routeOrder, AutomatedWaypointMode waypointMode = AutomatedWaypointMode.GroundedLanding,
        float radius = .65f, bool holdParachute = false, bool requirePowerJump = false)
    {
        order = Mathf.Max(0, routeOrder);
        mode = waypointMode;
        reachRadius = Mathf.Max(.25f, radius);
        deployParachute = holdParachute;
        usePowerJump = requirePowerJump;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}
