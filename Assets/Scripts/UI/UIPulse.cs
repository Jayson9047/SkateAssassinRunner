using UnityEngine;

public class UIPulse : MonoBehaviour
{
    public float minScale = 0.95f;
    public float maxScale = 1.05f;
    public float speed = 1.5f;
    public bool useUnscaledTime = true;

    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(t * speed) + 1f) * 0.5f);
        transform.localScale = _baseScale * pulse;
    }
}
