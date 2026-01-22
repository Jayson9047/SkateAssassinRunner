using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;
using System.Collections;

/// <summary>
/// Enemy Type 2:
/// - Has a shield trigger collider.
/// - If player touches shield: push player back on X axis, stop level speed, play player hit/fall anim,
///   then enemy attacks using EnemyBase sword/attack logic (existing kill logic).
/// </summary>
public class EnemyType2 : EnemyBase
{
    [Header("Shield")]
    [Tooltip("A trigger collider on a child object (e.g. 'ShieldTrigger') representing the shield volume.")]
    [SerializeField] private Collider shieldTriggerCollider;

    [Tooltip("Tag used to detect player collider (defaults to EnemyBase playerTag).")]
    [SerializeField] private string shieldPlayerTagOverride = ""; // optional, leave empty to use base

    [Header("Pushback")]
    [Tooltip("How many units to push the player on the X axis when shield is touched.")]
    [SerializeField] private float pushbackX = 1.5f;

    [Tooltip("If true, push direction will be away from the enemy based on relative X positions.")]
    [SerializeField] private bool pushAwayFromEnemy = true;

    [Tooltip("How long (seconds) the pushback takes. 1.0 = slow, cinematic shove.")]
    [SerializeField] private float pushbackDuration = 1.0f;

    [Tooltip("If true, uses unscaled time (still moves while LevelManager speed is 0).")]
    [SerializeField] private bool pushbackUseUnscaledTime = true;


    [Header("Player Reaction")]
    [Tooltip("Animator trigger to fire on the player when hit (hit/fall).")]
    [SerializeField] private string playerHitFallTrigger = "HitFall";

    [Tooltip("Delay before triggering enemy attack after shield hit (lets hit/fall start).")]
    [SerializeField] private float attackDelay = 0.15f;

    [Header("Execution (Cinematic)")]
    [SerializeField] private bool useCinematicExecution = true;

    [Tooltip("A transform on the PLAYER that marks where the enemy should walk to (e.g. Player/ExecutionPoint).")]
    [SerializeField] private string playerExecutionPointName = "ExecutionPoint";

    [Tooltip("Enemy walks along X only to this point (stops when within this distance).")]
    [SerializeField] private float executionStopDistance = 0.15f;

    [SerializeField] private float executionWalkSpeed = 2.0f;

    [Tooltip("Animator bool param for walking (or leave empty to skip).")]
    [SerializeField] private string enemyWalkBool = "IsWalking";

    [Tooltip("Animator trigger for shooting execution animation (or leave empty to skip).")]
    [SerializeField] private string enemyShootTrigger = "Shoot";

    [SerializeField] private float groundedWaitTimeout = 3.0f; // optional

    [Tooltip("Animator layer name that holds the katana overlay.")]
    [SerializeField] private string LowerBodyLayerName = "LowerBody";

    [Header("Shoot State (Animator)")]
    [SerializeField] private string enemyShootStateName = "ShootOnGround"; // <-- set this to your actual state name
    [Header("Shoot Timing (Frame-Based)")]
    [SerializeField] private int shootKillFrame = 34;
    [SerializeField] private int shootTotalFrames = 86;

    [Header("Execution Rotation")]
    [SerializeField] private float executionTargetYaw = 180f;
    [SerializeField] private float rotationSpeedDegPerSec = 360f; // tune: 180–720 feels good
    [SerializeField] private Transform rotateRoot; // optional: assign mesh root if needed


    [Header("Shield Visual & Break")]
    [Tooltip("Assign the *intact* shield GameObject (the one parented under the hand in Armature).")]
    [SerializeField] private GameObject intactShieldObject;

    [Tooltip("Prefab of the broken shield pieces (with rigidbodies/colliders).")]
    [SerializeField] private GameObject brokenShieldPrefab;

    [Tooltip("Explosion force applied to broken shield pieces.")]
    [SerializeField] private float shieldExplosionForce = 250f;

    [Tooltip("Explosion radius applied to broken shield pieces.")]
    [SerializeField] private float shieldExplosionRadius = 2f;

