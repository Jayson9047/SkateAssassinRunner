using UnityEngine;

/// <summary>Keeps the Phase 2 subject-only overlay camera exactly aligned to the live gameplay camera.</summary>
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public sealed class Phase2SubjectCameraSync : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Camera subjectCamera;

    public void Configure(Camera source, Camera subject)
    {
        sourceCamera = source;
        subjectCamera = subject;
        SynchronizeNow();
    }

    private void LateUpdate()
    {
        if (subjectCamera == null || !subjectCamera.enabled)
            return;

        SynchronizeNow();
    }

    public void SynchronizeNow()
    {
        if (sourceCamera == null || subjectCamera == null)
            return;

        transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        subjectCamera.orthographic = sourceCamera.orthographic;
        subjectCamera.orthographicSize = sourceCamera.orthographicSize;
        subjectCamera.fieldOfView = sourceCamera.fieldOfView;
        subjectCamera.nearClipPlane = sourceCamera.nearClipPlane;
        subjectCamera.farClipPlane = sourceCamera.farClipPlane;
        subjectCamera.aspect = sourceCamera.aspect;
        subjectCamera.rect = sourceCamera.rect;
    }
}
