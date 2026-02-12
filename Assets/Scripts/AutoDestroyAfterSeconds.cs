using UnityEngine;

public class AutoDestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Kill), lifetime);
    }

    //Just in case we want to kill it manually before the time is up
    private void Kill() => Destroy(gameObject);
}
