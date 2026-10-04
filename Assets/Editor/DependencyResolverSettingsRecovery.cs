using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Retry EDM settings saves rejected by a temporary Windows memory-mapped file lock.</summary>
[InitializeOnLoad]
internal static class DependencyResolverSettingsRecovery
{
    private const int MaximumAttempts = 6;
    private static bool pending;
    private static bool retrying;
    private static bool retryFailed;
    private static bool retryableLock;
    private static int attempts;
    private static double retryAt;

    static DependencyResolverSettingsRecovery()
    {
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Update;
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Error || !message.Contains("Unable to write to") ||
            !message.Contains("GvhProjectSettings.xml")) return;

        if (retrying) { retryFailed = true; retryableLock = message.Contains("1224"); return; }
        if (!message.Contains("1224")) return;
        if (pending) return;
        attempts = 0;
        pending = true;
        retryAt = EditorApplication.timeSinceStartup + 1d;
    }

    private static void Update()
    {
        if (!pending || EditorApplication.timeSinceStartup < retryAt ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        pending = false;
        retrying = true;
        retryFailed = false;
        retryableLock = false;
        attempts++;
        try
        {
            Type settings = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Google.ProjectSettings")).FirstOrDefault(t => t != null);
            MethodInfo save = settings?.GetMethod("Save", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (save == null) throw new MissingMethodException("Google.ProjectSettings.Save");
            save.Invoke(null, null);
        }
        catch (Exception exception)
        {
            // Other failure types need their own diagnosis; do not keep retrying them.
            Debug.LogWarning("Dependency resolver settings recovery could not run: " + exception.GetBaseException().Message);
            return;
        }
        finally { retrying = false; }

        if (!retryFailed)
            Debug.Log("Dependency resolver settings saved after a temporary Windows file lock.");
        else if (retryableLock && attempts < MaximumAttempts)
        {
            pending = true;
            retryAt = EditorApplication.timeSinceStartup + Math.Min(30d, Math.Pow(2d, attempts));
        }
        else if (retryableLock)
            Debug.LogWarning("Dependency resolver settings are still locked after six retries. Close the application holding GvhProjectSettings.xml, then save the resolver settings again.");
        else
            Debug.LogWarning("Dependency resolver settings could not be saved. See the preceding resolver error for details.");
    }
}