    [Tooltip("Optional upward bias so pieces pop a bit.")]
    [SerializeField] private float shieldExplosionUpward = 0.25f;

    [Tooltip("If true, shield trigger collider gets disabled once shattered.")]
    [SerializeField] private bool disableShieldTriggerOnShatter = true;

    [Header("Shield Shatter Effects")]
    [SerializeField] private string damageableLayerName = "Damageable";

    [Tooltip("How far enemy slides back on X when shield shatters.")]
    [SerializeField] private float shatterPushbackX = 5f;

    [Tooltip("How long the enemy shatter pushback takes.")]
    [SerializeField] private float shatterPushbackDuration = 0.25f;

    [Tooltip("Animator trigger to fire when shield shatters (enemy reacts / staggers).")]
    [SerializeField] private string enemyShieldShatterTrigger = "ShieldShatter";

    [Tooltip("If true, uses unscaled time for the shatter pushback.")]
    [SerializeField] private bool shatterPushbackUseUnscaledTime = true;

    [Header("Shield Reliability")]
    [SerializeField] private bool enableShieldFailsafe = true;

    // Optional: set in inspector to limit checks to the player layer.
    // If you don't set it, we'll fall back to tag checking.
    [SerializeField] private LayerMask playerMask = 0;

    private Coroutine _shatterPushbackRoutine;
    private int _damageableLayer = -1;
    public bool IsShieldIntact => _shieldIntact;
    private bool _shieldIntact = true;

    private int LowerBodyLayerIndex = -1;

    private Coroutine _executionRoutine;
    private Transform _playerRoot;
    private Transform _playerExecutionPoint;
    private Jumper _playerJumper;
    private Collider _playerMainCollider;


    private Coroutine _pushbackRoutine;

    private bool _shieldTriggered;

    protected override void Awake()
    {
        base.Awake();

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            // Prefer a CapsuleCollider/CharacterController if you use one
            _playerMainCollider =
                playerGo.GetComponentInChildren<CapsuleCollider>(true) ??
                playerGo.GetComponentInChildren<CharacterController>(true) as Collider ??
                playerGo.GetComponentInChildren<Collider>(true);
        }

        _damageableLayer = LayerMask.NameToLayer(damageableLayerName);
        if (_damageableLayer < 0)
            Debug.LogWarning($"{name}: Layer '{damageableLayerName}' not found. Layer swap won't work.");

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
            LowerBodyLayerIndex = animator.GetLayerIndex(LowerBodyLayerName);
        // Auto-find a child named "ShieldTrigger" if not assigned
        if (shieldTriggerCollider == null)
        {
            Transform t = transform.Find("ShieldTrigger");
            if (t != null)
                shieldTriggerCollider = t.GetComponent<Collider>();
        }

