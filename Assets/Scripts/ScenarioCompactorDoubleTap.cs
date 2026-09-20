using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

/// <summary>A local, approach-triggered mechanical cycle. No player ability changes.</summary>
public sealed class ScenarioCompactorDoubleTap : MonoBehaviour
{
    public enum CycleStage { Waiting, Warning, FirstImpact, Rising, CrossingWindow, TransitHold, SecondImpact, Finished }
    public Transform head, ram, entry;
    public Vector3 headDownPosition, headUpPosition, ramDownPosition, ramUpPosition, ramDownScale, ramUpScale;
    public Light warningLight;
    public Renderer signalLens;
    [Min(0f)] public float warningIntensity = 2f;
    public ParticleSystem impactDust;
    [Tooltip("World units travelled per second at the reference pace. All cycle durations below use this pace; twice the actual travel speed runs the hydraulics twice as fast.")]
    [Min(.1f)] public float referenceTravelSpeed = 10f;
    [Tooltip("Approach distance = this duration multiplied by Reference Travel Speed. Durations are reference seconds, not fixed real seconds.")]
    [Min(.1f)] public float approachLeadSeconds = 1.7f;
    [Min(.05f)] public float warningDuration = .25f;
    [Min(.05f)] public float dropDuration = .17f;
    [Min(.01f)] public float firstGroundHold = .4f;
    [Min(.05f)] public float riseDuration = .25f;
    [Min(.05f)] public float crossingWindow = .42f;
    [Tooltip("Keep the physical head raised after a successful crossing until it has passed the player's approach position by this distance. Covers the existing dash return.")]
    [Min(1f)] public float returnClearance = 3.1f;
    public CycleStage Stage { get; private set; }
    public float CycleTime => started ? cycleTime : -1f;
    public int ImpactCount { get; private set; }
    public bool CrossingRegistered { get; private set; }
    bool started, sampled;
    float cycleTime, previousX;
    float approachPlayerX, releasedAt;
    MaterialPropertyBlock signalProperties;

    void OnEnable()
    {
        started = sampled = false;
        cycleTime = 0f;
        ImpactCount = 0;
        CrossingRegistered = false;
        releasedAt = -1;
        Stage = CycleStage.Waiting;
        ApplyHeight(1f);
        if (impactDust) impactDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetLight(false);
    }

    void Update()
    {
        if (!head || !ram || !entry) return;
        float x = entry.position.x;
        float travel = sampled ? Mathf.Max(0, previousX - x) : 0;
        previousX = x;
        sampled = true;
        float referenceSpeed = Mathf.Max(.1f, referenceTravelSpeed);
        // Follow actual chunk travel, including speed changes and pauses during the encounter.
        if (started) cycleTime += travel / referenceSpeed;
        if (!started && travel > 0f)
        {
            var manager = LevelManager.Instance;
            if (manager != null && manager.CurrentPlayableCharacters != null)
                foreach (var player in manager.CurrentPlayableCharacters)
                {
                    if (!player || !player.isActiveAndEnabled) continue;
                    float gap = x - player.transform.position.x;
                    float triggerDistance = referenceSpeed * approachLeadSeconds;
                    if (gap <= triggerDistance && gap + travel > 0f)
                    {
                        started = true;
                        // Include the part of this frame already travelled beyond the sensor.
                        cycleTime = Mathf.Max(0f, (triggerDistance-gap) / referenceSpeed);
                        approachPlayerX = player.transform.position.x;
                    }
                    break;
                }
        }
        if (!started) return;
        float t = CycleTime;
        float firstHit = warningDuration + dropDuration;
        float liftStart = firstHit + firstGroundHold;
        float openStart = liftStart + riseDuration;
        float secondDrop = openStart + crossingWindow;
        // A completed physical crossing, rather than the input button, arms the return interlock.
        if (!CrossingRegistered && t >= openStart && t < secondDrop)
        {
            var manager = LevelManager.Instance;
            if (manager != null && manager.CurrentPlayableCharacters != null)
                foreach (var character in manager.CurrentPlayableCharacters)
                {
                    if (!character || !character.isActiveAndEnabled) continue;
                    var jumper = character as Jumper;
                    var body = character.GetComponent<BoxCollider>();
                    var headBounds = head.GetComponent<Renderer>().bounds;
                    if (jumper != null && jumper.IsGrounded && body != null
                        && body.bounds.min.x > headBounds.max.x
                        && body.bounds.max.y < headBounds.min.y
                        && Mathf.Abs(body.bounds.center.z-headBounds.center.z) < headBounds.extents.z+body.bounds.extents.z)
                        CrossingRegistered = true;
                    break;
                }
        }
        if (CrossingRegistered && t >= secondDrop)
        {
            if (releasedAt < 0 && x < approachPlayerX-returnClearance) releasedAt = t;
            if (releasedAt < 0)
            {
                Stage = CycleStage.TransitHold;
                ApplyHeight(1);
                SetLight(false);
                return;
            }
            secondDrop = releasedAt;
        }
        float secondHit = secondDrop + dropDuration;
        if (t < warningDuration) { Stage = CycleStage.Warning; ApplyHeight(1); }
        else if (t < firstHit) { Stage = CycleStage.FirstImpact; ApplyHeight(1-Mathf.Pow((t-warningDuration)/dropDuration,2)); }
        else if (t < liftStart) { Stage = CycleStage.FirstImpact; ApplyHeight(0); }
        else if (t < openStart) { Stage = CycleStage.Rising; ApplyHeight(Mathf.SmoothStep(0,1,(t-liftStart)/riseDuration)); }
        else if (t < secondDrop) { Stage = CycleStage.CrossingWindow; ApplyHeight(1); }
        else if (t < secondHit) { Stage = CycleStage.SecondImpact; ApplyHeight(1-Mathf.Pow((t-secondDrop)/dropDuration,2)); }
        else { Stage = CycleStage.Finished; ApplyHeight(0); }
        if (t >= firstHit && ImpactCount == 0) Impact();
        if (t >= secondHit && ImpactCount == 1) Impact();
        SetLight(Stage == CycleStage.Warning || Stage == CycleStage.SecondImpact || (Stage == CycleStage.CrossingWindow && t > secondDrop-.2f));
    }

    void ApplyHeight(float raised)
    {
        if (head) head.localPosition = Vector3.Lerp(headDownPosition,headUpPosition,raised);
        if (ram)
        {
            ram.localPosition = Vector3.Lerp(ramDownPosition,ramUpPosition,raised);
            ram.localScale = Vector3.Lerp(ramDownScale,ramUpScale,raised);
        }
    }
    void SetLight(bool flash)
    {
        bool green = Stage == CycleStage.CrossingWindow || Stage == CycleStage.TransitHold;
        Color color = green ? new Color(.05f,1f,.15f) : flash ? new Color(1,.12f,.01f) : new Color(1,.6f,.05f);
        float brightness = flash && !green ? (Mathf.Sin(cycleTime*30)>0 ? 1:.25f) : 1;
        if (warningLight) { warningLight.color=color; warningLight.intensity=brightness*warningIntensity; }
        if (signalLens)
        {
            if(signalProperties==null) signalProperties=new MaterialPropertyBlock();
            signalProperties.SetColor("_BaseColor",color*brightness);
            signalLens.SetPropertyBlock(signalProperties);
        }
    }
    void Impact()
    {
        ImpactCount++;
        if (impactDust) impactDust.Play();
    }
}
