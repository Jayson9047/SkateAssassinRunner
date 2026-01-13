using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
public class PowerMeter : MonoBehaviour
{
    public enum ZoneResult
    {
        Red,
        Yellow,
        Green,
        Cyan
    }

    [Serializable]
    public class PowerMeterResultEvent : UnityEvent<ZoneResult, float> { }
    // float is normalized value 0..1 (0 bottom, 1 top)

    [Header("Config")]
    [SerializeField] private PowerMeterConfig config;

    [Header("UI References")]
    [Tooltip("The RectTransform that defines the meter area height.")]
    [SerializeField] private RectTransform meterArea;

    [Tooltip("The ticker line RectTransform that moves up/down.")]
    [SerializeField] private RectTransform ticker;

    [Header("Optional Zone Images (auto-sized)")]
    [SerializeField] private RectTransform zoneRed;
    [SerializeField] private RectTransform zoneYellow;
    [SerializeField] private RectTransform zoneGreen;
    [SerializeField] private RectTransform zoneCyan;

    [Header("Optional Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText; // you can swap to TMP later if you want

    [Header("Meter Padding (keeps zones/ticker away from border)")]
    [SerializeField] private float verticalPadding = 5f;


    [Header("Events")]
    public UnityEvent OnStarted;
    public UnityEvent OnStopped;
    public PowerMeterResultEvent OnResult;

    // Runtime
    private bool _running;
    private float _phase;          // 0..1 progress within a cycle
    private float _normalizedValue; // 0..1 current ticker position

    public bool IsRunning => _running;
    public float CurrentNormalizedValue => _normalizedValue;

    private void Awake()
    {
        ValidateRefs();
        ApplyZoneVisualsFromConfig();
    }

    private void Update()
    {
        if (!_running || config == null) return;

        // Advance phase (cycles per second)
        _phase += Time.deltaTime * config.speed;

        // keep phase manageable
        if (_phase > 1000f) _phase -= Mathf.Floor(_phase);

        float t = _phase;

        // We want a back-and-forth motion in [0..1]
        // Option A: smooth sin wave
        // Option B: linear pingpong
        float v;
        if (config.smoothMotion)
        {
            // sin wave mapped to 0..1
            v = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;
        }
        else
        {
            v = Mathf.PingPong(t, 1f);
        }

        SetTickerNormalized(v);
    }

    public void StartMeter()
    {
        if (config == null)
        {
            Debug.LogError("[PowerMeter] Missing config.");
            return;
        }

        ValidateRefs();
        ApplyZoneVisualsFromConfig();

        _running = true;

        if (config.randomizeStartPosition)
        {
            float start = UnityEngine.Random.Range(0f, 1f);
            // We set phase so value begins near start.
            // For smooth motion it's not exact, but good enough for gameplay feel.
            _phase = start;
            SetTickerNormalized(start);
        }

        OnStarted?.Invoke();
    }

    public void StopMeterAndEvaluate()
    {
        if (!_running) return;

        _running = false;
        OnStopped?.Invoke();

        ZoneResult result = Evaluate(_normalizedValue);

        if (feedbackText != null)
        {
            feedbackText.text = result.ToString();
        }

        OnResult?.Invoke(result, _normalizedValue);
    }

    public ZoneResult Evaluate(float normalizedValue)
    {
        if (config == null) return ZoneResult.Red;

        // Priority: cyan > green > yellow > red (in case of overlaps)
        if (config.cyan.Contains(normalizedValue)) return ZoneResult.Cyan;
        if (config.green.Contains(normalizedValue)) return ZoneResult.Green;
        if (config.yellow.Contains(normalizedValue)) return ZoneResult.Yellow;
        return ZoneResult.Red;
    }

    public void ResetTickerTo(float normalizedValue)
    {
        SetTickerNormalized(Mathf.Clamp01(normalizedValue));
    }

    private void SetTickerNormalized(float v)
    {
        v = Mathf.Clamp01(v);
        _normalizedValue = v;

        if (meterArea == null || ticker == null) return;

        float height = meterArea.rect.height;

        // Inner usable height (border-safe)
        float innerHeight = Mathf.Max(0f, height - (verticalPadding*2));

        // Bottom/top limits within the rect
        float bottomY = (-height * 0.5f) + verticalPadding;
        float topY = (height * 0.5f) - verticalPadding;

        // 0 -> bottom, 1 -> top (inside padded area)
        float y = Mathf.Lerp(bottomY, topY, v);

        Vector2 pos = ticker.anchoredPosition;
        pos.y = y;
        ticker.anchoredPosition = pos;
    }

    private void ApplyZoneVisualsFromConfig()
    {
        if (config == null || meterArea == null) return;

        float height = meterArea.rect.height;

        // Helper to place zone rects by normalized min/max
        void PlaceZone(RectTransform rt, PowerMeterConfig.ZoneRange range)
        {
            if (rt == null) return;

            // Inner usable range
            float bottomY = (-height * 0.5f) + verticalPadding;
            float topY = (height * 0.5f) - verticalPadding;

            float minY = Mathf.Lerp(bottomY, topY, range.min);
            float maxY = Mathf.Lerp(bottomY, topY, range.max);

            float zoneHeight = Mathf.Max(0f, maxY - minY);

            // We keep zones centered in their segment
            Vector2 size = rt.sizeDelta;
            size.y = zoneHeight;
            rt.sizeDelta = size;

            Vector2 pos = rt.anchoredPosition;
            pos.y = (minY + maxY) * 0.5f;
            rt.anchoredPosition = pos;
        }

        PlaceZone(zoneRed, config.red);
        PlaceZone(zoneYellow, config.yellow);
        PlaceZone(zoneGreen, config.green);
        PlaceZone(zoneCyan, config.cyan);
    }

    private void ValidateRefs()
    {
        if (meterArea == null)
        {
            Debug.LogError("[PowerMeter] meterArea not assigned.");
        }
        if (ticker == null)
        {
            Debug.LogError("[PowerMeter] ticker not assigned.");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep zone visuals updated in editor
        if (!Application.isPlaying)
        {
            ApplyZoneVisualsFromConfig();
        }
    }
#endif
}
