using UnityEngine;

public class CameraPlayerBinder : MonoBehaviour
{
    [SerializeField] private CameraFollowTargetYOnly followTarget;

    [Header("Where to frame the player")]
    [SerializeField] private float xOffsetFromPlayer = 0f;   // 0 = player centered horizontally
    [SerializeField] private float yOffsetFromPlayer = 1.5f; // typical headroom
    [SerializeField] private float cameraZ = -10f;           // your side-view distance

    public void BindTo(Transform player)
    {
        followTarget.player = player;

        // Capture the player's current X so the camera starts centered
        followTarget.fixedX = player.position.x + xOffsetFromPlayer;
        followTarget.fixedZ = cameraZ;
        followTarget.yOffset = yOffsetFromPlayer;
    }
}
