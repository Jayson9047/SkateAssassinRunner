using System;
using UnityEngine;

namespace IndieKit
{
    public class SkateRunnerDestructibleObjects : MonoBehaviour, IDamageable
    {
        public static event Action<SkateRunnerDestructibleObjects> OnDestroyed; // global signal

        [SerializeField] private float health = 1f;
        [SerializeField] private GameObject DebrisPrefab;

        [Header("Gameplay")]
        [SerializeField] private bool countsAsEnemyKill = true;

        [Header("Optional SlowMo On Destroy")]
        [SerializeField] private float destroySlowMoScale = 0.12f;
        [SerializeField] private float destroySlowMoDurationRealtime = 2f;
        [SerializeField] private bool destroySlowMoAffectsPhysics = true;

        private float _initialHealth;
        private bool _isDead;

        private void Awake()
        {
            _initialHealth = health;
        }

        private void OnEnable()
        {
            health = _initialHealth;
            _isDead = false;
        }

        public void ApplyDamage(float damage, Vector3 hitPoint, bool triggerSlowMo = false)
        {
            if (_isDead) return;
            if (triggerSlowMo)
            {
                // Use your global defaults (or add fields on this component)
                SkateRunnerGameFeel.TriggerSlowMoStatic(destroySlowMoScale, destroySlowMoDurationRealtime, destroySlowMoAffectsPhysics);
            }
            health -= damage;
            if (health > 0f) return;

            _isDead = true;

            // spawn debris (NOT pooled)
            if (DebrisPrefab != null)
            {
                GameObject debris = Instantiate(DebrisPrefab, transform.position, transform.rotation);
                debris.transform.localScale = transform.localScale;

                for (int i = 0; i < debris.transform.childCount; i++)
                {
                    Transform child = debris.transform.GetChild(i);
                    if (child.TryGetComponent(out Rigidbody rb))
                    {
                        rb.AddExplosionForce(4f, hitPoint, 1.5f, 0f, ForceMode.Impulse);
                    }
                }
            }

            if (countsAsEnemyKill)
            {
                OnDestroyed?.Invoke(this);
            }

            gameObject.SetActive(false);
        }
        public void ResetDestructible()
        {
            _isDead = false;
        }
    }

}
