using DG.Tweening;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>
/// Cosmetic, unscaled pause-menu animation layered over the existing
/// GUIManager/LevelSelector pause behavior.
/// </summary>
[DisallowMultipleComponent]
public sealed class PausePresentationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform blurBackground;
    [SerializeField] private RectTransform menuFrame;
    [SerializeField] private LevelSelector resumeAction;

    [Header("Unscaled Timing")]
    [SerializeField, Min(0f)] private float entranceDuration = 0.25f;
    [SerializeField, Min(0f)] private float exitDuration = 0.18f;
    [SerializeField, Range(0.75f, 1f)] private float entranceFrameScale = 0.9f;
    [SerializeField, Range(0.75f, 1f)] private float entranceBlurScale = 0.96f;

    private Sequence sequence;
    private bool exiting;

    private void OnEnable()
    {
        exiting = false;
        sequence?.Kill();

        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
        }

        if (menuFrame != null) menuFrame.localScale = Vector3.one * entranceFrameScale;
        if (blurBackground != null) blurBackground.localScale = Vector3.one * entranceBlurScale;

        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (pauseCanvasGroup != null) sequence.Join(pauseCanvasGroup.DOFade(1f, entranceDuration));
        if (menuFrame != null) sequence.Join(menuFrame.DOScale(1f, entranceDuration).SetEase(Ease.OutBack));
        if (blurBackground != null) sequence.Join(blurBackground.DOScale(1f, entranceDuration).SetEase(Ease.OutCubic));
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;
        exiting = false;
    }

    /// <summary>Assigned only to Resume; restart and menu retain their original actions.</summary>
    public void RequestResume()
    {
        if (exiting) return;
        exiting = true;
        sequence?.Kill();

        if (pauseCanvasGroup == null || exitDuration <= 0f)
        {
            CompleteResume();
            return;
        }

        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = true;

        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        sequence.Join(pauseCanvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));
        if (menuFrame != null) sequence.Join(menuFrame.DOScale(0.96f, exitDuration).SetEase(Ease.InCubic));
        if (blurBackground != null) sequence.Join(blurBackground.DOScale(0.98f, exitDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(CompleteResume);
    }

    private void CompleteResume()
    {
        if (resumeAction != null)
        {
            resumeAction.Resume();
            return;
        }

        // Fail-safe: presentation references must never trap the player paused.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnPause();
        }
        else
        {
            Debug.LogWarning("[PausePresentation] No resume action or GameManager is available.", this);
        }
    }
}
