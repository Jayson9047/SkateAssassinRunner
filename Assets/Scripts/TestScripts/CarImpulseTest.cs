using UnityEngine;

public class CarImpulseTest : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform rearPoint;

    [Header("Impulse Tuning")]
    [SerializeField] private float upVelChange = 4.0f;
    [SerializeField] private float pitchAngVelChange = 5.0f;

    public void BlowRearUp()
    {
        if (rb == null || rearPoint == null) return;

        rb.WakeUp();

        // Clear for a readable test
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply an upward velocity change at the rear point -> guaranteed pitch torque
        rb.AddForceAtPosition(Vector3.up * upVelChange, rearPoint.position, ForceMode.VelocityChange);

        // Extra guarantee: pitch-up angular velocity
        rb.AddTorque(transform.right * pitchAngVelChange, ForceMode.VelocityChange);
    }
}