        // Attach relay to shield trigger
        if (shieldTriggerCollider != null)
        {
            if (!shieldTriggerCollider.isTrigger)
                Debug.LogWarning($"{name}: ShieldTrigger collider must have IsTrigger enabled.");

            var relay = shieldTriggerCollider.GetComponent<ShieldHitRelay>();
            if (relay == null)
                relay = shieldTriggerCollider.gameObject.AddComponent<ShieldHitRelay>();

            string tagToUse = string.IsNullOrEmpty(shieldPlayerTagOverride) ? playerTag : shieldPlayerTagOverride;
            relay.Init(this, tagToUse);
        }
        else
        {
            Debug.LogWarning($"{name}: No ShieldTrigger assigned/found. EnemyType2 shield won't work.");
        }
    }

    private void Update()
    {
        if (!enableShieldFailsafe) return;
        if (!_shieldIntact) return;
        if (_shieldTriggered) return;
        if (shieldTriggerCollider == null) return;
        if (_playerMainCollider == null) return;

        bool overlapped = Physics.ComputePenetration(
            _playerMainCollider, _playerMainCollider.transform.position, _playerMainCollider.transform.rotation,
            shieldTriggerCollider, shieldTriggerCollider.transform.position, shieldTriggerCollider.transform.rotation,
            out _, out _
        );

        if (!overlapped) return;

        NotifyPlayerTouchedShield(_playerMainCollider);
    }
    private void LateUpdate()
    {
        if (!enableShieldFailsafe) return;
        if (!_shieldIntact) return;
        if (_shieldTriggered) return;
        if (shieldTriggerCollider == null) return;
        if (_playerMainCollider == null) return;

        bool overlapped = Physics.ComputePenetration(
            _playerMainCollider, _playerMainCollider.transform.position, _playerMainCollider.transform.rotation,
            shieldTriggerCollider, shieldTriggerCollider.transform.position, shieldTriggerCollider.transform.rotation,
            out _, out _
        );

        if (!overlapped) return;

        NotifyPlayerTouchedShield(_playerMainCollider);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _shieldTriggered = false;

        _shieldIntact = true;

        if (shieldTriggerCollider != null)
            shieldTriggerCollider.enabled = true;

        if (intactShieldObject != null)
            intactShieldObject.SetActive(true);
    }

    public void ShatterShield(Vector3 hitPoint, float slamCenterX, bool applyPushbackAndAnim)
    {
        if (!_shieldIntact) return;
        _shieldIntact = false;

        if (disableShieldTriggerOnShatter && shieldTriggerCollider != null)
            shieldTriggerCollider.enabled = false;

        if (_damageableLayer >= 0)
            ApplyLayerRecursive(transform, _damageableLayer);

        if (applyPushbackAndAnim)
        {
            if (animator != null && !string.IsNullOrEmpty(enemyShieldShatterTrigger))
            {
                animator.ResetTrigger(enemyShieldShatterTrigger);
                animator.SetTrigger(enemyShieldShatterTrigger);
            }

            if (_shatterPushbackRoutine != null)
                StopCoroutine(_shatterPushbackRoutine);

            float dir = (transform.position.x >= slamCenterX) ? 1f : -1f;
            float targetX = transform.position.x + (dir * Mathf.Abs(shatterPushbackX));

            _shatterPushbackRoutine = StartCoroutine(
                MoveEnemyXOverTime(targetX, shatterPushbackDuration, shatterPushbackUseUnscaledTime)
            );
        }

        // Spawn broken pieces at intact shield location
        if (brokenShieldPrefab != null && intactShieldObject != null)
        {
            Transform t = intactShieldObject.transform;

            GameObject broken = Instantiate(
                brokenShieldPrefab,
                t.position,
                t.rotation
            );

            // Optional: don't parent to enemy (we want physics to fly freely)
            broken.transform.SetParent(null);

            // Apply explosion force to all rigidbodies in the broken prefab
            Rigidbody[] rbs = broken.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++)
            {
                if (rbs[i] == null) continue;
                rbs[i].AddExplosionForce(
                    shieldExplosionForce,
                    hitPoint,
                    shieldExplosionRadius,
                    shieldExplosionUpward,
                    ForceMode.Impulse
                );
            }
        }

        // Hide intact shield mesh last
        if (intactShieldObject != null)
            intactShieldObject.SetActive(false);
    }

    private static void ApplyLayerRecursive(Transform root, int layer)
    {
        if (root == null) return;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    public void PowerSlamKill(Vector3 hitPoint, float slamCenterX)
    {
        // We want the VISUAL: shield shatters
        // And the RESULT: enemy dies no matter what.

        // If shield is still intact, shatter it first (this switches to Damageable too)
        if (_shieldIntact)
        {
            ShatterShield(hitPoint, slamCenterX, false);
        }

        // Kill the enemy using destructible system (preferred)
        var destructible = GetComponent<IndieKit.SkateRunnerDestructibleObject>();
        if (destructible != null)
        {
            destructible.ApplyDamage(999999f, hitPoint);
            return;
        }

        // Fallback
        gameObject.SetActive(false);
    }


    private IEnumerator MoveEnemyXOverTime(float targetX, float duration, bool useUnscaledTime)
    {
        duration = Mathf.Max(0.01f, duration);

        float startX = transform.position.x;
        float t = 0f;

        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / duration);
            // Same smoothstep easing style you used elsewhere (feels less robotic)
            u = u * u * (3f - 2f * u);

            Vector3 p = transform.position;
            p.x = Mathf.Lerp(startX, targetX, u);
            transform.position = p;

            yield return null;
        }

        Vector3 finalPos = transform.position;
        finalPos.x = targetX;
        transform.position = finalPos;

        _shatterPushbackRoutine = null;
    }


    /// <summary>
    /// EnemyBase reach behavior (we can keep it empty for now if you ONLY want shield to cause the attack).
    /// Or you can let reach trigger attack too—your call.
    /// </summary>
    protected override void OnPlayerInReach(Collider playerCollider)
    {
        // For now: do nothing.
        // We want shield touch to be the trigger for the "pushback -> fall -> kill" sequence.
    }

    /// <summary>
    /// Called by the shield relay when player touches the shield trigger.
    /// </summary>
    public void NotifyPlayerTouchedShield(Collider playerCollider)
    {
        if (!_shieldIntact) return;
        _playerRoot = playerCollider.transform.root;
        _playerJumper = _playerRoot.GetComponentInChildren<Jumper>(true);
        // If player is invincible, ignore shield collision completely
        if (_playerJumper != null && _playerJumper.Invincible) // or whatever your flag/property is
            return;

        // Down attack is invincible: ignore shield touch during downslam.
        var down = playerCollider.GetComponentInParent<SwipeDownDetector>();
        if (down != null && down.IsDownAttacking)
            return;

        if (_shieldTriggered)
            return;

        _shieldTriggered = true;
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LockGameplayInputs();
        }

        // If player is currently doing a right-swipe dash, stop it immediately.
        var dash = playerCollider.GetComponentInParent<SwipeRightAttackDetector>();
        if (dash != null && dash.IsAttacking)
        {
            dash.AbortDashDueToShield();
        }

        // 1) Pushback player on X axis
        ApplyPushbackOnX(playerCollider);

        // 2) Stop level speed
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.FreezeSpeedAndCancelBoost();
        }


        // 3) Trigger player hit/fall animation
        TriggerPlayerHitFall(playerCollider);

        if (useCinematicExecution)
        {
            // Stop any previous execution routine
            if (_executionRoutine != null) StopCoroutine(_executionRoutine);

            _playerRoot = playerCollider.transform.root;
            _playerJumper = _playerRoot.GetComponentInChildren<Jumper>(true);
            if (_playerJumper == null)
            {
                Debug.LogWarning($"{name}: Jumper component not found on player. Execution will use timing only.");
            }

            _playerExecutionPoint = FindPlayerExecutionPoint(_playerRoot);

            _executionRoutine = StartCoroutine(CinematicExecutionRoutine(_playerRoot, _playerExecutionPoint));
        }
        else
        {
            // fallback: old behavior
            Invoke(nameof(TriggerAttack), Mathf.Max(0f, attackDelay));
        }
    }


    private Transform FindPlayerExecutionPoint(Transform playerRoot)
    {
        if (playerRoot == null) return null;

        var t = playerRoot.Find(playerExecutionPointName);
        if (t != null) return t;

        // Fallback: try deep search by name (slower but only happens on death)
        foreach (Transform child in playerRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == playerExecutionPointName)
                return child;
        }

        return null;
    }

    private IEnumerator CinematicExecutionRoutine(Transform playerRoot, Transform executionPoint)
    {
        // Let hit/fall start first
        yield return new WaitForSeconds(attackDelay);

        // Wait for pushback to finish (match pushback timing)
        if (pushbackDuration > 0f)
        {
            float t = 0f;
            while (t < pushbackDuration)
            {
                t += pushbackUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        // Now wait until player is grounded (real-time from Jumper)
        yield return WaitUntilPlayerGrounded_JumperOrTimeout();

        // ---- Only NOW enemy starts moving ----

        if (animator != null && !string.IsNullOrEmpty(enemyWalkBool))
            animator.SetBool(enemyWalkBool, true);

        float targetX = (executionPoint != null) ? executionPoint.position.x : playerRoot.position.x;

        while (Mathf.Abs(transform.position.x - targetX) > executionStopDistance)
        {
            float step = executionWalkSpeed * Time.deltaTime;
            float newX = Mathf.MoveTowards(transform.position.x, targetX, step);

            Vector3 p = transform.position;
            p.x = newX;
            transform.position = p;

            yield return null;
        }

        if (animator != null && LowerBodyLayerIndex >= 0)
            animator.SetLayerWeight(LowerBodyLayerIndex, 0f);

        if (animator != null && !string.IsNullOrEmpty(enemyWalkBool))
            animator.SetBool(enemyWalkBool, false);

        // Shoot
        if (animator != null && !string.IsNullOrEmpty(enemyShootTrigger))
        {
            animator.SetTrigger(enemyShootTrigger);
            yield return WaitForShootKillFrame();
        }
    }

    private IEnumerator WaitForShootKillFrame()
    {
        if (animator == null)
            yield break;

        int shootStateHash = Animator.StringToHash(enemyShootStateName);

        // 1) WAIT until we're actually in the shoot state (not walking, not transition)
        while (true)
        {
            // Prefer "next state" while transitioning, because the shoot may already be queued
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == shootStateHash)
                    break;
            }
            else
            {
                var cur = animator.GetCurrentAnimatorStateInfo(0);
                if (cur.shortNameHash == shootStateHash)
                    break;
            }

            yield return null;
        }

        // 2) NOW we are in (or transitioning into) shoot. Wait until it fully becomes current.
        while (animator.IsInTransition(0))
            yield return null;

        //turn toward player
        while (!RotateYawToward(executionTargetYaw, rotationSpeedDegPerSec))
            yield return null;

        // 3) Start counting from the shoot state's normalized time
        float killNormalized = Mathf.Clamp01((float)shootKillFrame / shootTotalFrames);
        bool killed = false;

        while (true)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);

            // Safety: if we somehow left shoot state, stop.
            if (st.shortNameHash != shootStateHash)
                yield break;

            // Kill exactly once when we cross the target normalized time
            if (!killed && st.normalizedTime >= killNormalized)
            {
                KillPlayerNow(_playerRoot);
                killed = true;
            }

            // Let shoot animation keep playing
            if (st.normalizedTime >= 1f)
            {
                // After shooting, move sideways away so respawn doesn't collide instantly.
                // Use existing pushback values (no new tuning vars).
                float dir = 1f;
                if (animator != null)
                    animator.SetLayerWeight(LowerBodyLayerIndex,0.8f);
                //turn Back on x axis
                while (!RotateYawToward(270, rotationSpeedDegPerSec))
                    yield return null;
                if (_playerRoot != null)
                {
                    // Move away from player’s side on X (if enemy is left of player, go further left, etc.)
                    dir = Mathf.Sign(transform.position.x - _playerRoot.position.x);
                    if (Mathf.Approximately(dir, 0f)) dir = 1f;
                }

                float targetX = transform.position.x + dir * shatterPushbackX;

                yield return MoveEnemyXOverTime(targetX, 2f, shatterPushbackUseUnscaledTime);

                yield break;
            }

            yield return null;
        }
    }

    private void KillPlayerNow(Transform playerRoot)
    {
        if (playerRoot == null) return;

        // we ask the LevelManager to kill the character
        LevelManager.Instance.KillCharacter(_playerJumper);
    }


    /// <summary>
    /// OPTIONAL: Put an Animation Event on the exact frame of the gunshot,
    /// calling this method instead of using shootKillDelay.
    /// </summary>
    public void OnShootKillFrame()
    {
        KillPlayerNow(_playerRoot);
    }

    public void NormalSlamKill(Vector3 hitPoint)
    {
        if (_shieldIntact) return; // only kill if already unshielded

        var destructible = GetComponent<IndieKit.SkateRunnerDestructibleObject>();
        if (destructible != null)
        {
            destructible.ApplyDamage(999999f, hitPoint);
        }
    }

    private IEnumerator WaitUntilPlayerGrounded_JumperOrTimeout()
    {
        // If no jumper, just bail (we’ll rely on pushbackDuration timing)
        if (_playerJumper == null)
            yield break;

        float t = 0f;

        // Use unscaled time so this still works even if you later slow time
        while (!_playerJumper.IsGrounded)
        {
            float dt = pushbackUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (groundedWaitTimeout > 0f && t >= groundedWaitTimeout)
            {
                Debug.LogWarning($"{name}: Timed out waiting for player to be grounded. Continuing execution anyway.");
                yield break;
            }

            yield return null;
        }
    }


    private void ApplyPushbackOnX(Collider playerCollider)
    {
        Transform playerT = playerCollider.transform;

        // If player's collider is a child, prefer root to move whole character
        if (playerCollider.attachedRigidbody != null)
            playerT = playerCollider.attachedRigidbody.transform;
        else if (playerCollider.transform.root != null)
            playerT = playerCollider.transform.root;

        float dir = -1f; // default: push left
        if (pushAwayFromEnemy)
        {
            // Push away based on relative X positions
            dir = (playerT.position.x >= transform.position.x) ? 1f : -1f;
        }

        float targetX = playerT.position.x + (dir * pushbackX);

        // Stop any previous pushback so we don't stack motion
        if (_pushbackRoutine != null)
            StopCoroutine(_pushbackRoutine);

        _pushbackRoutine = StartCoroutine(PushbackXOverTime(playerT, targetX, pushbackDuration));
    }

    private IEnumerator PushbackXOverTime(Transform playerT, float targetX, float duration)
    {
        if (playerT == null) yield break;

        float startX = playerT.position.x;
        float t = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            float dt = pushbackUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / duration);
            // Smoothstep easing (feels more “shove” than linear)
            u = u * u * (3f - 2f * u);

            Vector3 p = playerT.position;
            p.x = Mathf.Lerp(startX, targetX, u);
            playerT.position = p;

            yield return null;
        }

        // Final snap to guarantee exact position
        Vector3 finalPos = playerT.position;
        finalPos.x = targetX;
        playerT.position = finalPos;

        _pushbackRoutine = null;
    }

    private bool RotateYawToward(float targetYaw, float speedDegPerSec)
    {
        Transform t = rotateRoot != null ? rotateRoot : transform;

        float current = t.eulerAngles.y;
        float next = Mathf.MoveTowardsAngle(current, targetYaw, speedDegPerSec * Time.deltaTime);

        Vector3 e = t.eulerAngles;
        e.y = next;
        t.eulerAngles = e;

        // Return true when we’re basically there
        return Mathf.Abs(Mathf.DeltaAngle(next, targetYaw)) < 0.5f;
    }

    private void TriggerPlayerHitFall(Collider playerCollider)
    {
        // Try find animator on player (root or children)
        Animator playerAnim = null;

        if (playerCollider.attachedRigidbody != null)
            playerAnim = playerCollider.attachedRigidbody.GetComponentInChildren<Animator>(true);

        if (playerAnim == null)
            playerAnim = playerCollider.GetComponentInChildren<Animator>(true);

        if (playerAnim == null && playerCollider.transform.root != null)
            playerAnim = playerCollider.transform.root.GetComponentInChildren<Animator>(true);

        if (playerAnim == null)
        {
            Debug.LogWarning($"{name}: Could not find Animator on player to trigger '{playerHitFallTrigger}'.");
            return;
        }

        if (!string.IsNullOrEmpty(playerHitFallTrigger))
        {
            playerAnim.ResetTrigger(playerHitFallTrigger);
            playerAnim.SetTrigger(playerHitFallTrigger);
        }
    }

    /// <summary>
    /// Lives on the shield trigger and forwards OnTriggerEnter to the enemy.
    /// </summary>
    private class ShieldHitRelay : MonoBehaviour
    {
        private EnemyType2 owner;
        private string playerTag;

        public void Init(EnemyType2 enemy, string playerTag)
        {
            owner = enemy;
            this.playerTag = playerTag;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;

            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
                return;

            owner.NotifyPlayerTouchedShield(other);
        }
        //private void OnTriggerStay(Collider other)
        //{
        //    if (owner == null) return;

        //    string playerTag = string.IsNullOrEmpty(playerTagOverride) ? owner.playerTag : playerTagOverride;
        //    if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        //        return;

        //    owner.NotifyPlayerTouchedShield(other);
        //}

    }
}
