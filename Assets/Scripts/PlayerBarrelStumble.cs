using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>Short animation/input penalty, independent of tutorial and phase input locks.</summary>
[DisallowMultipleComponent]
public sealed class PlayerBarrelStumble : MonoBehaviour
{
    [SerializeField] private bool barrelStumbleEnabled = true;
    [SerializeField, Range(.3f, 2f), Tooltip("Total trip and recovery time in gameplay seconds.")]
    private float stumbleDuration = 1.2f;
    [SerializeField, Range(.2f, .8f)] private float tripPortion = .5f;
    [SerializeField, Range(0f, .15f)] private float blendDuration = .06f;
    [SerializeField] private AnimationClip tripClip;
    [SerializeField] private AnimationClip recoveryClip;
    [SerializeField, Range(.1f, 1f), Tooltip("Use the same opening portion as normal landing recovery.")]
    private float recoveryClipPortion = .4f;

    public bool IsStumbling { get; private set; }
    public float Duration { get => stumbleDuration; set => stumbleDuration = Mathf.Clamp(value, .3f, 2f); }
    private Animator animator;
    private Jumper jumper;
    private SwipeDownDetector down;
    private SwipeRightAttackDetector attack;
    private int katanaLayer;
    private float elapsed, tripSeconds, totalSeconds, savedKatanaWeight;
    private bool recovering;
    private static readonly int TripState = Animator.StringToHash("Base Layer.BarrelTrip");
    private static readonly int RecoverState = Animator.StringToHash("Base Layer.BarrelRecover");
    private static readonly int SkateState = Animator.StringToHash("Base Layer.Anim_RollerBladeExpertForwardLeft");

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        jumper = GetComponent<Jumper>();
        down = GetComponent<SwipeDownDetector>();
        attack = GetComponent<SwipeRightAttackDetector>();
        katanaLayer = animator != null ? animator.GetLayerIndex("KatanaLayer") : -1;
    }

    public bool TryStumble()
    {
        if (!isActiveAndEnabled || !barrelStumbleEnabled || IsStumbling || jumper == null ||
            !jumper.isActiveAndEnabled || jumper.Invincible || Time.timeScale <= 0f ||
            GameManager.Instance == null || GameManager.Instance.Status != GameManager.GameStatus.GameInProgress ||
            (LevelManager.Instance != null && LevelManager.Instance.GameplayInputsLocked) ||
            (down != null && down.IsDownAttacking)) return false;

        down?.CancelSlideForStumble();
        attack?.CancelDashForStumble();
        IsStumbling = true;
        elapsed = 0f; recovering = false;
        totalSeconds = Mathf.Clamp(stumbleDuration, .3f, 2f);
        tripSeconds = totalSeconds * Mathf.Clamp(tripPortion, .2f, .8f);
        if (animator != null)
        {
            savedKatanaWeight = katanaLayer >= 0 ? animator.GetLayerWeight(katanaLayer) : 0f;
            if (katanaLayer >= 0) animator.SetLayerWeight(katanaLayer, 0f);
            animator.ResetTrigger("JustJumped"); animator.ResetTrigger("Attack"); animator.ResetTrigger("Slide");
            animator.SetFloat("BarrelTripSpeed", tripClip != null ? tripClip.length / tripSeconds : 1f);
            animator.SetFloat("BarrelRecoverSpeed", recoveryClip != null ? recoveryClip.length * recoveryClipPortion / (totalSeconds - tripSeconds) : 1f);
            animator.CrossFadeInFixedTime(TripState, blendDuration, 0, 0f);
        }
        return true;
    }

    private void Update()
    {
        if (!IsStumbling) return;
        var status = GameManager.Instance != null ? GameManager.Instance.Status : GameManager.GameStatus.GameOver;
        if (animator != null)
        {
            var state = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("HitFall") || state.IsName("KnockDown_Front_Big")) { Finish(false); return; }
        }
        if (jumper == null || !jumper.isActiveAndEnabled ||
            (status != GameManager.GameStatus.GameInProgress && status != GameManager.GameStatus.Paused))
        { Finish(false); return; }
        // Pausing never spends the penalty or releases another system's input lock.
        if (status == GameManager.GameStatus.Paused || Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        if (!recovering && elapsed >= tripSeconds)
        {
            recovering = true;
            if (animator != null)
            {
                // Ordinary landing uses the sword-holding overlay. Keep that same
                // upper-body pose while the existing landing clip restores balance.
                if (katanaLayer >= 0) animator.SetLayerWeight(katanaLayer, savedKatanaWeight);
                animator.CrossFadeInFixedTime(RecoverState, blendDuration, 0, 0f);
            }
        }
        if (elapsed >= totalSeconds) Finish(true);
    }

    private void Finish(bool resumeSkating)
    {
        if (!IsStumbling) return;
        IsStumbling = false;
        if (animator != null)
        {
            if (katanaLayer >= 0) animator.SetLayerWeight(katanaLayer, savedKatanaWeight);
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            bool ownsPose = current.fullPathHash == TripState || current.fullPathHash == RecoverState ||
                (animator.IsInTransition(0) && (next.fullPathHash == TripState || next.fullPathHash == RecoverState));
            if (resumeSkating && ownsPose) animator.CrossFadeInFixedTime(SkateState, blendDuration, 0, 0f);
        }
    }

    private void OnDisable() { Finish(true); }
}
