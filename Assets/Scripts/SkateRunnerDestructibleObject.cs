using UnityEngine;

namespace IndieKit
{
    public class SkateRunnerDestructibleObjects : MonoBehaviour, IDamageable
    {
        [SerializeField] private float health = 1f;
        [SerializeField] private GameObject DebrisPrefab;

        private float _initialHealth;

        private void Awake()
        {
            _initialHealth = health;
        }

        private void OnEnable()
        {
            // IMPORTANT: pooled objects come back enabled, so reset health
            health = _initialHealth;
        }

        public void ApplyDamage(float damage, Vector3 hitPoint)
        {
            health -= damage;

            if (health > 0f)
                return;

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

            // DO NOT destroy pooled objects. Return to pool by disabling.
            gameObject.SetActive(false);
        }
    }
}
