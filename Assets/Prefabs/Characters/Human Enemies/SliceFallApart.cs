using UnityEngine;

public class SliceFallApart : MonoBehaviour
{
    [Header("Assign these")]
    public Rigidbody upperSlice;
    public Rigidbody lowerSlice;

    [Header("Tweak")]
    public float separationForce = 1.5f;
    public float upwardForce = 0.5f;
    public float torqueForce = 1.2f;

    void Start()
    {
        Vector3 right = transform.right;

        upperSlice.AddForce(right * separationForce + Vector3.up * upwardForce, ForceMode.Impulse);
        lowerSlice.AddForce(-right * (separationForce * 0.3f), ForceMode.Impulse);

        upperSlice.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        lowerSlice.AddTorque(Random.insideUnitSphere * (torqueForce * 0.5f), ForceMode.Impulse);

        Destroy(gameObject, 3f);
    }
}
