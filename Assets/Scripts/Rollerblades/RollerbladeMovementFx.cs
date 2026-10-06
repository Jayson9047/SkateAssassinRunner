using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>One optional FX pair follows the actually equipped skates.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(RollerbladeEquipper))]
public sealed class RollerbladeMovementFx : MonoBehaviour
{
    [SerializeField, InspectorName("Rollerblade FX")]
    private bool rollerbladeFx = true;
    [SerializeField] private LayerMask groundLayers = 1 << 8;
    [SerializeField, Min(0.01f)] private float contactTolerance = 0.35f;
    [SerializeField, Min(0f)] private float worldScrollMultiplier = 0.5f;
    [SerializeField, Min(0f), Tooltip("Visual lift above the physical road plane; keeps glow clear of raised road meshes.")]
    private float groundFxLift = 0.18f;
    private RollerbladeEquipper equipper;
    private RollerbladeFootFx leftFx, rightFx;
    private Renderer leftBlade, rightBlade;
    private GameObject activePrefab;
    private bool previousEnabled;

    public bool RollerbladeFX
    {
        get => rollerbladeFx;
        set { rollerbladeFx = value; RefreshPair(); }
    }

    private void Awake()
    {
        equipper = GetComponent<RollerbladeEquipper>();
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
        SetVisible(true);
    }

    private void LateUpdate()
    {
        if (previousEnabled != rollerbladeFx) RefreshPair(); // Inspector live toggle.
        if (!rollerbladeFx || leftFx == null || !leftFx.gameObject.activeSelf) return;
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
        bool contact = Physics.Raycast(wheel + Vector3.up * contactTolerance, Vector3.down,
            out hit, contactTolerance * 2f, groundLayers, QueryTriggerInteraction.Ignore);
        Vector3 point = contact ? hit.point + Vector3.up * groundFxLift : wheel;
        // Imported wheel meshes can dip below the motor's contact plane. Keep
        // the luminous ribbon visible above the road rather than inside it.
        Vector3 ribbonHead = contact
            ? new Vector3(wheel.x, Mathf.Max(wheel.y, point.y), wheel.z) : wheel;
        fx.Tick(ribbonHead, point, contact, speed, Time.deltaTime);
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
