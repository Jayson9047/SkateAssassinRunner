using UnityEngine;

public class UIBob : MonoBehaviour
{
    public float amplitude = 6f; // pixels
    public float frequency = 1.2f;
    public bool useUnscaledTime = true;

    private RectTransform _rt;
    private Vector2 _startPos;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _startPos = _rt.anchoredPosition;
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float offset = Mathf.Sin(t * frequency) * amplitude;
        _rt.anchoredPosition = _startPos + Vector2.up * offset;
    }
}
