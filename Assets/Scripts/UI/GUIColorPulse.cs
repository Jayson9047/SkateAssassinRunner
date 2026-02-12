using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TMP_Text))]
public class GUIColorPulse : MonoBehaviour
{
    [Header("Color Pulse")]
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = Color.red;
    [SerializeField] private float halfCycleSeconds = 0.12f; // white->red or red->white
    [SerializeField] private Ease ease = Ease.InOutSine;
    [SerializeField] private bool useUnscaledTime = true;

    private TMP_Text _text;
    private Tween _tween;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _text.color = colorA;
    }

    public void StartPulse()
    {
        if (_text == null) return;

        StopPulse();
        _text.color = colorA;

        _tween = _text.DOColor(colorB, halfCycleSeconds)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(useUnscaledTime);
    }

    public void StopPulse()
    {
        _tween?.Kill();
        _tween = null;

        if (_text != null)
            _text.color = colorA;
    }
}
