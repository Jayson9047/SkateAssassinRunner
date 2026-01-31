using UnityEngine;

namespace MoreMountains.InfiniteRunnerEngine
{
    public class ReturnHomeTrigger : MonoBehaviour
    {
        [SerializeField] private Collider triggerCollider;

        private SwipeRightAttackDetector _currentReturner;

        private void Awake()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider>();

            if (triggerCollider != null)
                triggerCollider.enabled = false; // disabled by default
        }

        public void Arm(SwipeRightAttackDetector returner)
        {
            _currentReturner = returner;

            if (triggerCollider != null)
                triggerCollider.enabled = true;
        }

        private void Disarm()
        {
            if (triggerCollider != null)
                triggerCollider.enabled = false;

            _currentReturner = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_currentReturner == null) return;

            // Only react to the player that armed this trigger
            if (!other.CompareTag("Player")) return;

            _currentReturner.OnReturnHomeReached(transform.position.x);

            // One-shot trigger
            Disarm();
        }
    }
}
