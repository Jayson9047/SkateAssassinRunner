using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

public class CarSpeedPulseActivator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MovingObject carMovingObject;
    [SerializeField] private Transform carTransform; // the thing that drifts (car root)

    [Header("Pulse")]
    [SerializeField] private float activeSpeed = 6f;
    [SerializeField] private float activeDuration = 2f;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 initialLocalScale;

    private bool activated;
    private Coroutine routine;

    private void Awake()
    {
        if (!carMovingObject)
            carMovingObject = GetComponentInParent<MovingObject>();

        if (!carTransform)
            carTransform = carMovingObject != null ? carMovingObject.transform : transform;

        CacheInitialLocalTransform();

        if (carMovingObject != null)
            carMovingObject.Speed = 0f;
    }

    private void OnEnable()
    {
        // Pool reuse reset
        activated = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (carMovingObject != null)
            carMovingObject.Speed = 0f;

        RestoreInitialLocalTransform();
    }

    private void CacheInitialLocalTransform()
    {
        if (carTransform == null) return;
        initialLocalPos = carTransform.localPosition;
        initialLocalRot = carTransform.localRotation;
        initialLocalScale = carTransform.localScale;
    }

    private void RestoreInitialLocalTransform()
    {
        if (carTransform == null) return;
        carTransform.localPosition = initialLocalPos;
        carTransform.localRotation = initialLocalRot;
        carTransform.localScale = initialLocalScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (activated) return;
        if (!collision.gameObject.CompareTag("CarAccelerationCollider")) return;

        Activate();
    }

    // call this from your collision hit
    public void Activate()
    {
        if (activated) return;
        activated = true;

        if (carMovingObject == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PulseRoutine());
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        carMovingObject.Speed = activeSpeed;

        yield return new WaitForSeconds(activeDuration);

        carMovingObject.Speed = 0f;

        // Snap back so pooled reuse is clean (also prevents “drift accumulation”)
        RestoreInitialLocalTransform();

        routine = null;
    }
}
