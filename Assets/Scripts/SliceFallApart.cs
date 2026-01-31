using UnityEngine;

public class SliceFallApart : MonoBehaviour
{
    [Header("Pieces")]
    [Tooltip("All rigidbody pieces to separate (2 slices OR 17 shield shards).")]
    public Rigidbody[] pieces;

    [Header("Forces")]
    public float separationForce = 1.5f;
    public float upwardForce = 0.5f;
    public float torqueForce = 1.2f;

    [Header("Spread")]
    [Tooltip("Bias direction in local space (right by default).")]
    public Vector3 localBiasDirection = Vector3.right;

    [Range(0f, 1f)]
    [Tooltip("0 = strict left/right bias, 1 = very random shatter.")]
    public float randomness = 0.5f;

    [Header("Lifetime")]
    public float destroyAfterSeconds = 3f;

    private void Start()
    {
        // If not assigned, auto-find all child rigidbodies
        if (pieces == null || pieces.Length == 0)
            pieces = GetComponentsInChildren<Rigidbody>(true);

        if (pieces == null || pieces.Length == 0)
        {
            Destroy(gameObject, destroyAfterSeconds);
            return;
        }

        Vector3 biasWorld = transform.TransformDirection(localBiasDirection.normalized);

        for (int i = 0; i < pieces.Length; i++)
        {
            Rigidbody rb = pieces[i];
            if (rb == null) continue;

            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.WakeUp();

            // Alternate left/right so it spreads instead of collapsing into one pile
            float side = (i % 2 == 0) ? 1f : -1f;

            // Random direction blended with bias (keeps it consistent + juicy)
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = Mathf.Abs(randomDir.y); // bias upward

            Vector3 dir = Vector3.Lerp(biasWorld * side, randomDir, randomness).normalized;

            rb.AddForce(dir * separationForce + Vector3.up * upwardForce, ForceMode.Impulse);

            float torqueScale = Mathf.Lerp(0.5f, 1f, Random.value);
            rb.AddTorque(Random.insideUnitSphere * (torqueForce * torqueScale), ForceMode.Impulse);
        }

        Destroy(gameObject, destroyAfterSeconds);
    }
}
