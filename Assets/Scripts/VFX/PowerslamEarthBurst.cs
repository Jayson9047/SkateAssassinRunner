using UnityEngine;

/// <summary>One combined EarthAOE mesh, animated without the vendor Timeline or per-rock animators.</summary>
[DisallowMultipleComponent]
public sealed class PowerslamEarthBurst : MonoBehaviour
{
    [SerializeField] private Transform shatteredGround;
    private float elapsed;
    private bool playing;

    public void Play()
    {
        elapsed = 0f;
        playing = true;
        if (shatteredGround != null)
        {
            shatteredGround.gameObject.SetActive(true);
            SetHeight(0f);
        }
    }

    private void Update()
    {
        if (!playing || shatteredGround == null) return;
        elapsed += Time.deltaTime;
        // Slam punches the broken earth upward, holds briefly, then sinks it below the road.
        float height = elapsed < 0.15f ? Mathf.SmoothStep(0f, 1f, elapsed / 0.15f)
            : elapsed < 0.55f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 0.55f) / 0.6f);
        SetHeight(height);
        if (elapsed >= 1.15f) Clear();
    }

    private void SetHeight(float height)
    {
        shatteredGround.localPosition = Vector3.down * (1f - height) * 0.65f;
        shatteredGround.localScale = new Vector3(1f, Mathf.Max(0.01f, height), 1f);
    }

    public void Clear()
    {
        playing = false;
        if (shatteredGround != null) shatteredGround.gameObject.SetActive(false);
    }

    private void OnDisable() => Clear();
}
