using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Removes the authored Debug Tools panel from the temporary scene copy used by
/// non-development player builds. The source scene is never modified.
/// </summary>
public sealed class SkateRunnerDebugReleaseStripper : IProcessSceneWithReport
{
    private const string DebugButtonName = "Button_DebugTools";
    private const string DebugPopupName = "DebugToolsPopup";

    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // A null report means this callback is not processing a Player build
        // (for example, an Editor scene operation). Keep the tools in the Editor.
        if (report == null || IsDevelopmentBuild(report)) return;

        int removedCount = StripDebugArtifacts(scene);
        if (removedCount > 0)
        {
            Debug.Log($"[Build] Removed {removedCount} Debug Tools object/component(s) from release scene '{scene.path}'.");
        }
    }

    internal static int StripDebugArtifacts(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;

        int removedCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            SkateRunnerDebugToolsController[] controllers =
                roots[rootIndex].GetComponentsInChildren<SkateRunnerDebugToolsController>(true);
            for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
            {
                Object.DestroyImmediate(controllers[controllerIndex]);
                removedCount++;
            }
        }

        // Re-read the roots because deleting components can invalidate Unity's
        // native object wrappers during build-scene processing.
        roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            removedCount += DestroyNamedObjectsRecursively(roots[rootIndex].transform);
        }

        return removedCount;
    }

    private static bool IsDevelopmentBuild(BuildReport report)
    {
        return (report.summary.options & BuildOptions.Development) != 0;
    }

    private static int DestroyNamedObjectsRecursively(Transform current)
    {
        if (current.name == DebugButtonName || current.name == DebugPopupName)
        {
            Object.DestroyImmediate(current.gameObject);
            return 1;
        }

        int removedCount = 0;
        for (int childIndex = current.childCount - 1; childIndex >= 0; childIndex--)
        {
            removedCount += DestroyNamedObjectsRecursively(current.GetChild(childIndex));
        }

        return removedCount;
    }
}
