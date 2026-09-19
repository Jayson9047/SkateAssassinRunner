using UnityEngine;

/// <summary>Continuous overlap for two boxes translating relative to each other.</summary>
public static class SweptBoxIntersection
{
    public static bool Intersects(Vector3 from, Vector3 to, Vector3 hazardHalf,
        Vector3 bodyX, Vector3 bodyY, Vector3 bodyZ)
    {
        float enter = 0f, exit = 1f;
        Vector3 delta = to - from;
        if (!Axis(Vector3.right, from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
            !Axis(Vector3.up, from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
            !Axis(Vector3.forward, from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
            !Axis(Vector3.Cross(bodyY, bodyZ), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
            !Axis(Vector3.Cross(bodyZ, bodyX), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
            !Axis(Vector3.Cross(bodyX, bodyY), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit))
            return false;
        for (int i = 0; i < 3; i++)
        {
            Vector3 a = i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;
            if (!Axis(Vector3.Cross(a, bodyX), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
                !Axis(Vector3.Cross(a, bodyY), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit) ||
                !Axis(Vector3.Cross(a, bodyZ), from, delta, hazardHalf, bodyX, bodyY, bodyZ, ref enter, ref exit))
                return false;
        }
        return true;
    }

    private static bool Axis(Vector3 axis, Vector3 from, Vector3 delta, Vector3 half,
        Vector3 bx, Vector3 by, Vector3 bz, ref float enter, ref float exit)
    {
        if (axis.sqrMagnitude < 0.00000001f) return true;
        axis.Normalize();
        float radius = Mathf.Abs(axis.x)*half.x + Mathf.Abs(axis.y)*half.y + Mathf.Abs(axis.z)*half.z
            + Mathf.Abs(Vector3.Dot(axis,bx)) + Mathf.Abs(Vector3.Dot(axis,by)) + Mathf.Abs(Vector3.Dot(axis,bz));
        float origin = Vector3.Dot(from, axis), speed = Vector3.Dot(delta, axis);
        if (Mathf.Abs(speed) < 0.000001f) return Mathf.Abs(origin) <= radius;
        float a = (-radius-origin)/speed, b = (radius-origin)/speed;
        if (a > b) { float swap = a; a = b; b = swap; }
        enter = Mathf.Max(enter, a); exit = Mathf.Min(exit, b);
        return enter <= exit;
    }
}

