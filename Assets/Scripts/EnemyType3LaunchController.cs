using UnityEngine;
using DG.Tweening;

public class EnemyType3LaunchController : MonoBehaviour
{
    [Header("Flight Target")]
    [SerializeField] private Transform meetPoint;

    [Header("Arc Tuning")]
    [SerializeField] private float flightDuration = 0.65f;
    [SerializeField] private float durationMultiplier = 1.6f; // >1 = slower arc
    [SerializeField] private float apexHeight = 2.5f;
    [SerializeField] private float sidewaysDrift = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string airLaunchTrigger = "AirLaunch";

    [Header("Near Target")]
    [SerializeField] private float nearTargetDistance = 0.35f;

    [SerializeField] private string upperBodyLayerName = "UpperBody";

    [Header("Phase 2 Vulnerability Gating")]
    [SerializeField] private string disarmedTag = "Enemy";
    [SerializeField] private int disarmedLayer;   // set to Default (or a custom NotDamageable layer)

    [SerializeField] private string armedTag = "Untagged";
    [SerializeField] private int armedLayer;   // set to Default (or a custom NotDamageable layer)

    [SerializeField] private GameObject enemyRoot; // drag the Enemy3 root here (the child under car)

    private int _upperBodyLayerIndex = -1;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Quaternion _originalLocalRot;

    private Collider[] _colliders;
    private Rigidbody _rb;
    private Tween _tween;

    public System.Action OnNearTarget;
    public System.Action OnArrived;


    private void Awake()
    {
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;

        _colliders = GetComponentsInChildren<Collider>(true);
        _rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            _upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
        }
        SetEnemyDisarmed(false);
    }

    private void OnEnable()
    {
        KillTween();
        ResetToCarState();
    }

    public void SetMeetPoint(Transform t) => meetPoint = t;

    public void Launch()
    {
        if (meetPoint == null)
        {
            Debug.LogError("[EnemyType3LaunchController] MeetPoint not assigned.");
            return;
        }

        KillTween();

        // Trigger AIR LAUNCH animation (Animator parameter, not state name)
        if (animator != null && !string.IsNullOrEmpty(airLaunchTrigger))
        {
            animator.ResetTrigger(airLaunchTrigger);
            animator.SetTrigger(airLaunchTrigger);

            // Disable UpperBody layer during air launch
            if (_upperBodyLayerIndex >= 0)
            {
                animator.SetLayerWeight(_upperBodyLayerIndex, 0f);
            }
        }

        // Detach from car
        transform.SetParent(null, true);

        // after you detach enemy from car
        var frameEvents = FindFirstObjectByType<Phase2PowerSlamFrameEvents>();
        if (frameEvents != null)
            frameEvents.SetEnemyInstance(gameObject); // or enemyRoot.gameObject

        // Disable physics influence
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        // Disable colliders during flight
        SetCollidersEnabled(false);

        Vector3 start = transform.position;
        Vector3 end = meetPoint.position;

        // Arc construction
        Vector3 mid = (start + end) * 0.5f;
        mid.y += apexHeight;

        Vector3 driftDir = Vector3.Cross((end - start).normalized, Vector3.up).normalized;
        mid += driftDir * sidewaysDrift;

        Vector3[] path = new[] { start, mid, end };

        bool nearFired = false;
        float duration = flightDuration * Mathf.Max(0.01f, durationMultiplier);

        _tween = transform
            .DOPath(path, duration, PathType.CatmullRom, PathMode.Full3D)
            .SetEase(ease)
            .OnUpdate(() =>
            {
                if (!nearFired && Vector3.Distance(transform.position, end) <= nearTargetDistance)
                {
                    nearFired = true;
                    OnNearTarget?.Invoke(); // slow-mo later
                }
            })
            .OnComplete(() =>
            {
                OnArrived?.Invoke();
            });
    }

    private void ResetToCarState()
    {
        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localRotation = _originalLocalRot;

        if (_rb != null)
            _rb.isKinematic = true;

        SetCollidersEnabled(true);
    }
    public void SetEnemyDisarmed(bool disarmed)
    {
        if (enemyRoot == null) return;

        string tag = disarmed ? disarmedTag: armedTag;
        int layer = disarmed ?  disarmedLayer : armedLayer;

        SetTagAndLayerRecursively(enemyRoot.transform, tag, layer);
    }

    private void SetTagAndLayerRecursively(Transform root, string tag, int layer)
    {
        root.gameObject.tag = tag;
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetTagAndLayerRecursively(root.GetChild(i), tag, layer);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null) return;
        for (int i = 0; i < _colliders.Length; i++)
            _colliders[i].enabled = enabled;
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();
        _tween = null;
    }
}
