using System.Globalization;
using Febucci.TextAnimatorCore.Time;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using UnityEngine;

/// <summary>Presentation only for the PowerMeter decision countdown.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(TextAnimator_TMP))]
public sealed class Phase2CountdownUrgency : MonoBehaviour
{
    [SerializeField, Min(1)] private int urgencyStartSeconds = 5;
    [SerializeField] private Color urgencyColor = new Color(1f, 0.09f, 0.09f);
    [SerializeField, Min(0.1f)] private float startingSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float finalSpeed = 3.5f;
    [Header("TAP prompt pulse")]
    [SerializeField, Range(0.5f, 1f)] private float minimumPulseScale = 0.94f;
    [SerializeField, Range(1f, 1.5f)] private float maximumPulseScale = 1.18f;
    [SerializeField, Min(0.1f), Tooltip("Size pulses per second at the urgency threshold.")]
    private float startingPulseFrequency = 2f;
    [SerializeField, Min(0.1f), Tooltip("Size pulses per second at one second remaining.")]
    private float finalPulseFrequency = 4.2f;
    private TextAnimator_TMP animator;
    private Vector3 normalScale;
    private bool urgencyPresentationActive;
    private float pulsePhase;
    private string lastMarkup;
    private int lastSecond = -1;
    private bool countdownActive;
    private int renderedThreshold;
    private float renderedStartingSpeed, renderedFinalSpeed;
    private Color renderedColor;

    public int UrgencyStartSeconds => Mathf.Max(1, urgencyStartSeconds);
    public bool IsUrgent => countdownActive && lastSecond > 0 && lastSecond <= UrgencyStartSeconds;

    private void Awake() => Configure();

    private void Configure()
    {
        if (animator == null) animator = GetComponent<TextAnimator_TMP>();
        animator.sharedSettings = null;
        animator.localSettings.timeScale = TimeScale.Unscaled;
        animator.animationLoop = AnimationLoop.LateUpdate;
    }

    private float UrgencyProgress(int seconds) => UrgencyStartSeconds == 1 ? 1f
        : Mathf.Clamp01((UrgencyStartSeconds - seconds) / (float)(UrgencyStartSeconds - 1));

    private void LateUpdate()
    {
        if (!urgencyPresentationActive || !IsUrgent) return;
        // TAP uses both Text Animator glyph motion and an unscaled size pulse.
        // Keep this pulse continuous across digits; updating text every frame would
        // restart the glyph effects and make the countdown appear static.
        float frequency = Mathf.Lerp(startingPulseFrequency,
            Mathf.Max(startingPulseFrequency, finalPulseFrequency), UrgencyProgress(lastSecond));
        pulsePhase = Mathf.Repeat(pulsePhase + Time.unscaledDeltaTime * frequency * Mathf.PI * 2f,
            Mathf.PI * 2f);
        float scale = Mathf.Lerp(minimumPulseScale, maximumPulseScale,
            (Mathf.Sin(pulsePhase) + 1f) * 0.5f);
        transform.localScale = normalScale * scale;
    }

    private void SetUrgencyPresentation(bool urgent)
    {
        if (urgencyPresentationActive == urgent) return;
        Configure();
        urgencyPresentationActive = urgent;
        if (urgent)
        {
            normalScale = transform.localScale;
            pulsePhase = Mathf.PI * 1.5f;
        }
        else
        {
            transform.localScale = normalScale;
        }
    }

    public string BuildCountdownMarkup(int seconds)
    {
        seconds = Mathf.Clamp(seconds, 0, 99);
        string digits = seconds.ToString("00", CultureInfo.InvariantCulture);
        if (seconds == 0 || seconds > UrgencyStartSeconds) return digits;
        float progress = UrgencyProgress(seconds);
        float speed = Mathf.Lerp(startingSpeed, Mathf.Max(startingSpeed, finalSpeed), progress);
        float amplitude = 0.55f; // Match the accepted TAP prompt's glyph movement.
        return "<color=#" + ColorUtility.ToHtmlStringRGB(urgencyColor)
            + "><shake a=" + amplitude.ToString("0.###", CultureInfo.InvariantCulture)
            + " s=" + speed.ToString("0.###", CultureInfo.InvariantCulture)
            + "><bounce a=0.2 s=" + (speed * 1.2f).ToString("0.###", CultureInfo.InvariantCulture)
            + ">" + digits + "</bounce></shake></color>";
    }

    public void ShowCountdown(int seconds)
    {
        seconds = Mathf.Clamp(seconds, 0, 99);
        if (countdownActive && lastSecond == seconds && renderedThreshold == UrgencyStartSeconds
            && renderedStartingSpeed == startingSpeed && renderedFinalSpeed == finalSpeed
            && renderedColor == urgencyColor) return;
        countdownActive = true;
        lastSecond = seconds;
        renderedThreshold = UrgencyStartSeconds;
        renderedStartingSpeed = startingSpeed;
        renderedFinalSpeed = finalSpeed;
        renderedColor = urgencyColor;
        SetUrgencyPresentation(IsUrgent);
        SetMarkup(BuildCountdownMarkup(lastSecond));
    }

    public void ShowPlain(string text)
    {
        countdownActive = false;
        lastSecond = -1;
        SetUrgencyPresentation(false);
        SetMarkup(text);
    }

    public void StopUrgency()
    {
        if (!countdownActive) return;
        countdownActive = false;
        SetUrgencyPresentation(false);
        SetMarkup(Mathf.Max(0, lastSecond).ToString("00", CultureInfo.InvariantCulture));
    }

    private void SetMarkup(string markup)
    {
        // The gameplay countdown calls every frame. Parse only a changed second/style.
        if (lastMarkup == markup) return;
        Configure();
        lastMarkup = markup;
        animator.SetText(markup, false);
    }

    private void OnDisable()
    {
        StopUrgency();
        lastMarkup = null;
    }
}
