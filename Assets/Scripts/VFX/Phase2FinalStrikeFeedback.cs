using MoreMountains.Feedbacks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Phase2FinalStrikeFeedback : MonoBehaviour
{
    public static Phase2FinalStrikeFeedback ActiveInstance { get; private set; }

    [Header("Scene-authored Feedback")]
    [SerializeField] private RuthlessTapSlashFeedback screenSlash;
    [SerializeField] private MMF_Player finalStrikeFlashFEEL;
    [SerializeField] private MMF_Player finalStrikeCameraShakeFEEL;
    [SerializeField] private MMF_Player finalStrikeHapticFEEL;

    private bool warnedMissingSlash;
    private bool warnedMissingFlash;
    private bool warnedMissingCameraShake;
    private bool warnedMissingHaptic;

    private void OnEnable() => ActiveInstance = this;

    public void Play(Vector3 worldStart, Vector3 worldEnd)
    {
        try
        {
            if (screenSlash != null)
                screenSlash.TriggerFinalStrike(worldStart, worldEnd);
            else
                WarnOnce(ref warnedMissingSlash, "Final screen Slash is not assigned");
        }
        catch (System.Exception exception)
        {
            WarnOnce(ref warnedMissingSlash, "Final screen Slash failed (" + exception.GetType().Name + ")");
        }

        try
        {
            if (finalStrikeFlashFEEL != null)
                finalStrikeFlashFEEL.PlayFeedbacks();
            else
                WarnOnce(ref warnedMissingFlash, "Final Strike Flash FEEL is not assigned");
        }
        catch (System.Exception exception)
        {
            WarnOnce(ref warnedMissingFlash, "Final Strike Flash FEEL failed (" + exception.GetType().Name + ")");
        }

        try
        {
            if (finalStrikeCameraShakeFEEL != null)
                finalStrikeCameraShakeFEEL.PlayFeedbacks(worldStart);
            else
                WarnOnce(ref warnedMissingCameraShake, "Final Strike Camera Shake FEEL is not assigned");
        }
        catch (System.Exception exception)
        {
            WarnOnce(ref warnedMissingCameraShake, "Final Strike Camera Shake FEEL failed (" + exception.GetType().Name + ")");
        }

        try
        {
            if (!SkateRunnerHaptics.CanPlay) return;
            if (finalStrikeHapticFEEL != null)
                finalStrikeHapticFEEL.PlayFeedbacks();
            else
                WarnOnce(ref warnedMissingHaptic, "Final Strike Haptic FEEL is not assigned");
        }
        catch (System.Exception exception)
        {
            WarnOnce(ref warnedMissingHaptic, "Final Strike Haptic FEEL failed (" + exception.GetType().Name + ")");
        }
    }

    private void WarnOnce(ref bool flag, string message)
    {
        if (flag) return;
        flag = true;
        Debug.LogWarning("[Phase 2 Presentation] " + message + "; gameplay continues.", this);
    }

    private void OnDisable()
    {
        if (ActiveInstance == this) ActiveInstance = null;
    }
}
