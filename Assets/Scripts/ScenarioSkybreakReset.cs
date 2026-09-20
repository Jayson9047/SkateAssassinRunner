using UnityEngine;

/// <summary>Restores this encounter's shield squad after shatter, knockback, or execution.</summary>
public sealed class ScenarioSkybreakReset : MonoBehaviour
{
    public EnemyType2[] guards;
    [Tooltip("Maximum recoil from each guard's authored position inside this moving chunk. Contains the existing world-space knockback tween.")]
    [Min(0f)] public float maximumLocalRecoil = 2f;
    Vector3[] positions;
    Quaternion[] rotations;
    Transform[] layeredObjects;
    int[] layers;

    void Awake()
    {
        positions = new Vector3[guards.Length];
        rotations = new Quaternion[guards.Length];
        var objects = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < guards.Length; i++)
        {
            positions[i] = guards[i].transform.localPosition;
            rotations[i] = guards[i].transform.localRotation;
            objects.AddRange(guards[i].GetComponentsInChildren<Transform>(true));
        }
        layeredObjects = objects.ToArray();
        layers = new int[layeredObjects.Length];
        for (int i = 0; i < layers.Length; i++) layers[i] = layeredObjects[i].gameObject.layer;
    }

    void OnEnable()
    {
        Restore();
        foreach (var guard in guards)
        {
            guard.gameObject.SetActive(true);
            var animator = guard.GetComponent<Animator>();
            if (animator) { animator.Rebind(); animator.Update(0f); }
        }
    }

    void OnDisable() => Restore();

    void LateUpdate()
    {
        if (positions == null) return;
        for (int i = 0; i < guards.Length; i++)
        {
            var guard = guards[i];
            if (!guard || guard.IsShieldIntact) continue;
            // Enemy2 tweens world X while the parent chunk also moves. A short
            // world-space tween alone does not bound its displacement in the chunk.
            var position = guard.transform.localPosition;
            position.x = Mathf.Clamp(position.x, positions[i].x-maximumLocalRecoil, positions[i].x+maximumLocalRecoil);
            guard.transform.localPosition = position;
        }
    }

    void Restore()
    {
        if (positions == null) return;
        for (int i = 0; i < guards.Length; i++)
        {
            if (!guards[i]) continue;
            guards[i].transform.localPosition = positions[i];
            guards[i].transform.localRotation = rotations[i];
        }
        for (int i = 0; i < layers.Length; i++)
            if (layeredObjects[i]) layeredObjects[i].gameObject.layer = layers[i];
    }
}
