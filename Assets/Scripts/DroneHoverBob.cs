using UnityEngine;

public class DroneHoverBob : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 2.0f;

    [Header("Drift (Perlin)")]
    [SerializeField] private float driftAmplitudeXZ = 0.06f;
    [SerializeField] private float driftSpeed = 0.8f;

    [Header("Wobble Rotation")]
    [SerializeField] private float wobbleDegrees = 3f;
    [SerializeField] private float wobbleSpeed = 1.6f;

    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot;
    private float _seed;

    private void Awake()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _seed = Random.Range(0f, 999f);
    }

    private void OnEnable()
    {
        // reset in case pooled
        transform.localPosition = _baseLocalPos;
        transform.localRotation = _baseLocalRot;
    }

    private void Update()
    {
        float t = Time.time;

        float bob = Mathf.Sin((t + _seed) * bobFrequency) * bobAmplitude;

        float nx = Mathf.PerlinNoise(_seed, (t * driftSpeed)) * 2f - 1f;
        float nz = Mathf.PerlinNoise(_seed + 10f, (t * driftSpeed)) * 2f - 1f;

        Vector3 drift = new Vector3(nx, 0f, nz) * driftAmplitudeXZ;

        transform.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f) + drift;

        float roll = Mathf.Sin((t + _seed) * wobbleSpeed) * wobbleDegrees;
        float pitch = Mathf.Sin((t + _seed + 2f) * wobbleSpeed) * wobbleDegrees;

        transform.localRotation = _baseLocalRot * Quaternion.Euler(pitch, 0f, -roll);
    }
}
