using UnityEngine;
using DG.Tweening;

public class Phase2PowerSlamFrameEvents : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerMeetPoint;

    [Header("Tween")]
    [SerializeField] private float launchDuration = 0.45f;
    [SerializeField] private float apexHeight = 2.0f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool _armedForThisSlam;
    private bool _launchedThisSlam;
    private Tween _tween;

    private void OnEnable()
    {
        // Pool-safe reset
        _armedForThisSlam = false;
        _launchedThisSlam = false;

        _tween?.Kill();
        _tween = null;
    }

    /// <summary>
    /// Call this ONLY when the PHASE2 down-attack button starts the special slam.
    /// This arms the frame-18 animation event to do the launch.
    /// </summary>
    public void ArmPhase2LaunchForNextSlam()
    {
        _armedForThisSlam = true;
        _launchedThisSlam = false; // reset per arm

        if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] ARMED");
    }

    /// <summary>
    /// Animation Event on frame 18 of the slam clip.
    /// </summary>
    public void OnPhase2PowerSlamFrame18()
    {
        if (!_armedForThisSlam)
        {
            if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] Frame18 fired but NOT armed (ignored)");
            return;
        }

        if (_launchedThisSlam)
        {
            if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] Frame18 fired but already launched (ignored)");
            return;
        }

        // Consume arm so other downattacks don't trigger
        _armedForThisSlam = false;
        _launchedThisSlam = true;

        if (playerMeetPoint == null)
        {
            Debug.LogError("[Phase2PowerSlamFrameEvents] playerMeetPoint is NULL.");
            return;
        }

        StartLaunchToMeetPoint(playerMeetPoint.position);

        //Transform playerRoot = transform.root;

        //Vector3 start = playerRoot.position;
        //Vector3 end = playerMeetPoint.position;

        //Vector3 mid = (start + end) * 0.5f;
        //mid.y += apexHeight;

        //Vector3[] path = new[] { start, mid, end };

        //_tween?.Kill();
        //_tween = playerRoot
        //    .DOPath(path, launchDuration, PathType.CatmullRom, PathMode.Full3D)
        //    .SetEase(ease);

        //if (debugLogs) Debug.Log("[Phase2PowerSlamFrameEvents] LAUNCH started via Frame18");
    }

    private void StartLaunchToMeetPoint(Vector3 end)
    {
        _tween?.Kill();

        Vector3 adjustedEnd = end + Vector3.up * 0.1f;

        _tween = transform
            .DOMove(adjustedEnd, launchDuration)
            .SetEase(ease);
    }


    public void EndExecutionAndReturnToGameplay()
    {
        // 1) stop any tweens on the body
        _tween?.Kill();

        // 2) snap body back to its normal local position under the player root
        //transform.localPosition = _originalLocalPos;
        //transform.localRotation = _originalLocalRot;

        // 3) re-enable your normal movement / gravity systems (whatever you disabled)
    }
}
