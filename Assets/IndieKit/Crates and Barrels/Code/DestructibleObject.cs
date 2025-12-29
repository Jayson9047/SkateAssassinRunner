using UnityEngine;

namespace IndieKit
{
    public class DestructibleObject : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private float health = 1f;

        [SerializeField]
        private GameObject DebrisPrefab;

        public void ApplyDamage(float damage, Vector3 hitPoint)
        {
            health -= damage;

            if (health <= 0f)
            {
                if (DebrisPrefab != null)
                {
                    GameObject debris = Instantiate(
                        DebrisPrefab,
                        transform.position,
                        transform.rotation
                    );

                    debris.transform.localScale = transform.localScale;

                    for (int i = 0; i < debris.transform.childCount; i++)
                    {
                        Transform child = debris.transform.GetChild(i);

                        if (child.TryGetComponent(out Rigidbody rb))
                        {
                            //Animate the debris to explode outward
                            rb.AddExplosionForce(4f, hitPoint, 1.5f, 0f, ForceMode.Impulse);
                        }
                    }
                }

                Destroy(gameObject);
            }
        }
    }
}
