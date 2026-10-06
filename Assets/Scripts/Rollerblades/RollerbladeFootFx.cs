using UnityEngine;

/// <summary>World history and ground contact are separate; no allocation in Tick.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeFootFx : MonoBehaviour
{
    [SerializeField] private LineRenderer ribbonCore, ribbonHalo;
    [SerializeField] private ParticleSystem directionalSparks, blueThruster;
    [SerializeField, Tooltip("Option 4 palette. Option 1 retains the original fire colors on the same emitter.")]
    private Gradient matchingSparkColors = new Gradient();
    [SerializeField, Min(0.01f)] private float pointSpacing = 0.07f;
    [SerializeField, Min(0.01f)] private float ribbonLifetime = 0.22f;
    private Vector3[] positions;
    private float[] ages;
    private int positionCount;
    private ParticleSystem activeParticles;
    private RollerbladeFxTrial trial;
    private bool configured, ribbonEnabled, emitting;
    private ParticleSystem.MinMaxGradient fireStartColor, fireLifetimeColor;
    private bool fireColorBySpeed;

    private void Awake()
    {
        positions = new Vector3[128];
        ages = new float[128];
        if (directionalSparks != null)
        {
            fireStartColor = directionalSparks.main.startColor;
            fireLifetimeColor = directionalSparks.colorOverLifetime.color;
            // Unity's returned Gradient wraps the module's native gradient.
            // Snapshot its keys before replacing the module, or switching back
            // would restore the matching palette instead of the original fire.
            var snapshot = new Gradient();
            var original = fireLifetimeColor.gradient;
            snapshot.SetKeys(original.colorKeys, original.alphaKeys);
            snapshot.mode = original.mode;
            fireLifetimeColor = new ParticleSystem.MinMaxGradient(snapshot);
            fireColorBySpeed = directionalSparks.colorBySpeed.enabled;
        }
    }

    public void SetTrial(RollerbladeFxTrial value)
    {
        if (configured && trial == value) return;
        Clear();
        trial = value;
        configured = true;
        bool matching = value == RollerbladeFxTrial.MatchingSparksAndTrail;
        bool sparks = value == RollerbladeFxTrial.SparksAndTrail || matching;
        if (directionalSparks != null && sparks)
        {
            // Change vertex colors on the same bounded emitter; no extra system
            // or material instance is needed to compare fire with matching sparks.
            var main = directionalSparks.main;
            main.startColor = matching ? new ParticleSystem.MinMaxGradient(Color.white) : fireStartColor;
            var colors = directionalSparks.colorOverLifetime;
            colors.color = matching ? new ParticleSystem.MinMaxGradient(matchingSparkColors) : fireLifetimeColor;
            // The CFXR source also has a separate orange/red speed gradient.
            var speedColors = directionalSparks.colorBySpeed;
            speedColors.enabled = !matching && fireColorBySpeed;
        }
        if (directionalSparks != null) directionalSparks.gameObject.SetActive(sparks);
        if (blueThruster != null) blueThruster.gameObject.SetActive(!sparks);
        activeParticles = sparks ? directionalSparks : blueThruster;
        ribbonEnabled = value != RollerbladeFxTrial.ThrusterOnly;
        if (ribbonCore != null) ribbonCore.enabled = ribbonEnabled;
        if (ribbonHalo != null) ribbonHalo.enabled = ribbonEnabled;
    }

    public void Tick(Vector3 ribbonHead, Vector3 rearWheel, bool contact, float scrollSpeed, float delta)
    {
        transform.position = rearWheel;
        bool moving = scrollSpeed > 0.1f;
        if (ribbonEnabled) UpdateRibbon(ribbonHead, scrollSpeed, moving, delta);
        bool scraping = contact && moving && activeParticles != null;
        if (scraping == emitting) return;
        emitting = scraping;
        if (scraping) activeParticles.Play(true);
        else if (activeParticles != null) activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void UpdateRibbon(Vector3 point, float scrollSpeed, bool moving, float delta)
    {
        if (positions == null) return;
        // The runner stays near a fixed X while the road scrolls. Move historical
        // points with the road, leaving the current skate position as the head.
        for (int i = 0; i < positionCount; i++)
        {
            positions[i] += Vector3.left * (scrollSpeed * delta);
            ages[i] += delta;
        }
        while (positionCount > 0 && ages[positionCount - 1] > ribbonLifetime) positionCount--;
        if (moving && (positionCount == 0 || (positions[0] - point).sqrMagnitude >= pointSpacing * pointSpacing))
        {
            positionCount = Mathf.Min(positionCount, positions.Length - 1);
            for (int i = positionCount; i > 0; i--)
            { positions[i] = positions[i - 1]; ages[i] = ages[i - 1]; }
            positions[0] = point; ages[0] = 0f; positionCount++;
        }
        DrawRibbon(ribbonCore); DrawRibbon(ribbonHalo);
    }

    private void DrawRibbon(LineRenderer ribbon)
    {
        if (ribbon == null) return;
        ribbon.positionCount = positionCount;
        // Reuse a bounded managed buffer. No persistent native allocation needs
        // cleanup during an Editor reload, and no temporary arrays are built.
        for (int i = 0; i < positionCount; i++) ribbon.SetPosition(i, positions[i]);
    }

    public void Clear()
    {
        positionCount = 0;
        if (ribbonCore != null) ribbonCore.positionCount = 0;
        if (ribbonHalo != null) ribbonHalo.positionCount = 0;
        if (directionalSparks != null) directionalSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (blueThruster != null) blueThruster.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        emitting = false;
    }
    private void OnDisable() => Clear();
}
