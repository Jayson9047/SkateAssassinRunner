using System.Collections;
using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

[RequireComponent(typeof(PlayableCharacter))]
public class PlayerPhase2Controller : MonoBehaviour
{
    [Header("Phase 2 Red Fail Settings")]
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private string phase2DeadTriggerName = "Phase2Dead";

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
