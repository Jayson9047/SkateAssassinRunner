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
    [Tooltip("FEEL camera shake strength on each ground impact. Zero disables it.")]
    [Range(0f, 1f)] public float impactCameraShakeIntensity = .05f;
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
    CycleStage lastAudioStage;
    float cycleTime, previousX;
    float approachPlayerX, releasedAt;
    float crossingWindowEnd, cycleDelay;
    Renderer headRenderer;
    MeshRenderer[] machineRenderers;
    Camera gameplayCamera;
    readonly Plane[] viewPlanes = new Plane[6];
    MaterialPropertyBlock signalProperties;

    void OnEnable()
    {
        started = sampled = false;
        cycleTime = cycleDelay = crossingWindowEnd = 0f;
        ImpactCount = 0;
        CrossingRegistered = false;
        releasedAt = -1;
        Stage = CycleStage.Waiting;
        lastAudioStage = CycleStage.Waiting;
        gameplayCamera = null;
        headRenderer = head ? head.GetComponent<Renderer>() : null;
        if (machineRenderers == null) machineRenderers = GetComponentsInChildren<MeshRenderer>(true);
        ApplyHeight(1f);
        if (impactDust) impactDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetLight(false);
    }

    void Update()
    {
        if (!head || !ram || !entry || Stage == CycleStage.Finished) return;
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
                        cycleTime = Mathf.Max(0f, (triggerDistance-gap) / referenceSpeed);
                        approachPlayerX = player.transform.position.x;
                    }
                    break;
                }
        }
        if (!started) return;
        if (HasExitedScreen(x))
        {
            Stage = CycleStage.Finished;
            ApplyHeight(1f);
            SetLight(false);
            return;
        }

        float t = CycleTime - cycleDelay;
        // Preserve the existing interlock so the dash return cannot hit a closing head.
        if (CrossingRegistered && t >= crossingWindowEnd && releasedAt < 0f)
        {
            if (x >= approachPlayerX-returnClearance)
            {
                Stage = CycleStage.TransitHold;
                PlayStageAudioIfNeeded();
                ApplyHeight(1f);
                SetLight(false);
                return;
            }
            releasedAt = CycleTime;
            cycleDelay = CycleTime-crossingWindowEnd;
            t = crossingWindowEnd;
        }

        if (t < warningDuration)
        {
            Stage = CycleStage.Warning;
            ApplyHeight(1f);
            PlayStageAudioIfNeeded();
            SetLight(true);
            return;
        }

        // Repeat drop, hold, rise and crossing window until the whole machine leaves view.
        float period = dropDuration + firstGroundHold + riseDuration + crossingWindow;
        int cycleIndex = Mathf.FloorToInt((t-warningDuration)/period);
        float phase = t-warningDuration-cycleIndex*period;
        float liftStart = dropDuration + firstGroundHold;
        float openStart = liftStart + riseDuration;
        CycleStage impactStage = cycleIndex == 0 ? CycleStage.FirstImpact : CycleStage.SecondImpact;
        if (phase < dropDuration)
        {
            Stage = impactStage;
            ApplyHeight(1f-Mathf.Pow(phase/dropDuration,2f));
        }
        else if (phase < liftStart) { Stage = impactStage; ApplyHeight(0f); }
        else if (phase < openStart)
        {
            Stage = CycleStage.Rising;
            ApplyHeight(Mathf.SmoothStep(0f,1f,(phase-liftStart)/riseDuration));
        }
        else { Stage = CycleStage.CrossingWindow; ApplyHeight(1f); }

        // Arm the return interlock for a successful crossing during any cycle.
        if (!CrossingRegistered && Stage == CycleStage.CrossingWindow && headRenderer)
        {
            var manager = LevelManager.Instance;
            if (manager != null && manager.CurrentPlayableCharacters != null)
                foreach (var character in manager.CurrentPlayableCharacters)
                {
                    if (!character || !character.isActiveAndEnabled) continue;
                    var jumper = character as Jumper;
                    var body = character.GetComponent<BoxCollider>();
                    var headBounds = headRenderer.bounds;
                    if (jumper != null && jumper.IsGrounded && body != null
                        && body.bounds.min.x > headBounds.max.x
                        && body.bounds.max.y < headBounds.min.y
                        && Mathf.Abs(body.bounds.center.z-headBounds.center.z) < headBounds.extents.z+body.bounds.extents.z)
                    {
                        CrossingRegistered = true;
                        crossingWindowEnd = warningDuration+(cycleIndex+1)*period;
                    }
                    break;
                }
        }
        PlayStageAudioIfNeeded();
        int completedImpacts = cycleIndex + (phase >= dropDuration ? 1 : 0);
        if (completedImpacts > ImpactCount)
        {
            // Do not burst old sounds/particles/shakes after a frame hitch.
            ImpactCount = completedImpacts-1;
            Impact();
        }
        SetLight(Stage == CycleStage.SecondImpact || (Stage == CycleStage.CrossingWindow && phase > period-.2f));
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
        SkateRunnerAudioManager.PlayCompactorImpact();
        if (impactDust) impactDust.Play();
        SkateRunnerGameFeel.TriggerCameraShakeStatic(head.position, impactCameraShakeIntensity);
    }


void PlayStageAudioIfNeeded()
    {
        if (Stage == lastAudioStage) return;
        lastAudioStage = Stage;
        if (Stage == CycleStage.FirstImpact || Stage == CycleStage.Rising || Stage == CycleStage.SecondImpact)
            SkateRunnerAudioManager.PlayCompactorMove();
    }


    bool HasExitedScreen(float x)
    {
        if (x >= approachPlayerX-returnClearance) return false;
        if (!gameplayCamera) gameplayCamera = Camera.main;
        if (!gameplayCamera || machineRenderers == null || machineRenderers.Length == 0) return false;

        GeometryUtility.CalculateFrustumPlanes(gameplayCamera, viewPlanes);
        foreach (var renderer in machineRenderers)
            if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy
                && GeometryUtility.TestPlanesAABB(viewPlanes, renderer.bounds))
                return false;
        return true;
    }
}
