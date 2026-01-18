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
    }

    /// <summary>
    /// Called when Phase2 car begins diagonal merge.
    /// Triggers aiming animation and blends in upper-body aiming layer.
    /// </summary>
    public void StartAiming()
    {
        if (Animator == null) return;

        // Blend in upper-body aiming layer
        if (_upperBodyLayerIndex >= 0)
        {
            Animator.SetLayerWeight(_upperBodyLayerIndex, 1f);
        }

        // Fire aiming trigger
        Animator.ResetTrigger(_startAimingTriggerHash);
        Animator.SetTrigger(_startAimingTriggerHash);
    }
}
