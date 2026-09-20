using UnityEngine;

public class AutoDestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField, HideInInspector] private bool recycleInsteadOfDestroy;

    public void ConfigurePooling(bool recycle)
    {
        recycleInsteadOfDestroy = recycle;
        CancelInvoke();
    }

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(Kill), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Kill()
    {
        if (recycleInsteadOfDestroy)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
