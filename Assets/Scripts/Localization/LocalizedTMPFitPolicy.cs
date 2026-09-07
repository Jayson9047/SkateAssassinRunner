using TMPro;
using UnityEngine;

/// <summary>
/// Fits only translations exceeding the authored label's usable bounds.
/// Never learn the baseline from an auto-sized runtime result or rebuild TMP
/// from TEXT_CHANGED_EVENT: that callback runs inside mesh generation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTMPFitPolicy : MonoBehaviour
{
    [Min(1f)] public float maxFontSize = 36f;
    [Min(1f)] public float minFontSize = 28.8f;
    public bool allowAutoSize = true;
    public bool fitToSingleLine = true;
    [HideInInspector, Min(1)] public int referenceCharacterCount = 1;
    [SerializeField, TextArea] private string authoredText;
    [SerializeField] private bool baselineCaptured;

    private TMP_Text target;
    private string lastText;
    private Vector2 lastRect;
    private bool pending = true;

    public string AuthoredText => authoredText;
    public float SizeRatio => target != null ? target.fontSize / maxFontSize : 1f;

    private void Awake() => target = GetComponent<TMP_Text>();
    private void OnEnable() => pending = true;
    private void OnRectTransformDimensionsChange() => pending = true;

    private void OnValidate()
    {
        maxFontSize = Mathf.Max(1f, maxFontSize);
        minFontSize = Mathf.Clamp(minFontSize, maxFontSize * 0.8f, maxFontSize);
        pending = true;
        // Never mutate TMP/scene serialization during component validation.
    }

    public void Configure(float maximum, float minimum, bool autoSize, bool singleLine, int referenceCharacters)
    {
        if (target == null) target = GetComponent<TMP_Text>();
        if (!baselineCaptured)
        {
            maxFontSize = maximum;
            authoredText = target.text;
            baselineCaptured = true;
        }
        minFontSize = Mathf.Clamp(minimum, maxFontSize * 0.8f, maxFontSize);
        allowAutoSize = autoSize;
        fitToSingleLine = singleLine && !authoredText.TrimEnd().Contains("\n");
        referenceCharacterCount = Mathf.Max(1, referenceCharacters);
        Apply();
    }

    public void Apply() => pending = true;

    private void LateUpdate()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        Vector2 rect = target.rectTransform.rect.size;
        if (!pending && lastText == target.text && lastRect == rect) return;
        if (rect.x <= 1f || rect.y <= 1f) return;
        pending = false;
        lastText = target.text;
        lastRect = rect;

        target.enableAutoSizing = false;
        target.fontSize = maxFontSize;
        if (!allowAutoSize || string.IsNullOrEmpty(lastText)) return;

        float width = Mathf.Max(1f, rect.x - target.margin.x - target.margin.z);
        float height = Mathf.Max(1f, rect.y - target.margin.y - target.margin.w);
        // Some LayerLabs labels intentionally extend beyond their text rects.
        // Retain the authored footprint instead of shrinking them in English.
        Vector2 reference = Measure(authoredText ?? string.Empty, width);
        width = Mathf.Max(width, reference.x + 0.5f);
        height = Mathf.Max(height, reference.y);
        Vector2 preferred = Measure(lastText, width);
        if (Fits(preferred, width, height)) return;

        float low = Mathf.Max(minFontSize, maxFontSize * 0.8f);
        float high = maxFontSize;
        float best = low;
        for (int i = 0; i < 10; i++)
        {
            float trial = (low + high) * 0.5f;
            target.fontSize = trial;
            if (Fits(Measure(lastText, width), width, height))
            {
                best = trial;
                low = trial;
            }
            else high = trial;
        }
        target.fontSize = best;
    }

    private Vector2 Measure(string value, float width) => target.GetPreferredValues(
        value, fitToSingleLine || !target.enableWordWrapping ? 32767f : width, 32767f);

    private static bool Fits(Vector2 size, float width, float height) =>
        size.x <= width - 0.5f && size.y <= height + 0.1f;
}
