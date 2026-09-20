using System;
using UnityEngine;
using Lofelt.NiceVibrations;

namespace IndieKit
{
    public enum DestructibleAudioKind { Automatic, EnemyType1, EnemyType2, EnemyType3, FlyingDrone, Barrel, Other }

    public class SkateRunnerDestructibleObject : MonoBehaviour, IDamageable
    {
        public static event Action<SkateRunnerDestructibleObject> OnDestroyed;     // fires for ALL destroyed
        public static event Action<SkateRunnerDestructibleObject> OnEnemyKilled;   // fires only for enemies

        [SerializeField] private float health = 1f;
        [SerializeField] private GameObject DebrisPrefab;

        [Header("Gameplay")]
        [SerializeField] private bool countsAsEnemyKill = true;
        [Header("Audio Classification")]
        [SerializeField] private DestructibleAudioKind audioKind = DestructibleAudioKind.Automatic;
        public bool CountsAsEnemyKill => countsAsEnemyKill;

        public DestructibleAudioKind ResolveAudioKind()
        {
            // DroneRoot owns health; its EnemyTypeDrone lives beneath it. Do not
            // search transform.root: a pooled container may hold unrelated enemies.
            if (GetComponentInParent<EnemyTypeDrone>() || GetComponentInChildren<EnemyTypeDrone>(true))
                return DestructibleAudioKind.FlyingDrone;
            if (audioKind != DestructibleAudioKind.Automatic) return audioKind;
            if (GetComponentInParent<EnemyType1>() || GetComponentInChildren<EnemyType1>(true)) return DestructibleAudioKind.EnemyType1;
            if (GetComponentInParent<EnemyType2>() || GetComponentInChildren<EnemyType2>(true)) return DestructibleAudioKind.EnemyType2;
            if (GetComponentInParent<EnemyType3>() || GetComponentInChildren<EnemyType3>(true)) return DestructibleAudioKind.EnemyType3;
            return DestructibleAudioKind.Other;
        }

        [Header("Optional SlowMo On Destroy")]
        [SerializeField] private float destroySlowMoScale = 0.12f;
        [SerializeField] private float destroySlowMoDurationRealtime = 2f;
        [SerializeField] private bool destroySlowMoAffectsPhysics = true;

        public static event Action<SkateRunnerDestructibleObject, KillCause> OnEnemyKilledWithCause; // enemy kill + cause

        public KillCause LastKillCause { get; private set; } = KillCause.Unknown;

        private float _initialHealth;
        private bool _isDead;
        private Enemy1SlicePool _enemy1SlicePool;

        private void Awake()
        {
            _initialHealth = health;
            // Prewarm during enemy/spawner initialization, never during the lethal hit.
            if (GetComponent<EnemyType1>() != null)
                _enemy1SlicePool = Enemy1SlicePool.Prepare(DebrisPrefab);
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

            bool presented = _enemy1SlicePool != null && _enemy1SlicePool.Play(
                DebrisPrefab, transform.position, transform.rotation, transform.lossyScale, KillContext.Current);
            // Preserve the existing debris path for every other enemy, including Phase 2.
            if (!presented && DebrisPrefab != null)
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

            // Always broadcast destroyed (barrels included)
            OnDestroyed?.Invoke(this);

            // Only enemies count for enemy-kill systems (slam meter, missions, etc.)
            if (countsAsEnemyKill)
            {
                // HIT STOP (gated: one per attack)
                bool isEnemyType3 = GetComponentInParent<EnemyType3>() != null;
                if (!isEnemyType3)
                {
                    SkateRunnerGameFeel.TriggerEnemyKillHitStopStatic(KillContext.Current, KillContext.CurrentAttackId);
                }

                // HAPTICS: enemy kill feedback (any attack)
                

                // FEEL: subtle camera impulse on every enemy kill
                SkateRunnerGameFeel.TriggerEnemyKillCameraShakeStatic(hitPoint);
SkateRunnerHaptics.PlayPreset(HapticPatterns.PresetType.MediumImpact);

                LastKillCause = KillContext.Current;

                OnEnemyKilled?.Invoke(this);
                OnEnemyKilledWithCause?.Invoke(this, LastKillCause);
            }

            gameObject.SetActive(false);
        }
        public void ResetDestructible()
        {
            _isDead = false;
        }
    }

}
