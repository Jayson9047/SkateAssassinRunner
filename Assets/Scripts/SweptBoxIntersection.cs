using UnityEngine;

/// <summary>Continuous overlap for two boxes translating relative to each other.</summary>
public static class SweptBoxIntersection
{
    public struct BoxPose
    {
        public Vector3 Position, CenterOffset, HalfSize;
        public Quaternion Rotation;
        public Vector3 Center => Position + Rotation * CenterOffset;

        public static BoxPose Capture(BoxCollider box)
        {
            Vector3 scale = box.transform.lossyScale;
            Vector3 half = Vector3.Scale(box.size * 0.5f, scale);
            return new BoxPose { Position = box.transform.position, Rotation = box.transform.rotation,
                CenterOffset = Vector3.Scale(box.center, scale),
                HalfSize = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z)) };
        }

        public static BoxPose Lerp(BoxPose a, BoxPose b, float t)
        {
            return new BoxPose { Position = Vector3.Lerp(a.Position, b.Position, t),
                Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
                CenterOffset = Vector3.Lerp(a.CenterOffset, b.CenterOffset, t),
                HalfSize = Vector3.Lerp(a.HalfSize, b.HalfSize, t) };
        }
    }

    /// <summary>
    /// Sweep sampled world poses in a common coordinate frame. In particular, never join a
    /// previous sword-local player point to a current sword-local point after the sword rotates.
    /// Rotation uses bounded 10-degree intervals; translating hazards retain one continuous sweep.
    /// </summary>
    public static bool IntersectsMovingBoxes(BoxPose hazardFrom, BoxPose hazardTo, BoxPose bodyFrom, BoxPose bodyTo)
    {
        float angle = Mathf.Max(Quaternion.Angle(hazardFrom.Rotation, hazardTo.Rotation),
            Quaternion.Angle(bodyFrom.Rotation, bodyTo.Rotation));
        int steps = Mathf.Clamp(Mathf.CeilToInt(angle / 10f), 1, 18);
        for (int i = 0; i < steps; i++)
        {
            float a = (float)i / steps, b = (float)(i + 1) / steps;
            BoxPose h0 = BoxPose.Lerp(hazardFrom, hazardTo, a);
            BoxPose h1 = BoxPose.Lerp(hazardFrom, hazardTo, b);
            BoxPose p0 = BoxPose.Lerp(bodyFrom, bodyTo, a);
            BoxPose p1 = BoxPose.Lerp(bodyFrom, bodyTo, b);
            Quaternion frame = Quaternion.Inverse(Quaternion.Slerp(h0.Rotation, h1.Rotation, 0.5f));
            Quaternion bodyRotation = frame * Quaternion.Slerp(p0.Rotation, p1.Rotation, 0.5f);
            Vector3 bodyHalf = (p0.HalfSize + p1.HalfSize) * 0.5f;
            if (Intersects(frame * (p0.Center - h0.Center), frame * (p1.Center - h1.Center),
                (h0.HalfSize + h1.HalfSize) * 0.5f,
                bodyRotation * Vector3.right * bodyHalf.x,
                bodyRotation * Vector3.up * bodyHalf.y,
                bodyRotation * Vector3.forward * bodyHalf.z)) return true;
        }
        // Include the exact final pose as well as the intermediate rotational approximation.
        Quaternion finalFrame = Quaternion.Inverse(hazardTo.Rotation);
        Quaternion finalBodyRotation = finalFrame * bodyTo.Rotation;
        Vector3 center = finalFrame * (bodyTo.Center - hazardTo.Center);
        return Intersects(center, center, hazardTo.HalfSize,
            finalBodyRotation * Vector3.right * bodyTo.HalfSize.x,
            finalBodyRotation * Vector3.up * bodyTo.HalfSize.y,
            finalBodyRotation * Vector3.forward * bodyTo.HalfSize.z);
    }

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
