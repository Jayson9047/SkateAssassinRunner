using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

public enum RollerbladeFxTrial
{
    [InspectorName("1 - Fire Directional Sparks + Trail")] SparksAndTrail = 0,
    [InspectorName("2 - CFXR4 Blue Thruster + Trail")] ThrusterAndTrail = 1,
    [InspectorName("3 - CFXR4 Blue Thruster Only")] ThrusterOnly = 2,
    [InspectorName("4 - Matching Directional Sparks + Trail")] MatchingSparksAndTrail = 3
}

/// <summary>One optional FX pair follows the actually equipped skates.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(RollerbladeEquipper))]
public sealed class RollerbladeMovementFx : MonoBehaviour
{
    [SerializeField, InspectorName("Rollerblade FX")]
    private bool rollerbladeFx = true;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("neonVelocityTrial")]
    [Tooltip("Comparison for every upgraded rollerblade. Option 1 keeps fire sparks; option 4 matches the equipped pair. Switch live in Play mode; only the chosen effect runs. Play-mode changes are temporary.")]
    private RollerbladeFxTrial rollerbladeEffect = RollerbladeFxTrial.MatchingSparksAndTrail;
    [SerializeField] private LayerMask groundLayers = 1 << 8;
    [SerializeField, Min(0.01f)] private float contactTolerance = 0.35f;
    [SerializeField, Min(0f)] private float worldScrollMultiplier = 0.5f;
    [SerializeField, Min(0f), Tooltip("Visual lift above the physical road plane; keeps glow clear of raised road meshes.")]
    private float groundFxLift = 0.18f;
    private RollerbladeEquipper equipper;
    private RollerbladeFootFx leftFx, rightFx;
    private Renderer leftBlade, rightBlade;
    private Jumper jumper;
    private Transform viewCamera;
    private GameObject activePrefab;
    private bool previousEnabled;

    public bool RollerbladeFX
    {
        get => rollerbladeFx;
        set { rollerbladeFx = value; RefreshPair(); }
    }
    public RollerbladeFxTrial Trial
    {
        get => rollerbladeEffect;
        set { rollerbladeEffect = value; ApplyTrial(); }
    }

    private void ApplyTrial()
    {
        if (leftFx != null) leftFx.SetTrial(rollerbladeEffect);
        if (rightFx != null) rightFx.SetTrial(rollerbladeEffect);
    }

    private void Awake()
    {
        equipper = GetComponent<RollerbladeEquipper>();
        jumper = GetComponent<Jumper>();
        if (Camera.main != null) viewCamera = Camera.main.transform;
    }
    private void OnEnable()
    {
        if (equipper == null) equipper = GetComponent<RollerbladeEquipper>();
        equipper.PairChanged += RefreshPair;
    }
    private void Start() => RefreshPair();

    private void RefreshPair()
    {
        previousEnabled = rollerbladeFx;
        var definition = equipper != null ? equipper.CurrentDefinition : null;
        GameObject prefab = rollerbladeFx && definition != null && definition.rollerbladeId != RollerbladeId.Default
            ? definition.movementFxPrefab : null;
        if (prefab == null)
        {
            SetVisible(false);
            return; // No instantiation, raycasts, particles or trail updates when disabled/default.
        }
        if (prefab.GetComponent<RollerbladeFootFx>() == null)
        {
            SetVisible(false);
            Debug.LogWarning("Rollerblade movement FX prefab needs RollerbladeFootFx.", this);
            return;
        }
        if (prefab != activePrefab || leftFx == null || rightFx == null)
        {
            DisposePair();
            leftFx = Instantiate(prefab).GetComponent<RollerbladeFootFx>();
            rightFx = Instantiate(prefab).GetComponent<RollerbladeFootFx>();
            activePrefab = prefab;
            if (leftFx == null || rightFx == null) { DisposePair(); return; }
        }
        leftBlade = equipper.CurrentLeftRollerblade != null
            ? equipper.CurrentLeftRollerblade.GetComponentInChildren<Renderer>() : null;
        rightBlade = equipper.CurrentRightRollerblade != null
            ? equipper.CurrentRightRollerblade.GetComponentInChildren<Renderer>() : null;
        leftFx.Clear(); rightFx.Clear();
        ApplyTrial();
        SetVisible(true);
    }

    private void LateUpdate()
    {
        if (previousEnabled != rollerbladeFx) RefreshPair(); // Inspector live toggle.
        if (!rollerbladeFx || leftFx == null || !leftFx.gameObject.activeSelf) return;
        ApplyTrial(); // Also responds to the Inspector while paused, without rebuilding rigs.
        if (GameManager.Instance != null && GameManager.Instance.Status == GameManager.GameStatus.Paused) return;
        if (Time.deltaTime <= 0f) return; // Keep paused history/particles frozen.
        if (GameManager.Instance == null || GameManager.Instance.Status != GameManager.GameStatus.GameInProgress)
        {
            leftFx.Clear(); rightFx.Clear();
            return;
        }
        float speed = LevelManager.Instance != null ? Mathf.Max(0f, LevelManager.Instance.Speed) * worldScrollMultiplier : 0f;
        UpdateFoot(leftFx, leftBlade, speed);
        UpdateFoot(rightFx, rightBlade, speed);
    }

    private void UpdateFoot(RollerbladeFootFx fx, Renderer blade, float speed)
    {
        if (fx == null || blade == null) return;
        // Mesh bounds track the wheel footprint despite animated foot rotation.
        Bounds bounds = blade.bounds;
        // Use the visible wheel rim, rather than the solid boot's centre: a
        // billboard glow inside the boot is hidden by the opaque skate mesh.
        Vector3 wheel = new Vector3(bounds.center.x, bounds.min.y + 0.025f, bounds.min.z - 0.025f);
        RaycastHit hit = new RaycastHit();
        // A visual height query, not wheel collision/contact detection. Imported
        // skates have no colliders and their animated mesh may float above the road.
        bool contact = Physics.Raycast(wheel + Vector3.up * 2f, Vector3.down,
            out hit, 5f, groundLayers, QueryTriggerInteraction.Ignore);
        Vector3 point = contact ? hit.point + Vector3.up * groundFxLift : wheel;
        // Imported wheel meshes can dip below the motor's contact plane. Keep
        // the luminous ribbon visible above the road rather than inside it.
        Vector3 ribbonHead = contact
            ? new Vector3(wheel.x, Mathf.Max(wheel.y, point.y), wheel.z) : wheel;
        // One rear-wheel rim on the one-piece mesh. Choose the rear endpoint
        // in runner space (+X is forward), even as the skating animation turns it.
        Bounds local = blade.localBounds;
        bool rowAlongX = local.size.x >= local.size.z;
        Vector3 sideAxis = rowAlongX ? Vector3.forward : Vector3.right;
        Vector3 cameraDirection = viewCamera != null ? viewCamera.position - bounds.center : Vector3.back;
        float side = Vector3.Dot(blade.transform.TransformDirection(sideAxis), cameraDirection) >= 0f ? 1f : -1f;
        Vector3 rowAxis = rowAlongX ? Vector3.right : Vector3.forward;
        float t = blade.transform.TransformDirection(rowAxis).x > 0f ? 0.14f : 0.86f;
        Vector3 rim = local.center;
        if (rowAlongX) { rim.x = Mathf.Lerp(local.min.x, local.max.x, t); rim.z += side * local.extents.z * 0.8f; }
        else { rim.z = Mathf.Lerp(local.min.z, local.max.z, t); rim.x += side * local.extents.x * 0.8f; }
        rim.y = local.min.y + 0.025f;
        Vector3 rearWheel = blade.transform.TransformPoint(rim);
        if (contact) rearWheel.y = point.y;
        bool scraping = contact && (jumper != null ? jumper.IsGrounded : bounds.min.y - hit.point.y <= contactTolerance);
        fx.Tick(ribbonHead, rearWheel, scraping, speed, Time.deltaTime);
    }

    private void SetVisible(bool visible)
    {
        if (leftFx != null) { if (!visible) leftFx.Clear(); leftFx.gameObject.SetActive(visible); }
        if (rightFx != null) { if (!visible) rightFx.Clear(); rightFx.gameObject.SetActive(visible); }
    }
    private void OnDisable()
    {
        if (equipper != null) equipper.PairChanged -= RefreshPair;
        DisposePair();
    }
    private void DisposePair()
    {
        if (leftFx != null) { leftFx.gameObject.SetActive(false); Destroy(leftFx.gameObject); }
        if (rightFx != null) { rightFx.gameObject.SetActive(false); Destroy(rightFx.gameObject); }
        leftFx = rightFx = null; activePrefab = null;
    }
}
