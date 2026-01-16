using UnityEngine;

public class CameraFollowTargetYOnly : MonoBehaviour
{
    public Transform player;

    [Header("Base Follow")]
    public float yOffset = 1.5f;
    public float fixedX = 0f;
    public float fixedZ = -10f;

    [Header("Overshoot")]
    public float overshootDamping = 10f;
    public float overshootReturnSpeed = 12f;

    float overshootOffsetY;
    float overshootVelocity;

    void LateUpdate()
    {
        if (!player) return;

        // Smoothly return overshoot to zero
        overshootOffsetY = Mathf.SmoothDamp(
            overshootOffsetY,
            0f,
            ref overshootVelocity,
            1f / overshootReturnSpeed
        );

        float finalY = player.position.y + yOffset + overshootOffsetY;
        transform.position = new Vector3(fixedX, finalY, fixedZ);
    }
}
