using UnityEngine;

public enum MineRouteSectionType
{
    VerticalUp,
    AngledUp,
    Horizontal,
    VerticalDown
}

public sealed class MineRouteSection : MonoBehaviour
{
    [SerializeField, Min(1)] private int order = 1;
    [SerializeField] private MineRouteSectionType sectionType;
    [SerializeField] private Vector2 entry;
    [SerializeField] private Vector2 exit;
    [SerializeField, Min(0f)] private float pathLength;

    public int Order => order;
    public MineRouteSectionType SectionType => sectionType;
    public Vector2 Entry => entry;
    public Vector2 Exit => exit;
    public float PathLength => pathLength;

    public void Configure(int sectionOrder, MineRouteSectionType type, Vector2 entryPoint,
        Vector2 exitPoint, float authoredPathLength)
    {
        order = Mathf.Max(1, sectionOrder);
        sectionType = type;
        entry = entryPoint;
        exit = exitPoint;
        pathLength = Mathf.Max(0f, authoredPathLength);
    }
}
