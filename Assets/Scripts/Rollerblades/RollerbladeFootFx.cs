using UnityEngine;

/// <summary>World history and ground contact are separate; no allocation in Tick.</summary>
[DisallowMultipleComponent]
public sealed class RollerbladeFootFx : MonoBehaviour
{
    [SerializeField] private LineRenderer ribbonCore, ribbonHalo;
    [SerializeField] private ParticleSystem frictionSparks, wheelGlow;
    [SerializeField] private Color sparkColor = new Color(0.1f, 1.4f, 2f, 1f);
    [SerializeField, Min(1f)] private float sparksPerSecond = 28f;
    [SerializeField, Min(0.01f)] private float pointSpacing = 0.07f;
    [SerializeField, Min(0.01f)] private float ribbonLifetime = 0.22f;
    private Vector3[] positions;
    private float[] ages;
    private int positionCount;
    private float sparkRemainder;
    private bool previousContact;

    private void Awake()
    {
        positions = new Vector3[128];
        ages = new float[128];
    }

    public void Tick(Vector3 wheel, Vector3 contactPoint, bool contact, float scrollSpeed, float delta)
    {
        transform.position = wheel;
        bool moving = scrollSpeed > 0.1f;
        UpdateRibbon(wheel, scrollSpeed, moving, delta);
        bool scraping = contact && moving;
        if (scraping)
        {
            sparkRemainder += delta * sparksPerSecond;
            int emitCount = Mathf.Min(6, Mathf.FloorToInt(sparkRemainder) + (previousContact ? 0 : 3));
            sparkRemainder -= Mathf.Floor(sparkRemainder);
            if (!frictionSparks.isPlaying) frictionSparks.Play(false);
            var emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < emitCount; i++)
            {
                emit.position = contactPoint + new Vector3(Random.Range(-0.07f, 0.07f), 0f, Random.Range(-0.03f, 0.03f));
                // These are wheel-contact sparks, not points in the road's history.
                // Full scroll velocity sends them down the entire ribbon at high speed.
                emit.velocity = new Vector3(-Random.Range(2f, 5f), Random.Range(0.4f, 1.8f), Random.Range(-0.65f, 0.65f));
                emit.startColor = Random.value < 0.4f ? new Color(2f, 2f, 2f, 1f) : sparkColor;
                emit.startSize = Random.Range(0.012f, 0.025f);
                emit.startLifetime = Random.Range(0.06f, 0.10f);
                frictionSparks.Emit(emit, 1);
            }
        }
        else sparkRemainder = 0f;
        // The zero-speed local glow belongs to the live contact, not old sparks.
        if (scraping)
        {
            wheelGlow.transform.position = contactPoint;
            if (!wheelGlow.isPlaying) wheelGlow.Play(false);
        }
        else if (wheelGlow.isPlaying) wheelGlow.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        previousContact = scraping;
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
        if (frictionSparks != null) frictionSparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (wheelGlow != null) wheelGlow.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        sparkRemainder = 0f; previousContact = false;
    }
    private void OnDisable() => Clear();
}
