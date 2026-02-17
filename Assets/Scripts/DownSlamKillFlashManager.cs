using UnityEngine;
using MoreMountains.Feedbacks;

public class DownSlamKillFlashManager : MonoBehaviour
{
    public static DownSlamKillFlashManager Instance { get; private set; }

    [Header("Flash")]
    [SerializeField] private MMF_Player downKillFlash;

    [Header("Timing")]
    [SerializeField] private float killWindowSeconds = 0.35f;   // how long after slam we count kills as "from slam"
    [SerializeField] private float flashCooldownSeconds = 0.10f; // prevent spam if multiple enemies die instantly

    private float _windowEndsAt = -1f;
    private float _nextFlashAllowedAt = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Call this exactly when downslam impact happens.
    public void ArmKillWindow()
    {
        _windowEndsAt = Time.unscaledTime + killWindowSeconds;
    }

    /// Call this when any enemy dies.
    public void NotifyEnemyDied()
    {
        if (Time.unscaledTime > _windowEndsAt) return;
        if (Time.unscaledTime < _nextFlashAllowedAt) return;

        _nextFlashAllowedAt = Time.unscaledTime + flashCooldownSeconds;
        if (downKillFlash != null) downKillFlash.PlayFeedbacks();
    }
}
