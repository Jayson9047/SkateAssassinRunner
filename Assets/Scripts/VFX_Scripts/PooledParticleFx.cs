using UnityEngine;

public class PooledParticleFx : MonoBehaviour
{
    private ParticleSystem[] _ps;
    private float _cachedLifetime = -1f;

    private void Awake()
    {
        Cache();
    }

    public void Cache()
    {
        if (_ps == null || _ps.Length == 0)
            _ps = GetComponentsInChildren<ParticleSystem>(true);

        if (_cachedLifetime < 0f)
            _cachedLifetime = EstimateLifetime(_ps);
    }

    public float PlayOneShot(bool forceNonLooping = true)
    {
        Cache();

        if (_ps == null) return 0.5f;

        for (int i = 0; i < _ps.Length; i++)
        {
            var ps = _ps[i];
            if (ps == null) continue;

            var main = ps.main;

            if (forceNonLooping)
                main.loop = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        return Mathf.Max(0.1f, _cachedLifetime);
    }

    private float EstimateLifetime(ParticleSystem[] list)
    {
        float max = 0.4f;

        if (list == null) return max;

        for (int i = 0; i < list.Length; i++)
        {
            var ps = list[i];
            if (ps == null) continue;

            var main = ps.main;

            // startDelay can be constant/curve; constantMax is safe upper bound
            float delay = main.startDelay.constantMax;
            float duration = main.duration;
            float lifetime = main.startLifetime.constantMax;

            // Some FX use long durations; clamp to something reasonable if you want:
            float total = delay + duration + lifetime;

            if (total > max) max = total;
        }

        // Small padding for safety
        return max + 0.05f;
    }
}