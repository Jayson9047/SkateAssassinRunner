using IndieKit;
using UnityEngine;

public class EnemyType3 : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator Animator;

    [Tooltip("Trigger name used to start aiming animation.")]
    [SerializeField] private string StartAimingTriggerName = "StartAiming";

    [Tooltip("Animator layer used for upper-body aiming.")]
    [SerializeField] private string UpperBodyLayerName = "UpperBody";

    [Header("Phase2 Kill Shot")]
    [SerializeField] private Transform firePoint;                 // assign in prefab (gun muzzle)
    [SerializeField] private GameObject projectilePrefab;          // assign (Toon projectile prefab)
    [SerializeField] private bool aimAtPlayer = true;              // recommended for Phase2
    [SerializeField] private Vector3 playerAimOffset = new Vector3(0f, 0.9f, 0f); // chest-ish
    [SerializeField] private float killShotProjectileSpeed = 14f;

    [Tooltip("Prevents double-shoot on the same fail event.")]
    [SerializeField] private bool shootOnlyOncePerPhase2 = true;

    private bool _killShotFired;

    private int _startAimingTriggerHash;
    private int _upperBodyLayerIndex = -1;

    private void Awake()
    {
        if (Animator == null)
        {
            Animator = GetComponentInChildren<Animator>(true);
        }

        if (Animator != null)
        {
            _startAimingTriggerHash = Animator.StringToHash(StartAimingTriggerName);
            _upperBodyLayerIndex = Animator.GetLayerIndex(UpperBodyLayerName);

            if (_upperBodyLayerIndex < 0)
            {
                Debug.LogWarning(
                    $"[EnemyType3] Animator layer '{UpperBodyLayerName}' not found on {gameObject.name}"
                );
            }
        }

        if (firePoint == null)
        {
            // Optional convenience: tries to find a child named FirePoint
            var t = transform.Find("FirePoint");
            if (t != null) firePoint = t;
        }
    }

    private void OnEnable()
    {
        _killShotFired = false;
    }

    /// <summary>
    /// Called when Phase2 car begins diagonal merge.
    /// Triggers aiming animation and blends in upper-body aiming layer.
    /// </summary>
    public void StartAiming()
    {
        if (Animator == null) return;

        if (_upperBodyLayerIndex >= 0)
        {
            Animator.SetLayerWeight(_upperBodyLayerIndex, 1f);
        }

        Animator.ResetTrigger(_startAimingTriggerHash);
        Animator.SetTrigger(_startAimingTriggerHash);
    }

    /// <summary>
    /// Fire one “execution” shot when Phase2 fails (timer hits 0 / red result).
    /// Uses the same projectile prefab + SimpleProjectile as the drone.
    /// </summary>
    public void ShootKillShot()
    {
        if (shootOnlyOncePerPhase2 && _killShotFired) return;
        _killShotFired = true;

        if (firePoint == null || projectilePrefab == null)
        {
            Debug.LogWarning("[EnemyType3] Missing firePoint or projectilePrefab for kill shot.");
            return;
        }

        Vector3 dir = firePoint.forward;

        if (aimAtPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 target = player.transform.position + playerAimOffset;
                dir = (target - firePoint.position).normalized;
            }
        }

        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));

        var projectile = go.GetComponent<SimpleProjectile>();
        if (projectile != null)
        {
            projectile.Init(dir, transform.root, killShotProjectileSpeed);
        }
    }
}
