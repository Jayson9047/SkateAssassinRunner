using UnityEngine;

/// <summary>
/// Behavior-only base class for enemies.
/// Health/damage/death are handled by SkateRunnerDestructibleObject (already attached).
/// This class only handles: player reach detection -> attack trigger cadence.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] protected Animator animator;

    [Tooltip("Animator trigger name to play attack.")]
    [SerializeField] protected string attackTrigger = "Attack";

    [Header("Player Detection")]
    [Tooltip("A child trigger collider placed in front of the enemy that detects the player.")]
    [SerializeField] protected Collider playerDetectionTrigger;

    [Tooltip("Tag used to detect player collider.")]
    [SerializeField] protected string playerTag = "Player";

    [Header("Attack Cadence")]
    [Tooltip("If true, enemy attacks only once when the player enters reach.")]
    [SerializeField] protected bool attackOnlyOnce = true;

    [Header("Weapon")]
    [SerializeField] private Collider swordCollider;

    [SerializeField] private string attackStateName = "EnemyAttack";
    [SerializeField] private int swordEnableFrame = 1;

    protected bool hasAttacked;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        // Auto-find a child named "PlayerDetectionTrigger" if not assigned
        if (playerDetectionTrigger == null)
        {
            Transform t = transform.Find("PlayerDetectionTrigger");
            if (t != null)
                playerDetectionTrigger = t.GetComponent<Collider>();
        }

        // Attach relay to the trigger so we don't need a separate script on the child object
        if (playerDetectionTrigger != null)
        {
            if (!playerDetectionTrigger.isTrigger)
                Debug.LogWarning($"{name}: PlayerDetectionTrigger collider must have IsTrigger enabled.");

            var relay = playerDetectionTrigger.GetComponent<PlayerReachRelay>();
            if (relay == null)
                relay = playerDetectionTrigger.gameObject.AddComponent<PlayerReachRelay>();

            relay.Init(this, playerTag);
        }
        else
        {
            Debug.LogWarning($"{name}: No PlayerDetectionTrigger assigned/found. Enemy will never attack.");
        }
    }

    protected virtual void OnEnable()
    {
        // Enemy is likely pooled. Reset per-spawn state.
        hasAttacked = false;
        if (swordCollider != null)
            swordCollider.enabled = false;
    }

    /// <summary>
    /// Called by relay when player enters the detection trigger.
    /// </summary>
    public void NotifyPlayerEnteredReach(Collider playerCollider)
    {
        if (attackOnlyOnce && hasAttacked)
            return;

        OnPlayerInReach(playerCollider);
    }

    /// <summary>
    /// Enemy-type-specific behavior on reach.
    /// </summary>
    protected abstract void OnPlayerInReach(Collider playerCollider);

    protected void TriggerAttack()
    {
        if (attackOnlyOnce && hasAttacked)
            return;

        hasAttacked = true;

        if (animator == null || string.IsNullOrEmpty(attackTrigger))
            return;

        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);

        StartCoroutine(EnableSwordAtFrameRoutine());
    }

    private System.Collections.IEnumerator EnableSwordAtFrameRoutine()
    {
        if (swordCollider == null || animator == null)
            yield break;

        // Always start disabled until we hit the frame
        swordCollider.enabled = false;

        // Wait until animator enters the attack state
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(attackStateName))
            yield return null;

        // Get current clip to read framerate
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            yield break;

        AnimationClip clip = clips[0].clip;
        float frameRate = clip.frameRate;

        // Frame -> seconds
        float enableTime = swordEnableFrame / frameRate;

        yield return new WaitForSeconds(enableTime);

        // Enable ONCE and never disable again
        swordCollider.enabled = true;
    }



    /// <summary>
    /// Lives on the detection trigger and forwards OnTriggerEnter to the enemy.
    /// </summary>
    private class PlayerReachRelay : MonoBehaviour
    {
        private EnemyBase owner;
        private string playerTag;

        public void Init(EnemyBase enemy, string playerTag)
        {
            owner = enemy;
            this.playerTag = playerTag;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;

            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
                return;

            owner.NotifyPlayerEnteredReach(other);
        }
    }

}
