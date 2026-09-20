using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace IndieKit
{
    /// <summary>
    /// Fixed-capacity pool for recurring physics debris. The oldest presentation is
    /// recycled when capacity is reached, so kills never instantiate during play.
    /// </summary>
    public sealed class SkateRunnerDebrisPool : MonoBehaviour
    {
        private sealed class Entry
        {
            public GameObject root;
            public Transform[] pieces;
            public Vector3[] positions;
            public Quaternion[] rotations;
            public Vector3[] scales;
            public Rigidbody[] bodies;
            public Collider[] colliders;
            public VisualEffect[] visualEffects;
        }

        private static readonly Dictionary<GameObject, SkateRunnerDebrisPool> Pools =
            new Dictionary<GameObject, SkateRunnerDebrisPool>();

        private GameObject prefab;
        private Entry[] entries;
        private int next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pools.Clear();
        }

        public static SkateRunnerDebrisPool Prepare(GameObject debrisPrefab, int capacity = 4)
        {
            if (debrisPrefab == null)
                return null;

            if (Pools.TryGetValue(debrisPrefab, out SkateRunnerDebrisPool existing) && existing != null)
                return existing;

            GameObject host = new GameObject($"DebrisPool ({debrisPrefab.name})");
            SkateRunnerDebrisPool pool = host.AddComponent<SkateRunnerDebrisPool>();
            pool.prefab = debrisPrefab;
            pool.Build(Mathf.Clamp(capacity, 1, 8));
            Pools[debrisPrefab] = pool;
            return pool;
        }

        private void Build(int capacity)
        {
            entries = new Entry[capacity];
            for (int i = 0; i < capacity; i++)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.name = prefab.name + " (pooled)";
                AutoDestroyAfterSeconds lifetime = instance.GetComponent<AutoDestroyAfterSeconds>();
                if (lifetime != null)
                    lifetime.ConfigurePooling(true);

                Entry entry = new Entry
                {
                    root = instance,
                    pieces = instance.GetComponentsInChildren<Transform>(true),
                    bodies = instance.GetComponentsInChildren<Rigidbody>(true),
                    colliders = instance.GetComponentsInChildren<Collider>(true),
                    visualEffects = instance.GetComponentsInChildren<VisualEffect>(true)
                };
                entry.positions = new Vector3[entry.pieces.Length];
                entry.rotations = new Quaternion[entry.pieces.Length];
                entry.scales = new Vector3[entry.pieces.Length];
                for (int p = 0; p < entry.pieces.Length; p++)
                {
                    entry.positions[p] = entry.pieces[p].localPosition;
                    entry.rotations[p] = entry.pieces[p].localRotation;
                    entry.scales[p] = entry.pieces[p].localScale;
                }

                instance.SetActive(false);
                entries[i] = entry;
            }
        }

public bool Play(GameObject debrisPrefab, Vector3 position, Quaternion rotation, Vector3 scale, Vector3 hitPoint)
        {
            if (debrisPrefab != prefab || entries == null || entries.Length == 0)
                return false;

            Entry entry = entries[next];
            next = (next + 1) % entries.Length;
            entry.root.SetActive(false);
            entry.root.transform.SetPositionAndRotation(position, rotation);
            entry.root.transform.localScale = scale;

            for (int p = 0; p < entry.pieces.Length; p++)
            {
                // The root was just placed at the destroyed barrel's world pose.
                // Restoring its cached local pose would send the whole effect back
                // to the debris prefab's original position under the pool host.
                if (entry.pieces[p] == entry.root.transform)
                    continue;

                entry.pieces[p].localPosition = entry.positions[p];
                entry.pieces[p].localRotation = entry.rotations[p];
                entry.pieces[p].localScale = entry.scales[p];
            }

            for (int i = 0; i < entry.bodies.Length; i++)
            {
                entry.bodies[i].linearVelocity = Vector3.zero;
                entry.bodies[i].angularVelocity = Vector3.zero;
                entry.bodies[i].Sleep();
            }

            for (int i = 0; i < entry.colliders.Length; i++)
                entry.colliders[i].enabled = true;

            bool playOptionalVfx = SkateRunnerPerformanceManager.CurrentTier == SkateRunnerPerformanceTier.High;
            for (int i = 0; i < entry.visualEffects.Length; i++)
                entry.visualEffects[i].enabled = playOptionalVfx;

            entry.root.SetActive(true);
            for (int i = 0; i < entry.bodies.Length; i++)
            {
                entry.bodies[i].WakeUp();
                entry.bodies[i].AddExplosionForce(4f, hitPoint, 1.5f, 0f, ForceMode.Impulse);
            }

            return true;
        }

        private void OnDestroy()
        {
            if (prefab != null && Pools.TryGetValue(prefab, out SkateRunnerDebrisPool pool) && pool == this)
                Pools.Remove(prefab);
        }
    }
}
