using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class GUIPulse : MonoBehaviour
{
    public enum PulseStyle { PunchScale, ScaleYoyo, PopSequence }

    [Header("General")]
    [SerializeField] private PulseStyle pulseStyle = PulseStyle.PunchScale;
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Punch Settings")]
    [SerializeField] private float punchAmount = 0.2f;
    [SerializeField] private float punchDuration = 0.12f;
    [SerializeField] private int punchVibrato = 1;
    [SerializeField] private float punchElasticity = 0.6f;

    [Header("Scale Yoyo Settings")]
    [SerializeField] private float yoyoScale = 1.15f;
    [SerializeField] private float yoyoHalfDuration = 0.08f;
    [SerializeField] private Ease yoyoEaseOut = Ease.OutBack;
    [SerializeField] private Ease yoyoEaseIn = Ease.InBack;

    [Header("Pop Sequence Settings")]
    [SerializeField] private float popUpScale = 1.2f;
    [SerializeField] private float popUpDuration = 0.07f;
    [SerializeField] private float popDownDuration = 0.10f;
    [SerializeField] private Ease popUpEase = Ease.OutBack;
    [SerializeField] private Ease popDownEase = Ease.OutQuad;

    private RectTransform _rt;
    private Tween _tween;
    private Vector3 _baseScaleVec;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _baseScaleVec = Vector3.one * baseScale;
        _rt.localScale = _baseScaleVec;
    }

    public void Pulse()
    {
        if (_rt == null) return;

        _tween?.Kill();
        _rt.DOKill();
        _rt.localScale = _baseScaleVec;

        switch (pulseStyle)
        {
            case PulseStyle.PunchScale:
                _tween = _rt.DOPunchScale(
                        Vector3.one * punchAmount,
                        punchDuration,
                        punchVibrato,
                        punchElasticity)
                    .SetUpdate(useUnscaledTime);
                break;

            case PulseStyle.ScaleYoyo:
                _tween = _rt.DOScale(_baseScaleVec * yoyoScale, yoyoHalfDuration)
                    .SetEase(yoyoEaseOut)
                    .SetUpdate(useUnscaledTime)
                    .OnComplete(() =>
                    {
                        _tween = _rt.DOScale(_baseScaleVec, yoyoHalfDuration)
                            .SetEase(yoyoEaseIn)
                            .SetUpdate(useUnscaledTime);
                    });
                break;

            case PulseStyle.PopSequence:
                Sequence seq = DOTween.Sequence().SetUpdate(useUnscaledTime);
                seq.Append(_rt.DOScale(_baseScaleVec * popUpScale, popUpDuration).SetEase(popUpEase));
                seq.Append(_rt.DOScale(_baseScaleVec, popDownDuration).SetEase(popDownEase));
                _tween = seq;
                break;
        }
    }
}
