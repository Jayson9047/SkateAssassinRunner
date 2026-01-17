using UnityEngine;
using TMPro;

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
    public int TapCount => _tapCount;

    private bool _active;
    private int _tapCount;
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
        _tapCount = 0;

        _onEnded = onEnded;

        float dur = durationSeconds > 0f ? durationSeconds : defaultDurationSeconds;
        _endAtUnscaledTime = Time.unscaledTime + dur;

        SetComboText(string.Format(comboFormat, _tapCount));
    }

    public void RegisterTap()
    {
        if (!_active) return;

        _tapCount++;
        SetComboText(string.Format(comboFormat, _tapCount));
    }

    public void End()
    {
        if (!_active) return;

        _active = false;

        int final = _tapCount;
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
