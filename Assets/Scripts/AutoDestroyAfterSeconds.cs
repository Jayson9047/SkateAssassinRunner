using UnityEngine;

public class AutoDestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Kill), lifetime);
    }

    private void Kill() => Destroy(gameObject);
}
