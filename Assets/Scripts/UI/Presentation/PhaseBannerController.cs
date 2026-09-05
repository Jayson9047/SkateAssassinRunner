using DG.Tweening;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One reusable phase banner. It listens to the authoritative gameplay events
/// and animates entirely on unscaled time so Phase 2 slow motion cannot affect it.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhaseBannerController : MonoBehaviour, MMEventListener<MMGameEvent>
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image bannerImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private Sprite phase1Sprite;
    [SerializeField] private Sprite phase2Sprite;

    [Header("Glow")]
    [SerializeField] private Color phase1Glow = new Color(0.208f, 0.812f, 1f, 0.46f);
    [SerializeField] private Color phase2Glow = new Color(1f, 0.353f, 0.18f, 0.46f);

    [Header("Unscaled Motion")]
    [SerializeField, Min(0f)] private float phase1Delay = 1.5f;
    [SerializeField, Min(0f)] private float entranceDuration = 0.33f;
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0f)] private float exitDuration = 0.42f;
    [SerializeField] private float entranceOffsetY = 300f;
    [SerializeField] private float exitOffsetY = 55f;
    [SerializeField, Range(0.5f, 1f)] private float entranceScale = 0.88f;

    private RectTransform rectTransform;
    private Vector2 restingPosition;
    private Sequence sequence;
    private bool phase1Shown;
    private bool warnedMissingReference;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        restingPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        HideImmediately();
    }

    private void OnEnable()
    {
        this.MMEventStartListening<MMGameEvent>();
        Phase2CarApproachController.OnPhase2ApproachStarted += HandlePhase2ApproachStarted;
    }

    private void OnDisable()
    {
        this.MMEventStopListening<MMGameEvent>();
        Phase2CarApproachController.OnPhase2ApproachStarted -= HandlePhase2ApproachStarted;
        sequence?.Kill();
        sequence = null;
    }

    public void OnMMEvent(MMGameEvent eventType)
    {
        if (eventType.EventName != "GameStart" || phase1Shown) return;
        phase1Shown = true;
        Show(phase1Sprite, phase1Glow, SkateRunnerAudioManager.PlayPhase1BannerImpact, phase1Delay);
    }

    private void HandlePhase2ApproachStarted()
    {
        Show(phase2Sprite, phase2Glow, SkateRunnerAudioManager.PlayPhase2BannerImpact, 0f);
    }

    public void PreviewPhase1() => Show(phase1Sprite, phase1Glow, SkateRunnerAudioManager.PlayPhase1BannerImpact, 0f);
    public void PreviewPhase2() => Show(phase2Sprite, phase2Glow, SkateRunnerAudioManager.PlayPhase2BannerImpact, 0f);

    private void Show(Sprite sprite, Color glowColor, System.Action playAudio, float delay)
    {
        if (canvasGroup == null || bannerImage == null || rectTransform == null || sprite == null)
        {
            if (!warnedMissingReference)
            {
                warnedMissingReference = true;
                Debug.LogWarning("[PhaseBanner] Required UI references or phase sprite are missing; banner skipped safely.", this);
            }
            return;
        }

        sequence?.Kill();
        bannerImage.sprite = sprite;
        bannerImage.preserveAspect = true;
        if (glowImage != null) glowImage.color = glowColor;

        rectTransform.anchoredPosition = restingPosition + Vector2.up * entranceOffsetY;
        rectTransform.localScale = Vector3.one * entranceScale;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (delay > 0f) sequence.AppendInterval(delay);
        sequence.AppendCallback(() => playAudio?.Invoke());
        sequence.Append(rectTransform.DOAnchorPos(restingPosition, entranceDuration).SetEase(Ease.OutBack));
        sequence.Join(rectTransform.DOScale(1f, entranceDuration).SetEase(Ease.OutBack));
        sequence.Join(canvasGroup.DOFade(1f, entranceDuration).SetEase(Ease.OutCubic));
        sequence.AppendInterval(holdDuration);
        sequence.Append(rectTransform.DOAnchorPos(restingPosition + Vector2.up * exitOffsetY, exitDuration).SetEase(Ease.InCubic));
        sequence.Join(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(HideImmediately);
    }

    private void HideImmediately()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = restingPosition;
            rectTransform.localScale = Vector3.one;
        }
    }
}
