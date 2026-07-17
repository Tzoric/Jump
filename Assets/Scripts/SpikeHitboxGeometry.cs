using UnityEngine;

public static class SpikeHitboxGeometry
{
    public const int PathCount = 3;
    public const int PointsPerPath = 3;

    private static readonly Vector2[][] Paths =
    {
        new[]
        {
            new Vector2(-.65625f, -.3125f),
            new Vector2(-.3854167f, -.3125f),
            new Vector2(-.5208333f, .3333333f)
        },
        new[]
        {
            new Vector2(-.1145833f, -.3125f),
            new Vector2(.15625f, -.3125f),
            new Vector2(.0208333f, .3333333f)
        },
        new[]
        {
            new Vector2(.4270833f, -.3125f),
            new Vector2(.6979167f, -.3125f),
            new Vector2(.5625f, .3333333f)
        }
    };

    public static PolygonCollider2D AddCollider(GameObject target)
    {
        PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;
        collider.pathCount = PathCount;
        for (int path = 0; path < PathCount; path++) collider.SetPath(path, Paths[path]);
        return collider;
    }

    public static Vector2 ExpectedPoint(int path, int point) => Paths[path][point];
}
