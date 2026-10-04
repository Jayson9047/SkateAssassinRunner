using System.Collections;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Shows the Elroi card before any loading work begins, then hands off to the
/// project's existing More Mountains loading-screen pipeline.
/// </summary>
[DisallowMultipleComponent]
public sealed class ElroiBootSplashController : MonoBehaviour
{
    [Header("Boot Timing (unscaled seconds)")]
    [SerializeField, Min(0f)] private float holdDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField] private string destinationScene = "SkateRunnerStartScreen";
    [SerializeField] private string firstLevelScene = "SkateRunner";

    [Header("Optional First-run Intro (disabled until a real cutscene is assigned)")]
    [SerializeField] private bool enableFirstRunIntro;
    [SerializeField] private PlayableDirector firstRunIntro;

    [Header("References")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;

        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 1f;
            splashCanvasGroup.interactable = false;
            splashCanvasGroup.blocksRaycasts = false;
        }

        // Deliberately do not begin async loading during this hold.
        yield return WaitRealtime(holdDuration);

        if (splashCanvasGroup != null && fadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            splashCanvasGroup.alpha = 0f;
        }

        if (string.IsNullOrWhiteSpace(destinationScene))
        {
            Debug.LogError("[ElroiBootSplash] Destination scene is empty; boot flow cannot continue.", this);
            yield break;
        }

        if (!SkateRunnerFirstRunProgress.HasPlayedFirstLevel && enableFirstRunIntro && firstRunIntro != null
            && firstRunIntro.playableAsset != null && !ES3.Load(SkateRunnerFirstRunProgress.IntroSeenKey, false))
        {
            firstRunIntro.gameObject.SetActive(true);
            firstRunIntro.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            firstRunIntro.extrapolationMode = DirectorWrapMode.None;
            firstRunIntro.Play();
            yield return null;
            while (firstRunIntro != null && firstRunIntro.state == PlayState.Playing) yield return null;
            ES3.Save(SkateRunnerFirstRunProgress.IntroSeenKey, true);
        }

        MMSceneLoadingManager.LoadScene(SkateRunnerFirstRunProgress.ResolveDestination(destinationScene, firstLevelScene));
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float endTime = Time.realtimeSinceStartup + Mathf.Max(0f, duration);
        while (Time.realtimeSinceStartup < endTime)
        {
            yield return null;
        }
    }
}
