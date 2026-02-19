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

    [Header("FEEL - Hit Stop On Enemy Kill")]
    [SerializeField] private MMF_Player enemyKillHitStopFeel;
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
        if (_slowMoActive) return;
        _slowMoActive = true;

        Time.timeScale = slowMoScale;

        bool affectsPhysics = affectsPhysicsOverride && slowMoAffectsPhysics;
        if (affectsPhysics)
            Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;

        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _restoreRoutine = StartCoroutine(RestoreSlowMoAfterRealtime(slowMoDurationRealtime, affectsPhysics));
    }

    private IEnumerator RestoreSlowMoAfterRealtime(float seconds, bool affectsPhysics)
    {
        yield return new WaitForSecondsRealtime(seconds);

        float startScale = Time.timeScale;
        float restoreDuration = 0.1f; // tweak: 0.08–0.12 sweet spot
        float t = 0f;

        while (t < restoreDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / restoreDuration);

            Time.timeScale = Mathf.Lerp(startScale, 1f, alpha);

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

    // --- Public API (static convenience) ---
    public static void TriggerSlowMoStatic(float slowMoScale, float slowMoDurationRealtime, bool affectsPhysicsOverride = true)
    {
        Ensure();
        _instance.TriggerSlowMo(slowMoScale, slowMoDurationRealtime, affectsPhysicsOverride);
    }
}
