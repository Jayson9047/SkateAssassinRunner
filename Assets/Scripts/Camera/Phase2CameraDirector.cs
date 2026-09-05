using Unity.Cinemachine;
using UnityEngine;

public class Phase2CameraDirector : MonoBehaviour
{
    public static Phase2CameraDirector ActiveInstance { get; private set; }

    [SerializeField] private CinemachineCamera followCam;
    [SerializeField] private CinemachineCamera collisionCam;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private int followPriority = 10;
    [SerializeField] private int collisionPriority = 20;

    public bool IsCollisionCameraSettled =>
        brain != null && collisionCam != null && !brain.IsBlending && CinemachineCore.IsLive(collisionCam);

    private void Awake()
    {
        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    private void OnEnable() => ActiveInstance = this;

    private void OnDisable()
    {
        if (ActiveInstance == this) ActiveInstance = null;
    }

    public void SwitchToCollision()
    {
        if (followCam != null) followCam.Priority = followPriority;
        if (collisionCam != null) collisionCam.Priority = collisionPriority;
    }

    public void SwitchToFollow()
    {
        if (followCam != null) followCam.Priority = followPriority;
        if (collisionCam != null) collisionCam.Priority = followPriority - 1;
    }
}
