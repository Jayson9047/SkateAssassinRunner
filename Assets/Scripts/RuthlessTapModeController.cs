using MoreMountains.InfiniteRunnerEngine;
using TMPro;
using UnityEngine;

public class RuthlessTapModeController : MonoBehaviour
{
    public static RuthlessTapModeController Instance { get; private set; }

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

        _active = false;

        int final = TapCount;
        _onEnded?.Invoke(final);
        _onEnded = null;

        // You can keep the combo on screen, or clear it:
        // SetComboText(idleText);
    }

    private void SetComboText(string text)
    {
        if (comboText == null) return;
        comboText.text = text;
    }
}
