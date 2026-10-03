using System;
using UnityEngine;

/// <summary>Deferred store-entitlement restoration boundary for Skate Assassin Runner.</summary>
public static class SkateAssassinRunnerPurchaseRestoration
{
    private static Action<Action<bool>> restoreBackend;
    private static bool restoring;
    public static event Action AvailabilityChanged;
    public static bool IsAvailable => restoreBackend != null && !restoring;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        restoreBackend = null;
        restoring = false;
        AvailabilityChanged = null;
    }

    // The future initialized IAP service registers a real entitlement recovery operation here.
    public static void RegisterBackend(Action<Action<bool>> backend)
    {
        restoreBackend = backend;
        AvailabilityChanged?.Invoke();
    }

    public static bool TryRestore(Action<bool> completed)
    {
        if (!IsAvailable) return false;
        restoring = true;
        AvailabilityChanged?.Invoke();
        bool callbackReceived = false;
        Action<bool> finish = success =>
        {
            if (callbackReceived) return;
            callbackReceived = true;
            restoring = false;
            AvailabilityChanged?.Invoke();
            completed?.Invoke(success);
        };
        try { restoreBackend(finish); }
        catch (Exception exception)
        {
            Debug.LogWarning("[Skate Assassin Runner Purchases] Restore failed: " + exception.Message);
            finish(false);
        }
        return true;
    }
}
