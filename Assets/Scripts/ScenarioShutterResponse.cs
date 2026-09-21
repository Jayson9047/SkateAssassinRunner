using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

/// <summary>Local airborne sensor and retracting shutter for the Jump Scanner encounter.</summary>
public sealed class ScenarioShutterResponse : MonoBehaviour
{
    public Transform shutter;
    public BoxCollider shutterHazard;
    public Transform entry;
    public Light[] warningLights;
    public Vector3 openPosition, closedPosition, openScale, closedScale;
    public float top = 6.2f;
    public float openBottom = 2.9f;
    public float closedBottom = -1.5f;
    public float sensorDistance = 15f;
    public float warningSeconds = 0.22f;
    public float closingSeconds = 0.25f;
    public bool Reacted { get; private set; }
    public float Closure { get; private set; }
    private float reactionTime;
    private bool movementSoundPlayed;

    private void OnEnable()
    {
        Reacted = false;
        Closure = 0f;
        reactionTime = 0f;
        movementSoundPlayed = false;
        Apply(0f);
        SetLights(false);
    }

    private void Update()
    {
        var manager = LevelManager.Instance;
        if (manager == null || manager.CurrentPlayableCharacters == null || entry == null) return;
        foreach (var player in manager.CurrentPlayableCharacters)
        {
            if (player == null || !player.isActiveAndEnabled) continue;
            float distance = entry.position.x - player.transform.position.x;
            bool withinSensor = distance < sensorDistance && distance > -3f;
            var jumper = player as Jumper;
            if (!Reacted && withinSensor && jumper != null && !jumper.IsGrounded)
            {
                Reacted = true;
                reactionTime = Time.time;
            }
            SetLights(withinSensor);
            break;
        }
        if (Reacted)
        {
            Closure = Mathf.Clamp01((Time.time - reactionTime - warningSeconds) / Mathf.Max(.01f, closingSeconds));
            if (!movementSoundPlayed && Closure > 0f)
            {
                movementSoundPlayed = true;
                SkateRunnerAudioManager.PlayCheckpointGateMove();
            }
            Apply(Mathf.SmoothStep(0f, 1f, Closure));
        }
    }

    private void SetLights(bool nearby)
    {
        if (warningLights == null) return;
        foreach (var light in warningLights)
        {
            if (!light) continue;
            light.color = Reacted ? Color.red : new Color(1f, .55f, .05f);
            light.intensity = nearby ? (Mathf.Sin(Time.time * (Reacted ? 25f : 7f)) > 0 ? 5f : .5f) : .5f;
        }
    }

    private void Apply(float amount)
    {
        if (shutter)
        {
            shutter.localPosition = Vector3.Lerp(openPosition, closedPosition, amount);
            shutter.localScale = Vector3.Lerp(openScale, closedScale, amount);
        }
        if (shutterHazard)
        {
            float bottom = Mathf.Lerp(openBottom, closedBottom, amount);
            var center = shutterHazard.center;
            center.y = (top + bottom) * .5f;
            shutterHazard.center = center;
            var size = shutterHazard.size;
            size.y = top - bottom;
            shutterHazard.size = size;
        }
    }
}
