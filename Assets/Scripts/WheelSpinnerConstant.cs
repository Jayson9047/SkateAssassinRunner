using UnityEngine;

public class WheelSpinnerConstant : MonoBehaviour
{
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float spinDegreesPerSecond = 720f; // 2 rotations/sec

    private void Update()
    {
        float d = spinDegreesPerSecond * Time.deltaTime;
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i]) wheels[i].Rotate(Vector3.right, d, Space.Self);
    }
}
