using System;
using UnityEngine;

public static class SlamEvents
{
    // origin = world impact center
    // radius = effective radius
    // isPower = slam meter full
    public static event Action<Vector3, float, bool> OnSlamImpact;

    public static void Raise(Vector3 origin, float radius, bool isPower)
        => OnSlamImpact?.Invoke(origin, radius, isPower);
}
