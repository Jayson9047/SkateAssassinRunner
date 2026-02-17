using System.Collections;
using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

[RequireComponent(typeof(PlayableCharacter))]
public class PlayerPhase2Controller : MonoBehaviour
{
    [Header("Phase 2 Red Fail Settings")]
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private string phase2DeadTriggerName = "Phase2Dead";

    [Header("Phase 2 Execution (Bullet-driven)")]
    [SerializeField] private float executionFailsafeSeconds = 0f; // set 0 to disable
    public bool Phase2ExecutionPending { get; private set; }
    private Coroutine _executionFailsafeCo;

    private Animator _animator;
    private PlayableCharacter _playableCharacter;
    private bool _phase2DeathInProgress;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _playableCharacter = GetComponent<PlayableCharacter>();

        if (_animator == null)
        {
            Debug.LogError("[PlayerPhase2Controller] Animator not found in children.");
        }
    }

    private void OnEnable()
    {
        _phase2DeathInProgress = false;
        Phase2ExecutionPending = false;

        if (_executionFailsafeCo != null)
        {
            StopCoroutine(_executionFailsafeCo);
            _executionFailsafeCo = null;
        }
    }

    public void BeginPhase2ExecutionPending()
    {
        // Don’t start it twice
        if (Phase2ExecutionPending) return;

        Phase2ExecutionPending = true;

        // Optional failsafe to avoid soft-lock if bullet misses (0 disables)
        if (executionFailsafeSeconds > 0f)
        {
            if (_executionFailsafeCo != null) StopCoroutine(_executionFailsafeCo);
            _executionFailsafeCo = StartCoroutine(ExecutionFailsafeCo());
        }
    }

    public void OnHitByPhase2ExecutionBullet()
    {
        if (!Phase2ExecutionPending) return;

        Phase2ExecutionPending = false;

        if (_executionFailsafeCo != null)
        {
            StopCoroutine(_executionFailsafeCo);
            _executionFailsafeCo = null;
        }

        TriggerPhase2RedFail();
    }

    private IEnumerator ExecutionFailsafeCo()
    {
        yield return new WaitForSeconds(executionFailsafeSeconds);
        _executionFailsafeCo = null;

        // If still pending, force the death to avoid hanging the run.
        if (Phase2ExecutionPending)
        {
            Phase2ExecutionPending = false;
            TriggerPhase2RedFail();
        }
    }


    /// <summary>
    /// Called when PowerMeter result is RED during Phase 2.
    /// This is a deterministic, cinematic death (not physics-based).
    /// </summary>
    public void TriggerPhase2RedFail()
    {
        if (_phase2DeathInProgress)
            return;

        _phase2DeathInProgress = true;
        StartCoroutine(Phase2RedFailCo());
    }

    private IEnumerator Phase2RedFailCo()
    {
        // 1) Simulate a regular jump (player intent)
        SimulateJump();

        // small delay so the jump visually starts
        //yield return new WaitForSeconds(jumpBeforeDeathDelay);

        // 2) Trigger Phase 2 specific death animation
        if (_animator != null)
        {
            _animator.ResetTrigger(phase2DeadTriggerName);
            _animator.SetTrigger(phase2DeadTriggerName);
        }

        // small buffer so animation state is entered cleanly
        yield return null;

        // small delay so the death visually ends
        yield return new WaitForSeconds(deathDelay);

        // 3) Kill the player using LevelManager (lives / respawn handled there)
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.KillCharacter(_playableCharacter);
        }
        else
        {
            Debug.LogError("[PlayerPhase2Controller] LevelManager.Instance is null.");
        }
    }


    /// <summary>
    /// Fires the same pathway as a normal jump, without unlocking controls.
    /// </summary>
    private void SimulateJump()
    {
        // This mirrors what TapOnlyMainActionZone does
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SendMessage("MainActionButtonDown", SendMessageOptions.DontRequireReceiver);
            InputManager.Instance.SendMessage("MainActionButtonUp", SendMessageOptions.DontRequireReceiver);
        }
    }
}
