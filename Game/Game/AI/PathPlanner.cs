using Godot;
using System.Collections.Generic;

/// <summary>
/// Pure-geometry utility. No Godot nodes, no scene tree, no state.
/// Computes a waypoint list (1–2 points) that routes around rectangular obstacles.
/// </summary>
public static class PathPlanner
{
    /// <summary>
    /// Returns an ordered list of intermediate waypoints from start to destination,
    /// routing around any Rect2 obstacles. Returns an empty list when the direct path
    /// is clear — caller should interpret that as "go direct".
    /// </summary>
    public static List<Vector2> ComputeWaypoints(
        Vector2 start,
        Vector2 destination,
        IEnumerable<Rect2> obstacles)
    {
        // Pre-expand all obstacles by the clearance margin once
        var expanded = new List<Rect2>();
        foreach (var o in obstacles)
            expanded.Add(Expand(o, AIConstants.ObstacleClearance));

        // Special case: start is inside an obstacle (e.g. enemy walked into ZoC).
        // Exit straight to the nearest edge rather than routing to a corner.
        foreach (var rect in expanded)
        {
            if (PointInsideRect(start, rect))
                return new List<Vector2> { NearestEdgeExit(start, rect) };
        }

        int blockerIdx = FindFirstIntersection(start, destination, expanded);
        if (blockerIdx < 0)
            return new List<Vector2>(); // direct path clear

        // Two candidate detour corners — one per side of the blocking obstacle
        (Vector2 cornerA, Vector2 cornerB) = GetExtremalCorners(start, destination, expanded[blockerIdx]);

        // Build both candidate routes; return the shorter one
        List<Vector2> routeA = BuildRoute(cornerA, destination, expanded);
        List<Vector2> routeB = BuildRoute(cornerB, destination, expanded);

        return TotalLength(start, destination, routeA) <= TotalLength(start, destination, routeB)
            ? routeA : routeB;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a 1- or 2-waypoint route starting with firstCorner.
    /// If firstCorner → destination is itself blocked, a second waypoint is added.
    /// </summary>
    private static List<Vector2> BuildRoute(Vector2 firstCorner, Vector2 destination, List<Rect2> expanded)
    {
        var route = new List<Vector2> { firstCorner };

        int blockerIdx = FindFirstIntersection(firstCorner, destination, expanded);
        if (blockerIdx >= 0)
        {
            (Vector2 c1, Vector2 c2) = GetExtremalCorners(firstCorner, destination, expanded[blockerIdx]);
            // Of the two second-leg candidates, pick the one that minimises distance for this leg
            float d1 = firstCorner.DistanceTo(c1) + c1.DistanceTo(destination);
            float d2 = firstCorner.DistanceTo(c2) + c2.DistanceTo(destination);
            route.Add(d1 <= d2 ? c1 : c2);
        }

        return route;
    }

    /// <summary>
    /// Returns the index of the first obstacle in <paramref name="expanded"/> whose
    /// expanded rect intersects the segment a→b, or -1 if the path is clear.
    /// </summary>
    private static int FindFirstIntersection(Vector2 a, Vector2 b, List<Rect2> expanded)
    {
        for (int i = 0; i < expanded.Count; i++)
            if (SegmentIntersectsRect(a, b, expanded[i]))
                return i;
        return -1;
    }

    /// <summary>
    /// Returns the two corners of <paramref name="rect"/> that are on opposite sides
    /// of the line a→b (the "left" extremal and "right" extremal), using the cross
    /// product Z-component as the signed-distance measure.
    /// These form the clockwise and counter-clockwise detour candidates.
    /// </summary>
    private static (Vector2 left, Vector2 right) GetExtremalCorners(Vector2 a, Vector2 b, Rect2 rect)
    {
        Vector2 tl = rect.Position;
        Vector2 tr = new(rect.Position.X + rect.Size.X, rect.Position.Y);
        Vector2 bl = new(rect.Position.X,               rect.Position.Y + rect.Size.Y);
        Vector2 br = rect.Position + rect.Size;

        float dx = b.X - a.X;
        float dy = b.Y - a.Y;

        float maxCross = float.MinValue;
        float minCross = float.MaxValue;
        Vector2 leftCorner = tl, rightCorner = tl;

        foreach (var c in new[] { tl, tr, bl, br })
        {
            // Z component of (b-a) × (c-a): positive = c is left of a→b
            float cross = dx * (c.Y - a.Y) - dy * (c.X - a.X);
            if (cross > maxCross) { maxCross = cross; leftCorner  = c; }
            if (cross < minCross) { minCross = cross; rightCorner = c; }
        }

        return (leftCorner, rightCorner);
    }

    /// <summary>
    /// Liang-Barsky AABB segment clip. Returns true if the segment a→b
    /// intersects (or is contained within) <paramref name="rect"/>.
    /// </summary>
    private static bool SegmentIntersectsRect(Vector2 a, Vector2 b, Rect2 rect)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float t0 = 0f, t1 = 1f;

        // p[i] < 0 → entering that boundary; q[i] = signed distance to boundary from a
        float[] p = { -dx,                                   dx,
                      -dy,                                   dy };
        float[] q = { a.X - rect.Position.X,                rect.Position.X + rect.Size.X - a.X,
                      a.Y - rect.Position.Y,                 rect.Position.Y + rect.Size.Y - a.Y };

        const float eps = 1e-6f;
        for (int i = 0; i < 4; i++)
        {
            if (System.Math.Abs(p[i]) < eps)
            {
                if (q[i] < 0f) return false; // parallel and outside this boundary
            }
            else
            {
                float r = q[i] / p[i];
                if (p[i] < 0f) { if (r > t0) t0 = r; }
                else           { if (r < t1) t1 = r; }
                if (t0 > t1) return false;
            }
        }
        // Require a real interior interval, not just a boundary touch (t0==t1==0
        // occurs when a waypoint corner starts exactly on the rect edge and exits immediately)
        return t1 > t0 + eps;
    }

