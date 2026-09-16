using DamageNumbersPro;
using IndieKit;
using MoreMountains.Tools;
using System.Collections;
using UnityEngine;

/// <summary>Gameplay GUI announcements and occasional world-space kill text using DNP pools.</summary>
public sealed class ArcadeAnnouncerPresentation : MonoBehaviour, MMEventListener<MMGameEvent>
{
    public static ArcadeAnnouncerPresentation Instance { get; private set; }

    [Header("Damage Numbers Pro")]
    [SerializeField] private DamageNumber announcementPrefab;
    [SerializeField] private DamageNumber rankAnnouncementPrefab;
    [SerializeField] private RectTransform announcementAnchor;
    [Header("Phase 2 Rank Timing")]
    [Tooltip("Unscaled seconds after successful Ruthless Tap completion before any of the three rank texts appears. Does not affect Powerslam or the text's visible lifetime.")]
    [SerializeField, Min(0f)] private float rankTextDelaySeconds = 0f;
    [Header("Enemy Kill Blood Popups")]
    [SerializeField] private DamageNumber enemyBloodPopupPrefab;
    [SerializeField, Range(2, 4)] private int minimumKillsBetweenBloodPopups = 2;
    [SerializeField, Range(2, 4)] private int maximumKillsBetweenBloodPopups = 4;
    [SerializeField, Min(0.1f)] private float enemyBloodPopupScale = 1f;
    [Header("Callout Intensity")]
    [SerializeField, Min(0.1f)] private float powerslamScale = 1.1f;
    [SerializeField, Min(0.1f)] private float killerAssassinScale = 1f;
    [SerializeField, Min(0.1f)] private float brutalScale = 1.12f;
    [SerializeField, Min(0.1f)] private float ruthlessScale = 1.25f;
    [SerializeField] private Color powerslamColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color killerAssassinColor = new Color(1f, 0.88f, 0.5f);
    [SerializeField] private Color brutalColor = new Color(1f, 0.55f, 0.12f);
    [SerializeField] private Color ruthlessColor = new Color(1f, 0.2f, 0.2f);

    private static readonly string[] BloodPhrases = { "Execution", "Critical", "Killed", "Dead" };
    // Cadence/phrase selection uses its own stream, separate from gameplay randomness.
    private readonly System.Random bloodRandom = new System.Random();
    private int killsUntilBloodPopup;
    private Coroutine pendingRankText;

    private void Awake()
    {
        Instance = this;
        ChooseNextBloodInterval();
    }

    private void Start()
    {
        if (announcementPrefab) announcementPrefab.PrewarmPool();
        if (rankAnnouncementPrefab && rankAnnouncementPrefab != announcementPrefab) rankAnnouncementPrefab.PrewarmPool();
        if (enemyBloodPopupPrefab) enemyBloodPopupPrefab.PrewarmPool();
    }

    private void OnEnable()
    {
        RuthlessTapModeController.CompletedSuccessfully += OnRankCompleted;
        this.MMEventStartListening<MMGameEvent>();
    }

    private void OnDisable()
    {
        RuthlessTapModeController.CompletedSuccessfully -= OnRankCompleted;
        this.MMEventStopListening<MMGameEvent>();
        CancelPendingRankText();
    }

    public void OnMMEvent(MMGameEvent e)
    {
        if (e.EventName == "GameStart" || e.EventName == "LifeLost") CancelPendingRankText();
    }

    private void OnRankCompleted(int finalCount)
    {
        CancelPendingRankText();
        if (!isActiveAndEnabled || RuthlessTapModeController.RankForCount(finalCount) == RuthlessComboRank.None) return;
        if (rankTextDelaySeconds <= 0f) ShowRank(finalCount);
        else pendingRankText = StartCoroutine(ShowRankAfterDelay(finalCount, rankTextDelaySeconds));
    }

    private IEnumerator ShowRankAfterDelay(int finalCount, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        pendingRankText = null;
        ShowRank(finalCount); // Use the captured result, never the subsequently reset tap counter.
    }

    private void CancelPendingRankText()
    {
        if (pendingRankText != null) StopCoroutine(pendingRankText);
        pendingRankText = null;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static void ShowPowerslam()
    {
        SkateRunnerAudioManager.PlayPowerslamAnnouncer();
        if (Instance) Instance.Show(Instance.announcementPrefab, "POWERSLAM!!!", Instance.powerslamScale, Instance.powerslamColor);
    }

    /// <summary>
    /// Called once by the authoritative destruction/cash handler, before reward checks.
    /// True replaces only this kill's cash text; the caller still awards the cash.
    /// </summary>
    public static bool TryShowEnemyKill(SkateRunnerDestructibleObject enemy, Vector3 cashPopupPosition)
    {
        if (!enemy || !enemy.CountsAsEnemyKill || !Instance || !Instance.isActiveAndEnabled || !Instance.enemyBloodPopupPrefab)
            return false;

        if (--Instance.killsUntilBloodPopup > 0) return false;

        Instance.ChooseNextBloodInterval();
        var phrase = BloodPhrases[Instance.bloodRandom.Next(BloodPhrases.Length)];
        var popup = Instance.enemyBloodPopupPrefab.Spawn(cashPopupPosition, phrase);
        if (!popup) return false;
        popup.SetScale(Instance.enemyBloodPopupScale);
        return true;
    }

    private void ChooseNextBloodInterval()
    {
        int minimum = Mathf.Clamp(minimumKillsBetweenBloodPopups, 2, 4);
        int maximum = Mathf.Clamp(maximumKillsBetweenBloodPopups, minimum, 4);
        killsUntilBloodPopup = bloodRandom.Next(minimum, maximum + 1);
    }

    private void ShowRank(int finalCount)
    {
        switch (RuthlessTapModeController.RankForCount(finalCount))
        {
            case RuthlessComboRank.KillerAssassin: Show(rankAnnouncementPrefab, "KILLER ASSASSIN", killerAssassinScale, killerAssassinColor); break;
            case RuthlessComboRank.Brutal: Show(rankAnnouncementPrefab, "BRUTAL!!!", brutalScale, brutalColor); break;
            case RuthlessComboRank.Ruthless: Show(rankAnnouncementPrefab, "RUTHLESS!!!", ruthlessScale, ruthlessColor); break;
        }
    }

    private void Show(DamageNumber prefab, string text, float scale, Color color)
    {
        if (!prefab || !announcementAnchor) return;
        var popup = prefab.SpawnGUI(announcementAnchor, Vector2.zero, text);
        popup.SetScale(scale);
        popup.SetColor(color);
    }
}
