using UnityEngine;

public class CarImpulseTest : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform rearPoint;

    [Header("Impulse Tuning (VelocityChange = instant kick)")]
    [SerializeField] private float upVelChange = 4.0f;

    // NEW: sideways �blown away� amount (X axis travel)
    [SerializeField] private float sideVelChange = 6.0f;

    // NEW: which way on X should it fly? (+1 = +X, -1 = -X)
    [SerializeField] private float xDirection = 1f;

    [Header("Flip Tuning")]
    [SerializeField] private float pitchAngVelChange = 5.0f;

    // Optional: add some roll for drama
    [SerializeField] private float rollAngVelChange = 0.0f;

    [SerializeField] private GameObject explosionObject;
    // If you zero velocity every time, you�ll never �carry� motion from before the blast.
    [Header("Debug")]
    

    [Header("Ground Impact Audio")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0f)] private float minimumGroundImpactSpeed = 2f;
[SerializeField] private bool clearVelocitiesFirst = true;

    public bool FlipArmed => _flipArmed;

    private bool _flipArmed;

    
    private bool _awaitingGroundImpact;
private bool _explosionDetached;

    public void DetachExplosionFromRearImpactPoint()
    {
        if (_explosionDetached) return;
        if (explosionObject == null) return;

        // Keep the same world position/rotation, just stop following the car.
        explosionObject.transform.SetParent(null, true);

        _explosionDetached = true;
    }
    public void ArmFlipOnce()
    {
        
        _awaitingGroundImpact = false;
_flipArmed = true;
    }

    public void DisarmFlip()
    {
        _flipArmed = false;
    }

    public void BlowRearUp()
    {
        if (!_flipArmed) return;   // <-- NEW gate
        _flipArmed = false;        // <-- consume token (one-shot)
if (rb == null || rearPoint == null) return;

        SkateRunnerAudioManager.PlayPhase2CarExplosion();
        _awaitingGroundImpact = true;
        
rb.WakeUp();
        explosionObject?.SetActive(true);
        if (clearVelocitiesFirst)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 blastVel = (Vector3.up * upVelChange) + (Vector3.right * sideVelChange * Mathf.Sign(xDirection));
        rb.AddForceAtPosition(blastVel, rearPoint.position, ForceMode.VelocityChange);
        rb.AddTorque(transform.right * pitchAngVelChange, ForceMode.VelocityChange);

        if (rollAngVelChange != 0f)
            rb.AddTorque(transform.forward * rollAngVelChange, ForceMode.VelocityChange);
    }


private void OnCollisionEnter(Collision collision)
    {
        if (!_awaitingGroundImpact || collision == null || collision.collider == null) return;
        if ((groundLayers.value & (1 << collision.collider.gameObject.layer)) == 0) return;
        if (collision.relativeVelocity.magnitude < minimumGroundImpactSpeed) return;
        _awaitingGroundImpact = false;
        SkateRunnerAudioManager.PlayPhase2CarGroundImpact();
    }
}