    private static bool PointInsideRect(Vector2 point, Rect2 rect) =>
        point.X > rect.Position.X && point.X < rect.Position.X + rect.Size.X &&
        point.Y > rect.Position.Y && point.Y < rect.Position.Y + rect.Size.Y;

    /// <summary>
    /// Returns the closest point on the perimeter of <paramref name="rect"/> to
    /// <paramref name="point"/>, which must be strictly inside the rect.
    /// The enemy moves to this point, exiting through the nearest wall.
    /// </summary>
    private static Vector2 NearestEdgeExit(Vector2 point, Rect2 rect)
    {
        float dLeft   = point.X - rect.Position.X;
        float dRight  = rect.Position.X + rect.Size.X - point.X;
        float dTop    = point.Y - rect.Position.Y;
        float dBottom = rect.Position.Y + rect.Size.Y - point.Y;

        float min = System.Math.Min(System.Math.Min(dLeft, dRight), System.Math.Min(dTop, dBottom));

        if (min == dLeft)   return new Vector2(rect.Position.X,                   point.Y);
        if (min == dRight)  return new Vector2(rect.Position.X + rect.Size.X,     point.Y);
        if (min == dTop)    return new Vector2(point.X, rect.Position.Y);
                            return new Vector2(point.X, rect.Position.Y + rect.Size.Y);
    }

    private static Rect2 Expand(Rect2 rect, float margin)
    {
        var m = new Vector2(margin, margin);
        return new Rect2(rect.Position - m, rect.Size + m * 2f);
    }

    private static float TotalLength(Vector2 start, Vector2 destination, List<Vector2> waypoints)
    {
        float total = 0f;
        Vector2 prev = start;
        foreach (var wp in waypoints) { total += prev.DistanceTo(wp); prev = wp; }
        return total + prev.DistanceTo(destination);
    }
}
