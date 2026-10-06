using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using IndieKit;

public class SkateRunnerGameFeel : MonoBehaviour
{
    private static SkateRunnerGameFeel _instance;

    [SerializeField] private bool slowMoAffectsPhysics = true;

    private float _defaultFixedDeltaTime;
    private bool _slowMoActive;
    private Coroutine _restoreRoutine;
    private float _activeSlowMoScale = 1f;

    public static float GetGameplayTimeScale(float normalScale) =>
        _instance != null && _instance._slowMoActive ? _instance._activeSlowMoScale : normalScale;

    private static bool MenuPaused => MoreMountains.InfiniteRunnerEngine.GameManager.Instance != null
        && MoreMountains.InfiniteRunnerEngine.GameManager.Instance.Status
            == MoreMountains.InfiniteRunnerEngine.GameManager.GameStatus.Paused;

    [Header("FEEL - Hit Stop On Enemy Kill")]
    [SerializeField] private MMF_Player enemyKillHitStopFeel;
    

    [Header("FEEL - Camera Shake On Enemy Kill")]
    [SerializeField] private MMF_Player enemyKillCameraShakeFeel;
    [SerializeField, Range(0f, 1f)] private float enemyKillCameraShakeIntensity = 0.05f;
[SerializeField] private float hitStopMinIntervalRealtime = 0.10f; // safety: never spam hitstop faster than this

    private int _lastHitStopAttackId = -1;
    private KillCause _lastHitStopCause = KillCause.Unknown;
    private float _lastHitStopTime = -999f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    /// <summary>
    /// Call this once somewhere (e.g., in your bootstrap scene),
    /// or it will auto-create itself the first time you call TriggerSlowMoStatic.
    /// </summary>
    public static void Ensure()
    {
        if (_instance != null) return;

        var go = new GameObject(nameof(SkateRunnerGameFeel));
        _instance = go.AddComponent<SkateRunnerGameFeel>();
    }

    // --- Public API (instance) ---
    private void TriggerSlowMo(float slowMoScale, float slowMoDurationRealtime, bool affectsPhysicsOverride = true)
    {
        if (_slowMoActive || MenuPaused) return;
        _slowMoActive = true;
        _activeSlowMoScale = Mathf.Clamp(slowMoScale, 0.01f, 1f);

        Time.timeScale = _activeSlowMoScale;

        bool affectsPhysics = affectsPhysicsOverride && slowMoAffectsPhysics;
        if (affectsPhysics)
            Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;

        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _restoreRoutine = StartCoroutine(RestoreSlowMoAfterRealtime(slowMoDurationRealtime, affectsPhysics));
    }

    private IEnumerator RestoreSlowMoAfterRealtime(float seconds, bool affectsPhysics)
    {
        // Real time for slow motion, but menu pause must preserve its remaining
        // duration and must never let this coroutine unpause the game.
        float remaining = Mathf.Max(0f, seconds);
        while (remaining > 0f)
        {
            if (!MenuPaused) remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        float startScale = _activeSlowMoScale;
        float restoreDuration = 0.1f; // tweak: 0.08�0.12 sweet spot
        float t = 0f;

        while (t < restoreDuration)
        {
            if (MenuPaused) { yield return null; continue; }
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / restoreDuration);

            _activeSlowMoScale = Mathf.Lerp(startScale, 1f, alpha);
            Time.timeScale = _activeSlowMoScale;

            if (affectsPhysics)
                Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;

            yield return null;
        }

        Time.timeScale = 1f;

        if (affectsPhysics)
            Time.fixedDeltaTime = _defaultFixedDeltaTime;

        _slowMoActive = false;
        _restoreRoutine = null;
    }

    public static void TriggerEnemyKillHitStopStatic(KillCause cause, int attackId)
    {
        Ensure();
        _instance.TriggerEnemyKillHitStop(cause, attackId);
    }

    private void TriggerEnemyKillHitStop(KillCause cause, int attackId)
    {
        if (enemyKillHitStopFeel == null) return;

        // Gate: one hit stop per attack instance
        if (attackId != 0 && attackId == _lastHitStopAttackId && cause == _lastHitStopCause)
            return;

        // Extra safety: never allow hit stop spam even if attackId isn't set somewhere
        if (Time.unscaledTime - _lastHitStopTime < hitStopMinIntervalRealtime)
            return;

        _lastHitStopAttackId = attackId;
        _lastHitStopCause = cause;
        _lastHitStopTime = Time.unscaledTime;

        enemyKillHitStopFeel.PlayFeedbacks();
    }

public static void TriggerEnemyKillCameraShakeStatic(Vector3 worldPosition)
    {
        Ensure();
        if (_instance.enemyKillCameraShakeFeel == null) return;

        _instance.enemyKillCameraShakeFeel.PlayFeedbacks(
            worldPosition,
            _instance.enemyKillCameraShakeIntensity);
    }

    public static void TriggerCameraShakeStatic(Vector3 worldPosition, float intensity)
    {
        if (intensity <= 0f) return;
        Ensure();
        if (_instance.enemyKillCameraShakeFeel == null) return;

        _instance.enemyKillCameraShakeFeel.PlayFeedbacks(worldPosition, Mathf.Clamp01(intensity));
    }



    // --- Public API (static convenience) ---
    public static void TriggerSlowMoStatic(float slowMoScale, float slowMoDurationRealtime, bool affectsPhysicsOverride = true)
    {
        Ensure();
        _instance.TriggerSlowMo(slowMoScale, slowMoDurationRealtime, affectsPhysicsOverride);
    }
}
