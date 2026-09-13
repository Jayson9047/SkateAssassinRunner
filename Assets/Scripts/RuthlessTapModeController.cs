using MoreMountains.InfiniteRunnerEngine;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

public enum RuthlessComboRank { None, KillerAssassin, Brutal, Ruthless }

public class RuthlessTapModeController : MonoBehaviour, MMEventListener<MMGameEvent>
{
    public static RuthlessTapModeController Instance { get; private set; }
    public static event System.Action<int> CompletedSuccessfully;

    public static RuthlessComboRank RankForCount(int count)
        => count >= 16 ? RuthlessComboRank.Ruthless : count >= 11 ? RuthlessComboRank.Brutal :
           count >= 6 ? RuthlessComboRank.KillerAssassin : RuthlessComboRank.None;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Mode Settings")]
    [SerializeField] private float defaultDurationSeconds = 6f;

    [Header("Combo Display")]
    [SerializeField] private string comboFormat = "COMBO x{0}";
    [SerializeField] private string idleText = "";


    public bool IsActive => _active;
    // TapOnlyMainActionZone owns accepted taps, their audio and the visible combo.
    // Read that same count for the mode-end callback instead of a second counter.
    public int TapCount => LevelManager.Instance != null ? LevelManager.Instance.RuthlessTapCount : 0;

    private bool _active;
    private float _endAtUnscaledTime;

    private System.Action<int> _onEnded; // optional callback

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetComboText(idleText);
    }

    private void Update()
    {
        if (!_active) return;

        if (Time.unscaledTime >= _endAtUnscaledTime)
        {
            End();
        }
    }

    public void Begin(float durationSeconds = -1f, System.Action<int> onEnded = null)
    {
        _active = true;

        _onEnded = onEnded;

        float dur = durationSeconds > 0f ? durationSeconds : defaultDurationSeconds;
        _endAtUnscaledTime = Time.unscaledTime + dur;

        SetComboText(string.Format(comboFormat, TapCount));

    }


    public void End()
    {
        if (!_active) return;
        var lm = LevelManager.Instance;
        var gm = GameManager.Instance;
        if (lm == null || !lm.RuthlessTapModeEntered || gm == null ||
            gm.Status == GameManager.GameStatus.LifeLost || gm.Status == GameManager.GameStatus.GameOver)
        {
            Cancel();
            return;
        }

        _active = false;

        int final = TapCount;
        var onEnded = _onEnded;
        _onEnded = null;
        // Capture and publish before the gameplay callback exits/resets the mode.
        CompletedSuccessfully?.Invoke(final);
        onEnded?.Invoke(final);

        // You can keep the combo on screen, or clear it:
        // SetComboText(idleText);
    }

    public void Cancel()
    {
        bool wasActive = _active;
        _active = false;
        _onEnded = null;
        if (wasActive) LevelManager.Instance?.ExitRuthlessTapMode();
    }

    public void OnMMEvent(MMGameEvent e)
    {
        if (e.EventName == "LifeLost" || e.EventName == "GameOver") Cancel();
    }

    private void OnEnable() => this.MMEventStartListening<MMGameEvent>();

    private void OnDisable()
    {
        this.MMEventStopListening<MMGameEvent>();
        Cancel();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void SetComboText(string text)
    {
        if (comboText == null) return;
        comboText.text = text;
    }
}
