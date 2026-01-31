using UnityEngine;

public class UIConstantSpinner : MonoBehaviour
{
    [Tooltip("Degrees per second. Positive = clockwise, negative = counter-clockwise (depending on canvas orientation).")]
    public float degreesPerSecond = 25f;

    [Tooltip("Use unscaled time so it keeps spinning even if Time.timeScale is 0 (common on menus).")]
    public bool useUnscaledTime = true;

    private RectTransform _rt;

    private void Awake()
    {
        _rt = transform as RectTransform;
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_rt != null)
        {
            // For UI, rotate around Z axis.
            _rt.Rotate(0f, 0f, -degreesPerSecond * dt);
        }
        else
        {
            transform.Rotate(0f, 0f, -degreesPerSecond * dt);
        }
    }
}
