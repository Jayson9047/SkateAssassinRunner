using UnityEngine;

/// <summary>Combined EarthAOE geometry, animated without Timeline or per-rock animators.</summary>
[DisallowMultipleComponent]
public sealed class PowerslamEarthBurst : MonoBehaviour
{
    [SerializeField] private Transform shatteredGround;
    [SerializeField] private Transform spikeRing;
    [SerializeField, Min(0.01f)] private float groundRiseSeconds = 0.12f;
    [SerializeField, Min(0.01f)] private float spikeRiseSeconds = 0.18f;
    [SerializeField, Min(0.01f)] private float holdUntilSeconds = 0.65f;
    [SerializeField, Min(0.01f)] private float sinkSeconds = 0.45f;
    private float elapsed;
    private bool playing;

    public void Play()
    {
        elapsed = 0f;
        playing = true;
        if (shatteredGround != null) shatteredGround.gameObject.SetActive(true);
        if (spikeRing != null) spikeRing.gameObject.SetActive(true);
        SetPose(0f);
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        SetPose(elapsed);
        if (elapsed >= holdUntilSeconds + sinkSeconds) Clear();
    }

    public void SetPose(float seconds)
    {
        elapsed = Mathf.Max(0f, seconds);
        float sink = seconds <= holdUntilSeconds ? 1f
            : 1f - Mathf.SmoothStep(0f, 1f, (seconds - holdUntilSeconds) / sinkSeconds);
        SetHeight(shatteredGround, Mathf.SmoothStep(0f, 1f, seconds / groundRiseSeconds) * sink, 0.8f);
        SetHeight(spikeRing, Mathf.SmoothStep(0f, 1f, seconds / spikeRiseSeconds) * sink, 0.5f);
    }

    private static void SetHeight(Transform target, float height, float depth)
    {
        if (target == null) return;
        target.localPosition = Vector3.down * (1f - height) * depth;
        target.localScale = new Vector3(1f, Mathf.Max(0.01f, height), 1f);
    }

    public void Clear()
    {
        playing = false;
        if (shatteredGround != null) shatteredGround.gameObject.SetActive(false);
        if (spikeRing != null) spikeRing.gameObject.SetActive(false);
    }

    private void OnDisable() => Clear();
}
