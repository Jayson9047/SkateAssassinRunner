using Unity.Cinemachine;
using UnityEngine;

public class Phase2CameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followCam;
    [SerializeField] private CinemachineCamera collisionCam;
    [SerializeField] private int followPriority = 10;
    [SerializeField] private int collisionPriority = 20;

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
