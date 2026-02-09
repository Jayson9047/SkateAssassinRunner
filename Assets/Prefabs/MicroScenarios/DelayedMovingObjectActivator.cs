using UnityEngine;
using MoreMountains.InfiniteRunnerEngine;

public class DelayedMovingObjectActivator : MonoBehaviour
{
    [SerializeField] private MovingObject movingObject;
    [SerializeField] private float activationDelay = 1.5f;
    [SerializeField] private float activeSpeed = 6f;

    private void Awake()
    {
        if (!movingObject)
            movingObject = GetComponent<MovingObject>();

        if (movingObject != null)
            movingObject.Speed = 0f;
    }

    private void OnEnable()
    {
        // pooled objects re-enable -> restart timing
        CancelInvoke();
        Invoke(nameof(Activate), activationDelay);
    }

    private void Activate()
    {
        if (movingObject == null) return;
        movingObject.Speed = activeSpeed;
    }
}
