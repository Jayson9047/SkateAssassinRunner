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

        public void ApplyDamage(float damage, Vector3 hitPoint)
        {
            if (_isDead) return;

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
    }
}
